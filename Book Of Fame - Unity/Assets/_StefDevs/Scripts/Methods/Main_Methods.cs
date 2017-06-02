using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text.RegularExpressions;
using StefDevs;

public static partial class Methods
{

    public static void Main_Start(GameData gameData)
    {
        gameData.book = gameData.book_mono.data;

        Book book = gameData.book;            

        // Get the manifest describing our request
        WebClient client = new WebClient();
        // Download manifest into single string
        book.manifest.manifestString = client.DownloadString(gameData.book_manifestURL);
        // Parse string for page descriptions using a defined regex
        book.manifest.pageDescriptions = gameData.manifest_regex.Matches(book.manifest.manifestString);

        IIIF_ImageRequestParams imageRequestParams = gameData.defaultImageRequestParams;

        #region // Get all transcription annotations
        // Load the transcription annotation manifest
        IIIF_AnnotationManifestFromJSON transcriptionManifest = new IIIF_AnnotationManifestFromJSON();
        JsonUtility.FromJsonOverwrite(Resources.Load<TextAsset>("Transcriptions/anno").text, transcriptionManifest);
        Dictionary<IIIF_EntryCoordinate, List<IIIF_Transcription_Element>> transcriptionAnnotations = new Dictionary<IIIF_EntryCoordinate, List<IIIF_Transcription_Element>>();

        for (int ann = 0; ann < transcriptionManifest.resources.Length; ann++)
        {
            IIIF_AnnotationManifestFromJSON_AnnotationElement annotationElement = transcriptionManifest.resources[ann];
            // Regex match within url provided

            // Determine the entry coordinate
            int indexOfEntryCoords = annotationElement.on.IndexOf(".jp2#") - 4;
            string coordString = annotationElement.on.Substring(indexOfEntryCoords, 4);
            IIIF_EntryCoordinate coord = new IIIF_EntryCoordinate()
            {
                isVerso = coordString.Contains("v"),
                leafNumber = int.Parse(coordString.Substring(0, 3))
            };

            // If we dont have a list of annotations for this entry/page, create/add one
            if (!transcriptionAnnotations.ContainsKey(coord))
                transcriptionAnnotations.Add(coord, new List<IIIF_Transcription_Element>());

            string rectParamsLabel = "#xywh=";
            string rectParamsString = annotationElement.on.Substring(annotationElement.on.IndexOf(rectParamsLabel) + rectParamsLabel.Length);
            int i_comma;
            int[] numbers = new int[4];
            for (int i = 0; i < 3; i++)
            {
                i_comma = rectParamsString.IndexOf(',');
                numbers[i] = int.Parse(rectParamsString.Substring(0, i_comma));
                rectParamsString = rectParamsString.Substring(i_comma + 1);
            }
            numbers[3] = int.Parse(rectParamsString);

            // Add this transcription element to the collection
            transcriptionAnnotations[coord].Add(
                    new IIIF_Transcription_Element()
                    {
                        content = annotationElement.resource.chars,
                        boundingBox_normalizedInPageSpace = new Rect(
                            (float)numbers[0] / imageRequestParams.targetWidth,
                            (float)numbers[1] / imageRequestParams.targetHeight,
                            (float)numbers[2] / imageRequestParams.targetWidth,
                            (float)numbers[3] / imageRequestParams.targetHeight
                            )
                    }
                    );
        }
        #endregion // Get all transcription annotations


        /// TODO (Stef) :: replace this assumption making hack (specifying desired page description indicies manually) with proper parsing of manifest to determine which page descriptions to download 
        IIIF_EntryCoordinate currentEntryCoordinate = new IIIF_EntryCoordinate() { isVerso = true, leafNumber = 81 };
        int descriptionIndex_first = 167; // 81 verso
        int descriptionIndex_last = 178; // 88 recto
        Book_Entry newPage;
        List<IIIF_Transcription_Element> transcriptionAnnotationList = new List<IIIF_Transcription_Element>();
        for (int i = descriptionIndex_first; i <= descriptionIndex_last; i++)
        {
            #region // Create book entries

            // creat a new entry
            newPage = new Book_Entry()
            {
                coordinate = currentEntryCoordinate,
                material_base = new Material(gameData.bookEntryBaseMaterial) {
                    name = "Placeholder Manuscript Mat - " + currentEntryCoordinate
                },
                material_transcription = new Material(gameData.bookEntryBaseTranscriptionMaterial)
                {
                    name = "Placeholder Transcription Mat - " + currentEntryCoordinate
                },
            };

            // Flip recto pages horizontally
            if (!newPage.coordinate.isVerso)
            {
                newPage.material_base.mainTextureScale = new Vector2(-1, 1);
                newPage.material_transcription.mainTextureScale = new Vector2(-1, 1);
            }

            // Add to indexed collection of entries
            book.entries.Add(newPage.coordinate, newPage);
            // Add to culminative list of entries (do we need this?)
            book.currentlyAccessibleEntries.Add(newPage.coordinate);


            #region // Create and queue manuscript image download job

            #region // Create image request params for manuscript image
            // verso images get offset of 60, recto gets 175
            imageRequestParams.cropOffsetX = newPage.coordinate.isVerso ? 175 : 60;
            imageRequestParams.webAddress = Methods.IIIF_Remove_Tail_From_Web_Address(book.manifest.pageDescriptions[i].Groups[1].Value);

            // Store request params for later use
            book.page_imageRequestParams.Add(newPage.coordinate, imageRequestParams);
            #endregion // Create image request params for manuscript image

            // Create a download job for the entry image
            IIIF_ImageDownloadJob downloadJob = new IIIF_ImageDownloadJob()
            {
                imageRequestParams = imageRequestParams,
                targetPageCoordinate = currentEntryCoordinate,
                resultTexture = new Texture2D(imageRequestParams.targetWidth, imageRequestParams.targetHeight),
                targetUrl = IIIF_Determine_Web_Address_For_Image(imageRequestParams),
                // NOTE (Stef) :: Don't create www object until ready to start downloading
                iiif_www = null
            };

            // Add it to the job queue
            gameData.imageDownload_jobQueue.Enqueue(downloadJob);
            #endregion 



            currentEntryCoordinate++;
            #endregion // Create book entries
        }

        #region // Generate transcription images
        RenderTexture renderTexture = new RenderTexture(imageRequestParams.targetWidth, imageRequestParams.targetHeight,0);
        renderTexture.Create();
        RenderTexture.active = renderTexture;

        TranscriptionRenderer transcriptionRenderer = (GameObject.Instantiate(gameData.assetReferences.transcription_rendererFab) as TranscriptionRenderer_mono).data;

        transcriptionRenderer.camera.targetTexture = renderTexture;
        List<TranscriptionRenderer_Annotation> annotationObjects = new List<TranscriptionRenderer_Annotation>();
        TranscriptionRenderer_Annotation newAnnotationUIElement;
        int n = 0;
        foreach (IIIF_EntryCoordinate key in transcriptionAnnotations.Keys)
        {
            if (n != 0) break;
            n++;

            List<IIIF_Transcription_Element> annotations = transcriptionAnnotations[key];

            // Build canvas

            // Build annotations on canvas
            foreach (IIIF_Transcription_Element annotation in annotations)
            {
                newAnnotationUIElement = (GameObject.Instantiate(gameData.assetReferences.transcription_annotationFab, transcriptionRenderer.canvas.transform, false) as TranscriptionRenderer_Annotation_mono).data;
                //newAnnotationUIElement.transform.SetParent(transcriptionRenderer.canvas.transform);
                Vector2 position = annotation.boundingBox_normalizedInPageSpace.position;
                position.x *= transcriptionRenderer.canvas.pixelRect.width;
                position.y *= transcriptionRenderer.canvas.pixelRect.height;
                newAnnotationUIElement.transform.anchoredPosition = position;
                newAnnotationUIElement.transform.sizeDelta = (Vector2.right * annotation.boundingBox_normalizedInPageSpace.width) + (Vector2.up * annotation.boundingBox_normalizedInPageSpace.height);
                newAnnotationUIElement.textMesh.text = annotation.content;
            }


            // Render to render texture
            renderTexture.DiscardContents();
            transcriptionRenderer.camera.Render();

            // read render texture into texture2D
            Texture2D texture = new Texture2D(imageRequestParams.targetWidth, imageRequestParams.targetHeight);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            // Create material
            // Assign the texture to the book entry
            book.entries[key].pageImage_transcription = texture;
            book.entries[key].material_transcription.mainTexture = book.entries[key].pageImage_transcription;
            book.entries[key].material_transcription.name = "Generated Transcription Mat - " + key;

            ImageTestPage_mono imageTest = GameObject.Instantiate(gameData.imageTestPrefab) as ImageTestPage_mono;
            imageTest.transform.localScale = new Vector3((float)texture.width / (float)texture.height, 1, 1);
            imageTest.gameObject.name = "Image Test - " + key;
            imageTest.transform.position = (Vector3.up * 1.1f * (key.leafNumber - 81 - (key.isVerso ? 0 : 1))) + Vector3.right * .4f * (key.isVerso ? -1 : 1);
            imageTest.renderer.material = book.entries[key].material_transcription;

        }

        // Cleanup
        {
            //GameObject.Destroy(transcriptionRenderer.gameObject);
            //RenderTexture.active = null;            
        }

        // Generate UI canvas with annotations laid out with TMP text elements
        // Set them all to auto size
        // After all are generated, find the maximum font size used and set all annotations to that size
        // Generate camera and render to texture - store the texture on the "newEntry"
        // Create camera
        // Set aspect ratio to correct size
        // Set Canvas camera to camera

        // Create new material and assign the texture to it's albedo etc.

        #endregion // Generate transcription image from transcription manuscript

        book.entriesDebugList = new List<Book_Entry>(book.entries.Values);


        // Initialize book

        // Set initial page
        /// NOTE (Stef) :: This assumes that the first entry in the list is a verso entry
        book.minRectoEntry = book.currentlyAccessibleEntries[1];
        book.maxRectoEntry = book.currentlyAccessibleEntries[book.currentlyAccessibleEntries.Count  - 1];
        book.currentRectoEntry = book.minRectoEntry;

        // Set materials for each page
        Methods.Book_Update_Page_Materials(book.entries, book.worldRefs.pageRenderers,book.openRectoRendererIndex, book.currentRectoEntry, gameData.bookEntryBaseMaterial);
    }

