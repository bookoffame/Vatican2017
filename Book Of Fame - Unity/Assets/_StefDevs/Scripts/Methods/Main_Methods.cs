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

        #region // Init book
        gameData.book = gameData.book_mono.data;

        Book book = gameData.book;            
        // Get the manifest describing our request
        WebClient client = new WebClient();
        // Download manifest into single string
        book.manifest.manifestString = client.DownloadString(gameData.book_manifestURL);
        // Parse string for page descriptions using a defined regex
        book.manifest.pageDescriptions = gameData.manifest_regex.Matches(book.manifest.manifestString);

        #region // Create book entries
        /// TODO (Stef) :: replace this assumption making hack (specifying desired page description indicies manually) with proper parsing of manifest to determine which page descriptions to download 
        IIIF_ImageRequestParams imageRequestParams = gameData.defaultImageRequestParams;
        IIIF_EntryCoordinate currentEntryCoordinate = new IIIF_EntryCoordinate() { isVerso = true, leafNumber = 81 };
        int descriptionIndex_first = 167; // 81 verso
        int descriptionIndex_last = 178; // 88 recto
        Book_Entry newPage;
        List<IIIF_Transcription_Element> transcriptionAnnotationList = new List<IIIF_Transcription_Element>();
        for (int i = descriptionIndex_first; i <= descriptionIndex_last; i++)
        {

            // creat a new entry
            newPage = new Book_Entry()
            {
                coordinate = currentEntryCoordinate,
                material_base = new Material(gameData.bookEntryBaseMaterial)
                {
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
            if(!gameData.debug_onlyDLOnePage || i == descriptionIndex_first)
                gameData.imageDownload_jobQueue.Enqueue(downloadJob);
            #endregion 



            currentEntryCoordinate++;
        }
        book.nInitialDownloadJobs = gameData.imageDownload_jobQueue.Count;
        gameData.allDownloadJobsFinished = false;
        #endregion // Create book entries

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
                        //boundingBox_normalizedInPageSpace = new Rect(numbers[0], numbers[1], numbers[2], numbers[3])
                        boundingBox_normalizedInPageSpace = new Rect(
                            (float)numbers[0] / imageRequestParams.cropWidth,
                            (float)numbers[1] / imageRequestParams.cropHeight,
                            (float)numbers[2] / imageRequestParams.cropWidth,
                            (float)numbers[3] / imageRequestParams.cropHeight
                            )
                    }
                    );
        }
        #endregion // Get all transcription annotations

        #region // Generate transcription images
        RenderTexture renderTexture = new RenderTexture(imageRequestParams.targetWidth, imageRequestParams.targetHeight,0);
        renderTexture.Create();
        RenderTexture.active = renderTexture;
        TranscriptionRenderer transcriptionRenderer = (GameObject.Instantiate(gameData.assetReferences.transcription_rendererFab) as TranscriptionRenderer_mono).data;
        transcriptionRenderer.camera.targetTexture = renderTexture;
        List<GameObject> annotationUIObjects = new List<GameObject>();
        TranscriptionRenderer_Annotation newAnnotationUIElement;

        foreach (IIIF_EntryCoordinate key in transcriptionAnnotations.Keys)
        {
            List<IIIF_Transcription_Element> annotations = transcriptionAnnotations[key];

            // Build annotations on canvas
            foreach (IIIF_Transcription_Element annotation in annotations)
            {
                newAnnotationUIElement = (GameObject.Instantiate(gameData.assetReferences.transcription_annotationFab, transcriptionRenderer.canvas.transform, false) as TranscriptionRenderer_Annotation_mono).data;
                newAnnotationUIElement.transform.offsetMin = Vector2.Scale(annotation.boundingBox_normalizedInPageSpace.min, transcriptionRenderer.canvas.pixelRect.size) ;
                newAnnotationUIElement.transform.offsetMax = Vector2.Scale(annotation.boundingBox_normalizedInPageSpace.max, transcriptionRenderer.canvas.pixelRect.size) ;
                newAnnotationUIElement.textMesh.text = annotation.content;
                annotationUIObjects.Add(newAnnotationUIElement.transform.gameObject);
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

            // Image test
            //ImageTestPage_mono imageTest = GameObject.Instantiate(gameData.imageTestPrefab) as ImageTestPage_mono;
            //imageTest.transform.localScale = new Vector3((float)texture.width / (float)texture.height, 1, 1);
            //imageTest.gameObject.name = "Image Test - " + key;
            //imageTest.transform.position = (Vector3.up * 1.1f * (key.leafNumber - 81 - (key.isVerso ? 0 : 1))) + Vector3.right * .4f * (key.isVerso ? -1 : 1);
            //imageTest.renderer.material = book.entries[key].material_transcription;

            // Cleanup
            foreach (GameObject uiElement in annotationUIObjects)
                GameObject.Destroy(uiElement);
            annotationUIObjects.Clear();
        }
        // Cleanup
        GameObject.Destroy(transcriptionRenderer.gameObject);
        RenderTexture.active = null;

        #endregion // Generate transcription image from transcription manuscript

        book.entriesDebugList = new List<Book_Entry>(book.entries.Values);

        // Initialize book
        book.worldRefs.baseMeshSkeleton_transforms = book.worldRefs.baseMeshSkeleton_root.GetComponentsInChildren<Transform>();
        book.worldRefs.transcriptionMeshSkeleton_transforms = book.worldRefs.transcriptionMeshSkeleton_root.GetComponentsInChildren<Transform>();

        // Set initial page
        /// NOTE (Stef) :: This assumes that the first entry in the list is a verso entry
        book.minRectoEntry = book.currentlyAccessibleEntries[1];
        book.maxRectoEntry = book.currentlyAccessibleEntries[book.currentlyAccessibleEntries.Count  - 1];
        book.currentRectoEntry = book.minRectoEntry;

        // Set materials for each page
        Methods.Book_Update_Page_Materials(book.entries, book.worldRefs.pageRenderers, book.worldRefs.pageRenderers_transcriptions, book.openRectoRendererIndex, book.currentRectoEntry, gameData.bookEntryBaseMaterial);
        #endregion // Init book

        #region // Init user
        gameData.user = new User()
        {
            agent = gameData.agentObject.data,
            userParams = gameData.assetReferences.userParams.data,
            intent = new User_Intent()
        };
        //gameData.user.agent.currentPosition = gameData.user.agent.transform.position;
        gameData.user.agent.pitch_current = gameData.user.agent.camera.transform.eulerAngles.x;
        gameData.user.agent.yaw_current = gameData.user.agent.camera.transform.eulerAngles.y;
        #endregion // Init user
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

                    // Image test
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
            else 
            {
                // Update loading bar
                float singlejobValue = 1f / book.nInitialDownloadJobs;
                int downloadsCompleted = book.nInitialDownloadJobs - gameData.imageDownload_jobQueue.Count;
                float totalProgress = ((float) downloadsCompleted / book.nInitialDownloadJobs) + (downloadJob.iiif_www.progress * singlejobValue);
                book.ui_bookAccess.image_loadingBar.fillAmount = totalProgress;
                Color newColor = book.ui_bookAccess.image_loadingBar.color;
                newColor = Color.Lerp(book.ui_bookAccess.image_loadingBar_hue_empty, book.ui_bookAccess.image_loadingBar_hue_full, totalProgress);
                newColor.a = book.ui_bookAccess.image_loadingBar.color.a;
                book.ui_bookAccess.image_loadingBar.color = newColor;
                //gameData.timeOfLastProgressUpdate = Time.time;
                //Debug.Log("Download Progress: " + (downloadJob.iiif_www.progress * 100).ToString("0") + "%");
            }
        }

        // Start next job in queue, if any
        if (gameData.imageDownload_currentJob == null && !gameData.allDownloadJobsFinished)
        {
            if (gameData.imageDownload_jobQueue.Count > 0)
            {
                gameData.imageDownload_currentJob = gameData.imageDownload_jobQueue.Peek();
                gameData.imageDownload_currentJob.iiif_www = new WWW(gameData.imageDownload_currentJob.targetUrl);
                Debug.Log("STARTING download job :: " + gameData.imageDownload_currentJob.targetPageCoordinate + " URL: " + gameData.imageDownload_currentJob.iiif_www.url);
            }
            else
            {
                // Detect all jobs finished
                gameData.allDownloadJobsFinished = true;
                book.ui_bookAccess.animator.Play("Book_UI_Access_Unlock", 1);

                // Update loading bar
                book.ui_bookAccess.image_loadingBar.fillAmount = 1;
                Color newColor = book.ui_bookAccess.image_loadingBar.color;
                newColor = book.ui_bookAccess.image_loadingBar_hue_full;
                newColor.a = book.ui_bookAccess.image_loadingBar.color.a;
                book.ui_bookAccess.image_loadingBar.color = newColor;
            }
        }
        #endregion // Image download job maintenance

        // Syncronize base book and transcription book
        for (int i = 0; i < book.worldRefs.baseMeshSkeleton_transforms.Length; i++)
        {
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localRotation = book.worldRefs.baseMeshSkeleton_transforms[i].localRotation;
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localPosition = book.worldRefs.baseMeshSkeleton_transforms[i].localPosition;
        }


        User user = gameData.user;
        Agent agent = user.agent;
        if (!user.agent.isViewingBook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            gameData.inputModule.m_cursorPos = Vector2.right * Screen.width / 2 + Vector2.up * Screen.height / 2;
            User_Intent userIntent = user.intent;
            #region /// Lateral movement intent
            Vector3 camFor = user.agent.camera.transform.forward;
            //Project the camera direction down onto a plane
            camFor -= (Vector3.Dot(camFor, Vector3.up) * Vector3.up);
            Quaternion camRot = Quaternion.LookRotation(camFor);

            userIntent.moveIntent = Vector3.zero;
            userIntent.moveInput = (Vector3.right * Input.GetAxis("Horizontal")) + (Vector3.forward * Input.GetAxis("Vertical"));
            userIntent.moveIntent = camRot * userIntent.moveInput;
            userIntent.moveIntent -= (Vector3.Dot(userIntent.moveIntent, Vector3.up) * Vector3.up);
            userIntent.moveIntent = userIntent.moveIntent.normalized * userIntent.moveInput.magnitude;
            #endregion

            #region // Player locomotion
            Vector3 acceleration = Vector3.zero;

            // Acceleration
            if(userIntent.moveIntent.magnitude > .1f)
            {
            float currentTargetSpeed = userIntent.moveIntent.magnitude * user.userParams.move_maxSpeed;
            float accelAmount = user.userParams.move_accel;
            Vector3 projectedVelocity = agent.rigidbody.velocity + (userIntent.moveIntent.normalized * accelAmount * Time.deltaTime);
            projectedVelocity = projectedVelocity.normalized * Mathf.Clamp(projectedVelocity.magnitude, 0, currentTargetSpeed);
            acceleration += (projectedVelocity - agent.rigidbody.velocity) / Time.deltaTime;
            }
            else
            {
                acceleration += -agent.rigidbody.velocity * user.userParams.move_decel;
            }

            // Velocity
            agent.rigidbody.velocity += acceleration * Time.deltaTime;

            //// Velocity
            //agent.velocity += acceleration * Time.deltaTime;

            //// Position
            //agent.currentPosition += agent.velocity * Time.deltaTime;
            //agent.transform.position = agent.currentPosition;
            #endregion // Player locomotion

            // Camera control
            agent.pitch_current += -Input.GetAxis("Mouse Y") * user.userParams.lookSensitivity;
            agent.pitch_current = Mathf.Clamp(agent.pitch_current, -70, 85);
            agent.yaw_current += Input.GetAxis("Mouse X") * user.userParams.lookSensitivity;
            agent.yaw_current %= 360;
            agent.camera.transform.eulerAngles = (Vector3.right * agent.pitch_current) + (Vector3.up * agent.yaw_current);
        }
    }

    public static void OpenBook()
    {
        GameData gameData = GameManager.gameDataInstance;

        Debug.Log("Opening book!");
        // Trigger book animation
        gameData.book.worldRefs.animator.Play("Opening",0);

        // Disable access UI
        gameData.book.ui_bookAccess.gameObject.SetActive(false);

        // User enter book view mode
    }

    public static void User_Enter_Book_Viewing_Mode(User user, Book book)
    {

    }

    public static void Book_Update_Page_Materials(Dictionary<IIIF_EntryCoordinate, Book_Entry> entries, Renderer[] pageRenderers, Renderer[] pageRenderers_transcription, int openRectoRendererIndex, IIIF_EntryCoordinate currentRectoEntry, Material fallbackMaterial)
    {
        IIIF_EntryCoordinate entryCoord;
        for (int i = 0; i < pageRenderers.Length; i++)
        {
            entryCoord = IIIF_EntryCoordinate.Translate(currentRectoEntry, i - openRectoRendererIndex);
            if (entries.ContainsKey(entryCoord))
            {
                pageRenderers[i].material = entries[entryCoord].material_base;
                pageRenderers_transcription[i].material = entries[entryCoord].material_transcription;
            }
            else
            {
                pageRenderers[i].material = fallbackMaterial;
            }
        }
    }
}