    public static void Book_Update_Page_Materials(Dictionary<IIIF_EntryCoordinate, Book_Entry> entries, Renderer[] pageRenderers, int openRectoRendererIndex, IIIF_EntryCoordinate currentRectoEntry, Material fallbackMaterial)
    {
        IIIF_EntryCoordinate entryCoord;
        for (int i = 0; i < pageRenderers.Length; i++)
        {
            entryCoord = IIIF_EntryCoordinate.Translate(currentRectoEntry, i - openRectoRendererIndex);
            if (entries.ContainsKey(entryCoord))
            {
                pageRenderers[i].material = entries[entryCoord].material_base;
            }
            else
            {
                pageRenderers[i].material = fallbackMaterial;
            }
        }
    }

    public static void Main_Update(GameData gameData)
    {
        Book book = gameData.book;

        #region // Image download job maintenance
        if (gameData.imageDownload_currentJob != null)
        {
            IIIF_ImageDownloadJob downloadJob = gameData.imageDownload_currentJob;
            Book_Entry targetEntry = book.entries[downloadJob.targetPageCoordinate];
            if (gameData.imageDownload_currentJob.iiif_www.isDone)
            {

                // Detect failure
                if (!string.IsNullOrEmpty(downloadJob.iiif_www.error))
                {
                    // Log failure
                    Debug.LogError("Failed to download " + downloadJob.iiif_www.url + ":" + downloadJob.iiif_www.error);
                }
                else
                {
                    // Log success
                    Debug.Log("FINISHED downloading " + downloadJob.targetPageCoordinate + " URL: " + downloadJob.iiif_www.url);

                    // Copy downloaded texture to page data
                    targetEntry.pageImage_base = downloadJob.iiif_www.texture;
                    targetEntry.material_base.mainTexture = targetEntry.pageImage_base;
                    targetEntry.material_base.name = "Generated Manuscript Mat - " + targetEntry.coordinate;
                    //ImageTestPage_mono imageTest = GameObject.Instantiate(gameData.imageTestPrefab) as ImageTestPage_mono;
                    //imageTest.transform.localScale = new Vector3((float)downloadJob.iiif_www.texture.width / (float)downloadJob.iiif_www.texture.height, 1, 1);
                    //imageTest.gameObject.name = "Image Test - " + downloadJob.targetPageCoordinate;
                    //imageTest.transform.position = (Vector3.up * 1.1f * (downloadJob.targetPageCoordinate.leafNumber - 81 - (downloadJob.targetPageCoordinate.isVerso ? 0 : 1))) + Vector3.right * .4f * (downloadJob.targetPageCoordinate.isVerso ? -1 : 1);
                    //imageTest.renderer.material = book.entries[downloadJob.targetPageCoordinate].material_base;
                }

                // Discard download job
                gameData.imageDownload_jobQueue.Dequeue();
                gameData.imageDownload_currentJob = null;
            }
            else if (Time.time - gameData.timeOfLastProgressUpdate > .2f)
            {
                gameData.timeOfLastProgressUpdate = Time.time;
                Debug.Log("Download Progress: " + (downloadJob.iiif_www.progress * 100).ToString("0") + "%");
            }
        }

        // Start next job in queue, if any
        if (gameData.imageDownload_currentJob == null && gameData.imageDownload_jobQueue.Count > 0)
        {
            gameData.imageDownload_currentJob = gameData.imageDownload_jobQueue.Peek();
            gameData.imageDownload_currentJob.iiif_www = new WWW(gameData.imageDownload_currentJob.targetUrl);
            Debug.Log("STARTING download job :: " + gameData.imageDownload_currentJob.targetPageCoordinate + " URL: " + gameData.imageDownload_currentJob.iiif_www.url);
        }
        #endregion // Image download job maintenance
    }
}