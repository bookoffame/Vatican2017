using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text.RegularExpressions;
using BookOfFame;
using System.IO;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// TODO :: 
/// - Page drag
/// - book close/open
/// - Page turn flicker on finish
public static partial class Methods
{
    public static void Main_Initialize(ref GameState gameState, ref GameParams gameParams, SceneReferences sceneRefs)
    {
        Physics.autoSimulation = false;
        Physics.autoSyncTransforms = false;

        gameState.clock = new Clock();

        gameState.sceneReferences = sceneRefs;
        gameParams.assetReferences = gameParams.assetReferences_so.data;
        gameState.imageDownload_jobQueue = new Queue<IIIF_ImageDownloadJob>();

        #region // Init looking glass
        gameState.lookingGlass = sceneRefs.lookingGlass_mono.data;
        gameState.lookingGlass.isActive = false;
        gameState.lookingGlass.gameObject.SetActive(false);
        #endregion // Init looking glass

        #region // Init book
        gameState.book = sceneRefs.book_mono.data;

        Book book = gameState.book;
        book.worldRefs.transform = sceneRefs.book_mono.transform;

        gameState.book.firstEntry = new IIIF_EntryCoordinate() { isVerso = true, leafNumber = 81 };
        gameState.book.lastEntry = new IIIF_EntryCoordinate() { isVerso = false, leafNumber = 87 };

        #region // Get Manifest
        if (!gameParams.fetchNewManifest && gameParams.assetReferences.cachedManifest != null)
        {
            // Read manifest from that
            book.manifest.manifestString = gameParams.assetReferences.cachedManifest.text;
        }
        else
        {
            // Get the manifest describing our request
            WebClient client = new WebClient();
            // Download manifest into single string
            book.manifest.manifestString = client.DownloadString(GameParams.book_manifestURL);

            if (gameParams.fetchNewManifest)
            {
                string path = Application.dataPath + "Manifest.txt";
#if UNITY_EDITOR
                if (gameParams.assetReferences.cachedManifest != null)
                {
                    path = UnityEditor.AssetDatabase.GetAssetPath(gameParams.assetReferences.cachedManifest);
                }
#endif
                using (StreamWriter sw = new StreamWriter(path))
                {
                    // Add some text to the file.
                    sw.Write(book.manifest.manifestString);
                }
            }
        }
        #endregion // Get Manifest

        #region // Map page descriptions to entry coordinates
        // Parse string for page descriptions using a defined regex
        book.manifest.pageDescriptions = GameParams.manifest_regex.Matches(book.manifest.manifestString);
        Dictionary<IIIF_EntryCoordinate, string> entryWebAdresses = new Dictionary<IIIF_EntryCoordinate, string>();

        /// TODO (Stef) :: Current parsing of manifest does the job for now, but is probably fragile
        // Current pattern:
        // Find instance of an image file path and read to the left of the file extension for the page number and side
        // "@id":"http://www.e-codices.unifr.ch/loris/fmb/fmb-cb-0048/fmb-cb-0048_028v.jp2"
        //                                                                        ^  ^^- file extention index
        //                                                                        |  |- page side
        //                                                                        |- leaf number
        int index;
        string address;
        IIIF_EntryCoordinate matchCoord = new IIIF_EntryCoordinate();
        foreach (Match match in book.manifest.pageDescriptions)
        {
            address = Methods.IIIF_Remove_Tail_From_Web_Address(match.Groups[1].Value);
            index = address.IndexOf(".jp2");
            if (index < 0) continue;
            if (address[index - 1] == 'v')
                matchCoord.isVerso = true;
            else if (address[index - 1] == 'r')
                matchCoord.isVerso = false;
            else
                continue;

            matchCoord.leafNumber = uint.Parse(address.Substring(index - 4, 3));
            entryWebAdresses.Add(matchCoord, address);
        }
        #endregion // Map page descriptions to entry coordinates

        #region // Create book entries
        IIIF_ImageRequestParams imageRequestParams = IIIF_ImageRequestParams.Default;
        
        Book_Entry newPage;
        List<IIIF_Transcription_Element> transcriptionAnnotationList = new List<IIIF_Transcription_Element>();
        for (IIIF_EntryCoordinate entryCoord = gameState.book.firstEntry; entryCoord <= gameState.book.lastEntry; entryCoord++)
        {
            // creat a new entry
            newPage = new Book_Entry()
            {
                coordinate = entryCoord,
                material_base = new Material(gameParams.assetReferences.bookEntryBaseMaterial)
                {
                    name = "Placeholder Manuscript Mat - " + entryCoord
                },
                material_transcription = new Material(gameParams.assetReferences.bookEntryBaseTranscriptionMaterial)
                {
                    name = "Placeholder Transcription Mat - " + entryCoord
                },
            };

            // Add to indexed collection of entries
            book.entries.Add(newPage.coordinate, newPage);
            // Add to culminative list of entries (do we need this?)
            //book.currentlyAccessibleEntries.Add(newPage.coordinate);


            #region // Create and queue manuscript image download job

            #region // Create image request params for manuscript image
            // verso images get offset of 60, recto gets 175
            imageRequestParams.cropOffsetX = newPage.coordinate.isVerso ? 175 : 60;
            imageRequestParams.webAddress = entryWebAdresses[entryCoord];

            // Store request params for later use
            book.page_imageRequestParams.Add(newPage.coordinate, imageRequestParams);
            #endregion // Create image request params for manuscript image

            // Create a download job for the entry image
            IIIF_ImageDownloadJob downloadJob = new IIIF_ImageDownloadJob()
            {
                imageRequestParams = imageRequestParams,
                targetPageCoordinate = entryCoord,
                resultTexture = new Texture2D(imageRequestParams.targetWidth, imageRequestParams.targetHeight),
                targetUrl = IIIF_Determine_Web_Address_For_Image(imageRequestParams),
                // NOTE (Stef) :: Don't create www object until ready to start downloading
                iiif_www = null
            };

            // Add it to the job queue
            if (!gameParams.debug_onlyDLOnePage || entryCoord == gameState.book.firstEntry)
                gameState.imageDownload_jobQueue.Enqueue(downloadJob);
            #endregion 
        }
        book.nInitialDownloadJobs = gameState.imageDownload_jobQueue.Count;
        gameState.allDownloadJobsFinished = false;
        book.ui_bookAccess.button.interactable = false;
        #endregion // Create book entries

        #region // Get all transcription annotations
        // Load the transcription annotation manifest
        IIIF_AnnotationManifestFromJSON transcriptionManifest = new IIIF_AnnotationManifestFromJSON();
        JsonUtility.FromJsonOverwrite(gameParams.annotationsSource.text, transcriptionManifest);
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
                leafNumber = uint.Parse(coordString.Substring(0, 3))
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
                            // Convert y pos from top-down to bottom-up
                            (float)(imageRequestParams.cropHeight - numbers[1]) / imageRequestParams.cropHeight,
                            (float)numbers[2] / imageRequestParams.cropWidth,
                            (float)numbers[3] / imageRequestParams.cropHeight
                            )
                    }
                    );
        }
        #endregion // Get all transcription annotations

        #region // Generate transcription images
        RenderTexture renderTexture = new RenderTexture(imageRequestParams.targetWidth, imageRequestParams.targetHeight, 0);
        renderTexture.Create();
        RenderTexture.active = renderTexture;
        TranscriptionRenderer transcriptionRenderer = (GameObject.Instantiate(gameParams.assetReferences.transcription_rendererFab) as TranscriptionRenderer_mono).data;
        transcriptionRenderer.camera.targetTexture = renderTexture;
        List<GameObject> annotationUIObjects = new List<GameObject>();
        TranscriptionRenderer_Annotation newAnnotationUIElement;

        foreach (IIIF_EntryCoordinate key in transcriptionAnnotations.Keys)
        {
            List<IIIF_Transcription_Element> annotations = transcriptionAnnotations[key];

            // Build annotations on canvas
            foreach (IIIF_Transcription_Element annotation in annotations)
            {
                newAnnotationUIElement = (GameObject.Instantiate(gameParams.assetReferences.transcription_annotationFab, transcriptionRenderer.canvas.transform, false) as TranscriptionRenderer_Annotation_mono).data;
                newAnnotationUIElement.transform.offsetMin = Vector2.Scale(annotation.boundingBox_normalizedInPageSpace.min, transcriptionRenderer.canvas.pixelRect.size);
                newAnnotationUIElement.transform.offsetMax = Vector2.Scale(annotation.boundingBox_normalizedInPageSpace.max, transcriptionRenderer.canvas.pixelRect.size);
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

            ////Image test
            //ImageTestPage_mono imageTest = GameObject.Instantiate(gameParams.assetReferences.imageTestPrefab) as ImageTestPage_mono;
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


        book.leaves_pool = new Stack<Book_Leaf>();

        #region // Create book leaves
        book.firstAallowedLeaf = book.firstEntry.isVerso ? book.firstEntry.leafNumber : book.firstEntry.leafNumber - 1;
        book.lastAllowedLeaf = book.lastEntry.isVerso ? book.lastEntry.leafNumber + 1 : book.lastEntry.leafNumber;

        book.leaves_active_normal = new List<Book_Leaf>();
        book.leaves_active_transcription = new List<Book_Leaf>();

        Book_AddLeaf(book, gameParams.assetReferences, true);
        Book_AddLeaf(book, gameParams.assetReferences, false);
        #endregion // Create book leaves

        #endregion // Init book

        #region // Init user
        gameState.user = new User()
        {
            agent = sceneRefs.agentObject.data,
            userParams = gameParams.assetReferences.userParams.data,
            intent = new User_Intent()
        };

        gameState.user.agent.camera_control_locomotion.initialFoV = gameState.user.agent.camera_main.fieldOfView;

        Methods.User_Enter_Mode_Locomotion(gameState.user, gameState.book, gameState.lookingGlass, false);

        gameState.user.agent.camera_control_locomotion.pitch_current = gameState.user.agent.camera_main.transform.eulerAngles.x;
        gameState.user.agent.camera_control_locomotion.yaw_current = gameState.user.agent.camera_main.transform.eulerAngles.y;
        #endregion // Init user
    }    

    public static void Main_Update(ref GameState gameState, GameParams gameParams)
    {
        Book book = gameState.book;

        #region // Handle and clear incoming ui messages
        if ((gameState.uiEvents & UIEvents.OPEN_BOOK) != 0)
        {
            Methods.User_Enter_Mode_Book_Viewing(gameState.user, gameState.book);
        }
        if ((gameState.uiEvents & UIEvents.CLOSE_BOOK) != 0)
        {
            Methods.User_Enter_Mode_Locomotion(gameState.user, gameState.book, gameState.lookingGlass);
        }
        if ((gameState.uiEvents & UIEvents.NEXT_PAGE) != 0)
        {
            Methods.Book_TurnPage(gameState.book, gameParams.assetReferences, false);
        }
        if ((gameState.uiEvents & UIEvents.PREV_PAGE) != 0)
        {
            Methods.Book_TurnPage(gameState.book, gameParams.assetReferences, true);
        }
        // Clear UI events
        gameState.uiEvents = 0;
        #endregion // Handle incoming ui interactions

        #region // Image download job maintenance
        if (gameState.imageDownload_currentJob != null)
        {
            IIIF_ImageDownloadJob downloadJob = gameState.imageDownload_currentJob;
            Book_Entry targetEntry = book.entries[downloadJob.targetPageCoordinate];
            if (gameState.imageDownload_currentJob.iiif_www.isDone)
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

                    if (downloadJob.targetPageCoordinate.leafNumber >= book.leaves_active_normal[0].leafNumber
                        && downloadJob.targetPageCoordinate.leafNumber < book.leaves_active_normal[book.leaves_active_normal.Count - 1].leafNumber)
                    {
                        uint index = downloadJob.targetPageCoordinate.leafNumber - book.leaves_active_normal[0].leafNumber;
                        if (downloadJob.targetPageCoordinate.isVerso)
                        {
                            book.leaves_active_normal[(int)index].renderer_verso.material = targetEntry.material_base;
                            //book.leaves_active_transcription[(int)index].renderer_verso.material = targetEntry.material_transcription;
                        }
                        else
                        {
                            book.leaves_active_normal[(int)index].renderer_recto.material = targetEntry.material_base;
                            //book.leaves_active_transcription[(int)index].renderer_recto.material = targetEntry.material_transcription;
                        }
                    }

                    // Image test
                    //ImageTestPage_mono imageTest = GameObject.Instantiate(gameData.imageTestPrefab) as ImageTestPage_mono;
                    //imageTest.transform.localScale = new Vector3((float)downloadJob.iiif_www.texture.width / (float)downloadJob.iiif_www.texture.height, 1, 1);
                    //imageTest.gameObject.name = "Image Test - " + downloadJob.targetPageCoordinate;
                    //imageTest.transform.position = (Vector3.up * 1.1f * (downloadJob.targetPageCoordinate.leafNumber - 81 - (downloadJob.targetPageCoordinate.isVerso ? 0 : 1))) + Vector3.right * .4f * (downloadJob.targetPageCoordinate.isVerso ? -1 : 1);
                    //imageTest.renderer.material = book.entries[downloadJob.targetPageCoordinate].material_base;

                    // Save to disk
                    //byte[] bytes = downloadJob.iiif_www.texture.EncodeToPNG();
                    //System.IO.Stream stream = System.IO.File.Create(Application.dataPath + "/" + downloadJob.targetPageCoordinate.ToString() + ".png");
                    //stream.Write(bytes, 0, bytes.Length);
                    //stream.Dispose();
                }

                // Discard download job
                gameState.imageDownload_jobQueue.Dequeue();
                gameState.imageDownload_currentJob = null;
            }
            else
            {
                // Update loading bar
                float singlejobValue = 1f / book.nInitialDownloadJobs;
                int downloadsCompleted = book.nInitialDownloadJobs - gameState.imageDownload_jobQueue.Count;
                float totalProgress = ((float)downloadsCompleted / book.nInitialDownloadJobs) + (downloadJob.iiif_www.progress * singlejobValue);
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
        if (gameState.imageDownload_currentJob == null && !gameState.allDownloadJobsFinished)
        {
            if (gameState.imageDownload_jobQueue.Count > 0)
            {
                gameState.imageDownload_currentJob = gameState.imageDownload_jobQueue.Peek();
                gameState.imageDownload_currentJob.iiif_www = new WWW(gameState.imageDownload_currentJob.targetUrl);
                Debug.Log("STARTING download job :: " + gameState.imageDownload_currentJob.targetPageCoordinate + " URL: " + gameState.imageDownload_currentJob.iiif_www.url);
            }
            else
            {
                // Detect all jobs finished
                gameState.allDownloadJobsFinished = true;
                book.ui_bookAccess.button.interactable = true;
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

        #region // Page turning

        // Start page drag
        if (gameState.pageDragStartEvent.queued)
        {
            gameState.pageDragStartEvent.queued = false;
            if (!gameState.lookingGlass.isActive)
            {
                bool isLeftPage = gameState.pageDragStartEvent.mousePosition_viewport_x < gameState.pageDragStartEvent.panelCenterPosition_viewport_x;
                bool nextLeafIsAvailable = isLeftPage ? Book_Can_Turn_Page_Previous(gameState.book) : Book_Can_Turn_Page_Next(gameState.book);

                if (nextLeafIsAvailable)
                {
                    float panelCenterToMousePos = gameState.pageDragStartEvent.mousePosition_viewport_x - gameState.pageDragStartEvent.panelCenterPosition_viewport_x;
                    float distanceToComplete = Mathf.Abs(panelCenterToMousePos);
                    float minDistance = .25f;
                    if (distanceToComplete < minDistance)
                        distanceToComplete = minDistance;

                    gameState.book.pageDrag_mousePos_start_x = gameState.pageDragStartEvent.mousePosition_viewport_x;
                    gameState.book.pageDrag_mousePos_target_x = gameState.pageDragStartEvent.panelCenterPosition_viewport_x + (isLeftPage ? distanceToComplete : -distanceToComplete);

                    Book_AddLeaf(gameState.book, gameParams.assetReferences, isLeftPage);
                }
            }
        }

        if (gameParams.pageTurnTime <= 0) gameParams.pageTurnTime = .01f;
        uint nLeavesToRemove_left = 0;
        uint nLeavesToRemove_right = 0;
        // Move leaves towards target animation state
        for (int i = 0; i < book.leaves_active_normal.Count; i++)
        {
            Book_Leaf leaf = book.leaves_active_normal[i];
            double pageTurnSpeed = leaf.animState_current.clipDuration / (double)gameParams.pageTurnTime;

            if (leaf.isBeingDragged)
            {
                #region // Page Drag
                if (Input.GetMouseButton(0))
                {
                    book.pageDrag_progress = Mathf.InverseLerp(book.pageDrag_mousePos_start_x, book.pageDrag_mousePos_target_x, Input.mousePosition.x / Screen.width);

                    // Detect drag completion
                    if (book.pageDrag_progress >= 1)
                    {
                        // Initialize as at rest opposite of the side that it started on
                        Book_Leaf_InitializeAsAtRest(leaf, leaf.animState_current == leaf.animState_rightToLeft);
                    }
                    else
                    {
                        leaf.animState_current.targetTime = book.pageDrag_progress * leaf.animState_current.clipDuration;
                        leaf.animState_current.currentTime = leaf.animState_current.targetTime;
                        leaf.animState_current.playableClip.SetTime(leaf.animState_current.currentTime);
                    }
                }
                else
                {
                    // Target nearest side and let go of page
                    leaf.animState_current.targetTime = Mathf.Round(Mathf.Clamp01(book.pageDrag_progress)) * leaf.animState_current.clipDuration;
                    leaf.isBeingDragged = false;
                }
                #endregion
            }
            else if (!leaf.animState_current.atRest)
            {
                #region // Automatically turn page toward target
                //if (currentTime != leaf.pageTurnAnim_targetTime)
                {
                    // Move towards target
                    leaf.animState_current.currentTime = MoveTowards(leaf.animState_current.currentTime, leaf.animState_current.targetTime, pageTurnSpeed * Time.deltaTime);
                    leaf.animState_current.playableClip.SetTime(leaf.animState_current.currentTime);

                    // Detect page finished turning
                    if (leaf.animState_current.currentTime == leaf.animState_current.targetTime)
                    {
                        // Landing on left
                        if (leaf.animState_current == leaf.animState_leftToRight && leaf.animState_current.targetTime == 0
                         || leaf.animState_current == leaf.animState_rightToLeft && leaf.animState_current.targetTime == leaf.animState_current.clipDuration)
                        {
                            Book_Leaf_InitializeAsAtRest(leaf, true);
                            nLeavesToRemove_left++;
                        }
                        // Landing on right
                        else if (leaf.animState_current == leaf.animState_rightToLeft && leaf.animState_current.targetTime == 0
                              || leaf.animState_current == leaf.animState_leftToRight && leaf.animState_current.targetTime == leaf.animState_current.clipDuration)
                        {
                            Book_Leaf_InitializeAsAtRest(leaf, false);
                            nLeavesToRemove_right++;
                        }
                    }
                }
                #endregion // Automatically turn page toward target
            }
            // Push changes to leaf data on the heap
            book.leaves_active_normal[i] = leaf;
        }

        // Remove hidden leaves
        for (int i = 0; i < nLeavesToRemove_left; i++) Book_RemoveLeaf(book, true);
        for (int i = 0; i < nLeavesToRemove_right; i++) Book_RemoveLeaf(book, false);

        #endregion // Page turning

        Book_UpdatePageUI(gameState.book);

        #region // Syncronize base book and transcription book
        // Cover
        for (int i = 0; i < book.worldRefs.baseMeshSkeleton_transforms.Length; i++)
        {
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localRotation = book.worldRefs.baseMeshSkeleton_transforms[i].localRotation;
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localPosition = book.worldRefs.baseMeshSkeleton_transforms[i].localPosition;
        }
        // Leaves
        for (int i = 0; i < book.leaves_active_transcription.Count; i++)
        {
            Book_Leaf_SetCurrentAnimState(book.leaves_active_transcription[i], book.leaves_active_normal[i].animState_current == book.leaves_active_normal[i].animState_leftToRight);
            book.leaves_active_transcription[i].animState_current.playableClip.SetTime(book.leaves_active_normal[i].animState_current.currentTime);
        }
        #endregion // Syncronize base book and transcription book

        #region // User simulation
        User user = gameState.user;
        Agent agent = user.agent;

        #region // User Intent
        user.intent = User_Intent.emptyIntent;

        User_Intent newIntent = User_Intent.emptyIntent;
        User_Params userParams = user.userParams;

        #region /// Lateral movement intent
        Vector3 camFor = user.agent.camera_main.transform.forward;
        //Project the camera direction down onto a plane
        camFor -= (Vector3.Dot(camFor, Vector3.up) * Vector3.up);
        Quaternion camRot = Quaternion.LookRotation(camFor);

        newIntent.moveIntent_locomotion = Vector3.zero;
        newIntent.moveInput = (Vector3.right * Input.GetAxis("Horizontal")) + (Vector3.forward * Input.GetAxis("Vertical"));
        newIntent.moveIntent_locomotion = camRot * newIntent.moveInput;
        newIntent.moveIntent_locomotion -= (Vector3.Dot(newIntent.moveIntent_locomotion, Vector3.up) * Vector3.up);
        newIntent.moveIntent_locomotion = newIntent.moveIntent_locomotion.normalized * newIntent.moveInput.magnitude;
        #endregion

        #region // Bookview intent
        if (user.agent.isViewingBook)
        {
            // Zoom intent
            newIntent.zoomIntent = Input.mouseScrollDelta.y;
            // Move Intent
            newIntent.moveIntent_bookView = (Vector3.right * Input.GetAxis("Horizontal")) + (Vector3.up * Input.GetAxis("Vertical"));
            // Toggle looking glass
            newIntent.lookingGlass_toggle = Input.GetKeyDown(KeyCode.Space);
        }
        #endregion // Bookview intent

        user.intent = newIntent;

        #endregion // User Intent

        float fov;
        if (user.agent.isViewingBook)
        {
            #region // Looking Glass
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            gameState.sceneReferences.inputModule.m_cursorPos = Input.mousePosition;

            LookingGlass lookingGlass = gameState.lookingGlass;
            if (user.intent.lookingGlass_toggle)
            {
                if (lookingGlass.isActive)
                {
                    // Disable looking glass
                    lookingGlass.isActive = false;
                    lookingGlass.gameObject.SetActive(false);
                }
                else
                {
                    // Enable looking glass
                    lookingGlass.isActive = true;
                    lookingGlass.positioningPlane = new Plane(-gameState.book.worldRefs.transform.forward, gameState.book.worldRefs.transform.position - gameState.book.worldRefs.transform.forward * lookingGlass.distanceAboveBook);
                    // Reset current position
                    Vector3 glassStartPosition = gameState.book.worldRefs.transform.position
                        - gameState.book.worldRefs.transform.forward * lookingGlass.distanceAboveBook
                        - gameState.book.worldRefs.transform.up * .5f;
                    lookingGlass.transform.position = glassStartPosition;

                    // Reset target position
                    lookingGlass.position_target_worldSpace = lookingGlass.positioningPlane.ClosestPointOnPlane(gameState.book.worldRefs.cameraSocket.position);
                }
                lookingGlass.gameObject.SetActive(lookingGlass.isActive);
            }

            if (lookingGlass.isActive)
            {
                Camera refCamera = user.agent.camera_main;
                Ray mouseRay = refCamera.ScreenPointToRay(Input.mousePosition);
                // Detect dragging start
                if (!lookingGlass.isBeingDragged && Input.GetMouseButtonDown(0))
                {
                    Debug.DrawRay(mouseRay.origin, mouseRay.direction, Color.green, 10);
                    RaycastHit hit;
                    if (lookingGlass.collider.Raycast(mouseRay, out hit, 100))
                    {
                        lookingGlass.isBeingDragged = true;
                        float planeHitDistance;
                        if (lookingGlass.positioningPlane.Raycast(mouseRay, out planeHitDistance))
                        {
                            lookingGlass.position_selectionOffset = lookingGlass.transform.position - (mouseRay.origin + mouseRay.direction * planeHitDistance);
                        }
                    }
                }

                // Detect dragging stop
                if (lookingGlass.isBeingDragged && !Input.GetMouseButton(0))
                {
                    lookingGlass.isBeingDragged = false;
                }

                // Determine new target position
                if (lookingGlass.isBeingDragged)
                {
                    float planeHitDistance;
                    if (lookingGlass.positioningPlane.Raycast(mouseRay, out planeHitDistance))
                    {
                        lookingGlass.position_target_worldSpace = (mouseRay.origin + mouseRay.direction * planeHitDistance) + lookingGlass.position_selectionOffset;
                    }
                }

                // Move the glass to it's target position
                lookingGlass.transform.position = Vector3.Lerp(lookingGlass.transform.position, lookingGlass.position_target_worldSpace, lookingGlass.lerpFactor * Time.deltaTime);
            }
            #endregion // Looking Glass

            #region // Annotation raycast
            // If right click raycast currently open (target) left and right page
            #endregion // Annotation raycast

            #region // Camera control
            // Adjust target position offset
            agent.camera_control_bookViewing.positionOffset_target += (Vector3)newIntent.moveIntent_bookView * userParams.bookView_offset_sensitivity * Time.deltaTime;
            agent.camera_control_bookViewing.positionOffset_target.x = Mathf.Clamp(agent.camera_control_bookViewing.positionOffset_target.x, -userParams.bookView_offset_limit.x, userParams.bookView_offset_limit.x);
            agent.camera_control_bookViewing.positionOffset_target.y = Mathf.Clamp(agent.camera_control_bookViewing.positionOffset_target.y, -userParams.bookView_offset_limit.y, userParams.bookView_offset_limit.y);

            // Current position offset
            //agent.camera_control_bookViewing.positionOffset_current = Vector2.Lerp(agent.camera_control_bookViewing.positionOffset_current, agent.camera_control_bookViewing.positionOffset_target, userParams.bookView_offset_lerpSpeed);

            // Current position - offset from root
            agent.cameras_parent.localPosition = Vector3.Lerp(agent.cameras_parent.localPosition, agent.camera_control_bookViewing.positionOffset_target, userParams.bookView_offset_lerpSpeed * Time.deltaTime);

            // Adjust target zoom
            agent.camera_control_bookViewing.zoom_target = Mathf.Clamp01(agent.camera_control_bookViewing.zoom_target + newIntent.zoomIntent * userParams.bookView_zoom_sensitivity);
            // Move toward target zoom
            agent.camera_control_bookViewing.zoom_current = Mathf.Lerp(agent.camera_control_bookViewing.zoom_current, agent.camera_control_bookViewing.zoom_target, userParams.bookView_zoom_lerpSpeed * Time.deltaTime);
            // Current fov given current zoom
            fov = Mathf.Lerp(userParams.bookView_zoom_fov_max, userParams.bookView_zoom_fov_min, agent.camera_control_bookViewing.zoom_current);


            // Camera root rotation and position move toward current socket
            Vector3 newEulerAngles_parent = agent.cameras_parent.localEulerAngles;
            newEulerAngles_parent.x = Mathf.Lerp(newEulerAngles_parent.x, 0, Mathf.Clamp01(userParams.camera_toSocketLerpSpeed * Time.deltaTime));
            newEulerAngles_parent.y = Mathf.Lerp(newEulerAngles_parent.y, 0, Mathf.Clamp01(userParams.camera_toSocketLerpSpeed * Time.deltaTime));
            newEulerAngles_parent.z = 0;
            agent.cameras_parent.localEulerAngles = Vector3.zero;
            #endregion // Camera control
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
            gameState.sceneReferences.inputModule.m_cursorPos = Vector2.right * Screen.width / 2 + Vector2.up * Screen.height / 2;

            // Camera control
            agent.camera_control_locomotion.pitch_current += -Input.GetAxis("Mouse Y") * user.userParams.lookSensitivity;
            agent.camera_control_locomotion.pitch_current = Mathf.Clamp(agent.camera_control_locomotion.pitch_current, -70, 85);
            agent.camera_control_locomotion.yaw_current += Input.GetAxis("Mouse X") * user.userParams.lookSensitivity;
            agent.camera_control_locomotion.yaw_current %= 360;

            // Move towards initial fov
            fov = Mathf.Lerp(agent.camera_main.fieldOfView, agent.camera_control_locomotion.initialFoV, userParams.bookView_zoom_lerpSpeed * Time.deltaTime);
        }

        // Camera root rotation and position move toward current socket
        /// TODO (Stef) :: Transition camera root to new position
        {

            //Vector3 newEulerAngles = agent.cameras_root.localEulerAngles;
            //newEulerAngles.x = Mathf.Lerp(newEulerAngles.x, 0, Mathf.Clamp01(userParams.camera_toSocketLerpSpeed * Time.deltaTime));
            //newEulerAngles.y = Mathf.Lerp(newEulerAngles.y, 0, Mathf.Clamp01(userParams.camera_toSocketLerpSpeed * Time.deltaTime));
            //newEulerAngles.z = 0;
            agent.cameras_root.localEulerAngles = Vector3.zero;
        }

        //agent.cameras_root.localEulerAngles = Vector3.Slerp(agent.cameras_parent.localEulerAngles, Vector3.zero, userParams.camera_toSocketLerpSpeed * Time.deltaTime);
        agent.cameras_root.localPosition = Vector3.Lerp(agent.cameras_root.localPosition, Vector3.zero, userParams.camera_toSocketLerpSpeed * Time.deltaTime);

        // Apply current fov
        agent.camera_main.fieldOfView = fov;
        agent.camera_ui.fieldOfView = fov;
        agent.camera_transcription.fieldOfView = fov;
        #endregion // User simulation
    }

    public static void Main_FixedUpdate(ref GameState gameState, GameParams gameParams, float deltaTime)
    {
        //deltaTime = Mathf.Clamp(deltaTime, 0, gameParams.maxPhysicsTimeStep);
        Physics.Simulate(deltaTime);

        User user = gameState.user;
        Agent agent = user.agent;

        if (!user.agent.isViewingBook)
        {
#region // Player locomotion
            Vector3 acceleration = Vector3.zero;

            // Acceleration
            if (user.intent.moveIntent_locomotion.magnitude > .1f)
            {
                float currentTargetSpeed = user.intent.moveIntent_locomotion.magnitude * user.userParams.move_maxSpeed;
                float accelAmount = user.userParams.move_accel;
                Vector3 projectedVelocity = agent.rigidbody.velocity + (user.intent.moveIntent_locomotion.normalized * accelAmount * deltaTime);
                projectedVelocity = projectedVelocity.normalized * Mathf.Clamp(projectedVelocity.magnitude, 0, currentTargetSpeed);
                acceleration += (projectedVelocity - agent.rigidbody.velocity) / deltaTime;
            }
            else
            {
                acceleration += -agent.rigidbody.velocity * user.userParams.move_decel;
            }

            // Velocity
            agent.rigidbody.velocity += acceleration * deltaTime;
#endregion // Player locomotion

            // Camera direction application
            agent.cameras_parent.localEulerAngles = (Vector3.right * agent.camera_control_locomotion.pitch_current) + (Vector3.up * agent.camera_control_locomotion.yaw_current);
        }
    }

    public static void User_Enter_Mode_Book_Viewing(User user, Book book)
    {
        user.agent.isViewingBook = true;
        user.agent.cameras_root.SetParent(book.worldRefs.cameraSocket, true);
        user.agent.crosshair.enabled = false;
        book.ui_viewMode.gameObject.SetActive(true);
        // Disable access UI
        book.ui_bookAccess.gameObject.SetActive(false);
        // Trigger book animation
        AnimationPlayableUtilities.PlayClip(book.worldRefs.animator, book.closedToOpen, out book.playableGraph);
    }

    public static void User_Enter_Mode_Locomotion(User user, Book book, LookingGlass lookingGlass, bool playAnim = true)
    {
        user.agent.isViewingBook = false;
        user.agent.cameras_root.SetParent(user.agent.cameraSocket, true);
        user.agent.crosshair.enabled = true;
        book.ui_viewMode.gameObject.SetActive(false);
        // Enable access UI
        book.ui_bookAccess.gameObject.SetActive(true);

        // Disable looking glass
        lookingGlass.isActive = false;
        lookingGlass.gameObject.SetActive(false);


        if (playAnim)
            AnimationPlayableUtilities.PlayClip(book.worldRefs.animator, book.openToClosed, out book.playableGraph);


        //immediately complete any transitioning pages
    }

    public static void Book_TurnPage(Book book, AssetReferences assetRefs, bool directionIsLeft)
    {
        //if (directionIsLeft ? !Book_Can_Turn_Page_Previous(book) : !Book_Can_Turn_Page_Next(book))
        //    return;        

        Book_Leaf leaf = null;
        if (directionIsLeft)
        {
            // Search right to left for a leaf that is turning to the left or is resting on the left
            Book_Leaf l;
            for (int i = book.leaves_active_normal.Count - 1; i >= 0; i--)
            {
                l = book.leaves_active_normal[i];
                if (!l.animState_current.atRest && l.animState_current == l.animState_rightToLeft)
                {
                    leaf = l;
                    break;
                }
                else if (l.animState_current.atRest && l.animState_current == l.animState_leftToRight)
                {
                    leaf = l;
                    // Replace resting leaf on left
                    Book_AddLeaf(book, assetRefs, true);
                    break;
                }
            }
        }
        else
        {
            // Search left to right for a leaf that is turning to the right or is resting on the right
            Book_Leaf l;
            for (int i = 0; i < book.leaves_active_normal.Count; i++)
            {
                l = book.leaves_active_normal[i];
                if (!l.animState_current.atRest && l.animState_current == l.animState_leftToRight)
                {
                    leaf = l;
                    break;
                }
                else if (l.animState_current.atRest && l.animState_current == l.animState_rightToLeft)
                {
                    leaf = l;
                    // Replace resting leaf on right
                    Book_AddLeaf(book, assetRefs, false);
                    break;
                }
            }
        }

        if(leaf != null)
        {
            // Set the leaf's in motion towards target side
            leaf.animState_current.atRest = false;
            if (leaf.animState_current == leaf.animState_rightToLeft)
            {
                leaf.animState_current.targetTime = !directionIsLeft ? leaf.animState_current.clipDuration : 0;
            }
            else
            {
                leaf.animState_current.targetTime = directionIsLeft ? leaf.animState_current.clipDuration : 0;
            }
        }
    }

    public static bool Book_Can_Turn_Page_Previous(Book book)
    {
        return book.leaves_active_normal[0].leafNumber - 1 >= book.firstAallowedLeaf;
    }

    public static bool Book_Can_Turn_Page_Next(Book book)
    {
        return book.leaves_active_normal[book.leaves_active_normal.Count - 1].leafNumber + 1 <= book.lastAllowedLeaf;
    }

    public static void Book_UpdatePageUI(Book book)
    {
        // Turn off left and right buttons if we have hit boundary of available pages
        book.ui_viewMode.pageTurnButton_next.interactable = Methods.Book_Can_Turn_Page_Next(book);
        book.ui_viewMode.pageTurnButton_previous.interactable = Methods.Book_Can_Turn_Page_Previous(book);
        
        book.ui_viewMode.pageNumber_previous.text = book.leaves_active_normal[0].leafNumber.ToString() + "v";
        book.ui_viewMode.pageNumber_next.text = book.leaves_active_normal[book.leaves_active_normal.Count - 1].leafNumber.ToString() + "r";
    }

    public static void Book_AddLeaf(Book book, AssetReferences assetRefs, bool left)
    {
        uint leafNumber;
        if(book.leaves_active_normal.Count == 0)
            leafNumber = book.firstAallowedLeaf;
        else if (left)
            leafNumber = book.leaves_active_normal[0].leafNumber - 1;
        else
            leafNumber = book.leaves_active_normal[book.leaves_active_normal.Count - 1].leafNumber + 1;

        #region // Create
        Book_Leaf newLeaf_normal;
        {
            if (book.leaves_pool.Count > 0)
            {
                newLeaf_normal = book.leaves_pool.Pop();
            }
            else
            {
                newLeaf_normal = GameObject.Instantiate(assetRefs.bookLeafPrefab).data;
                Book_Leaf_InitializeAnimationGraph(newLeaf_normal, left);
            }
            newLeaf_normal.leafNumber = leafNumber;
            newLeaf_normal.gameObject.name = "Norm Leaf " + leafNumber;
            newLeaf_normal.gameObject.SetActive(true);
            newLeaf_normal.gameObject.transform.parent = book.worldRefs.leafParent_normal;
            newLeaf_normal.gameObject.transform.localPosition = Vector3.zero;
            newLeaf_normal.gameObject.transform.localRotation = Quaternion.identity;
            newLeaf_normal.gameObject.layer = book.worldRefs.leafParent_normal.gameObject.layer;
            foreach (Transform trans in newLeaf_normal.gameObject.GetComponentsInChildren<Transform>())
                trans.gameObject.layer = book.worldRefs.leafParent_normal.gameObject.layer;
            Book_Leaf_InitializeAsAtRest(newLeaf_normal, left);
        }

        Book_Leaf newLeaf_transcription;
        {
            if (book.leaves_pool.Count > 0)
            {
                newLeaf_transcription = book.leaves_pool.Pop();
            }
            else
            {
                newLeaf_transcription = GameObject.Instantiate(assetRefs.bookLeafPrefab, book.worldRefs.leafParent_transcription, false).data;
                Book_Leaf_InitializeAnimationGraph(newLeaf_transcription, left);
            }
            newLeaf_transcription.leafNumber = leafNumber;
            newLeaf_transcription.gameObject.name = "Trans Leaf " + leafNumber;
            newLeaf_transcription.gameObject.SetActive(true);
            newLeaf_transcription.gameObject.transform.parent = book.worldRefs.leafParent_transcription;
            newLeaf_transcription.gameObject.transform.localPosition = Vector3.zero;
            newLeaf_transcription.gameObject.transform.localRotation = Quaternion.identity;
            newLeaf_transcription.gameObject.layer = book.worldRefs.leafParent_transcription.gameObject.layer;
            foreach(Transform trans in newLeaf_transcription.gameObject.GetComponentsInChildren<Transform>())
                trans.gameObject.layer = book.worldRefs.leafParent_transcription.gameObject.layer;
            Book_Leaf_InitializeAsAtRest(newLeaf_transcription, left);
        }
        #endregion // Create 

        #region // Add to collection
        if (left)
        {        
            book.leaves_active_normal.Insert(0, newLeaf_normal);
            book.leaves_active_transcription.Insert(0, newLeaf_transcription);
        }
        else
        {
            book.leaves_active_normal.Add(newLeaf_normal);
            book.leaves_active_transcription.Add(newLeaf_transcription);
        }
        #endregion // Add to collection

        #region // Set materials;
        IIIF_EntryCoordinate coord = new IIIF_EntryCoordinate { leafNumber = leafNumber, isVerso = false };

        newLeaf_normal.renderer_recto.material = book.entries.ContainsKey(coord) ? book.entries[coord].material_base : assetRefs.bookEntryBaseMaterial;
        newLeaf_transcription.renderer_recto.material = book.entries.ContainsKey(coord) ? book.entries[coord].material_transcription : assetRefs.bookEntryBaseTranscriptionMaterial;

        coord.isVerso = true;
        newLeaf_normal.renderer_verso.material = book.entries.ContainsKey(coord) ? book.entries[coord].material_base : assetRefs.bookEntryBaseMaterial;
        newLeaf_transcription.renderer_verso.material = book.entries.ContainsKey(coord) ? book.entries[coord].material_transcription : assetRefs.bookEntryBaseTranscriptionMaterial;
        #endregion // Set materials;
    }

    public static void Book_RemoveLeaf(Book book, bool left)
    {
        int index = left ? 0 : book.leaves_active_normal.Count - 1;

        book.leaves_active_normal[index].gameObject.SetActive(false);
        book.leaves_pool.Push(book.leaves_active_normal[index]);
        book.leaves_active_normal.RemoveAt(index);

        book.leaves_active_transcription[index].gameObject.SetActive(false);
        book.leaves_pool.Push(book.leaves_active_transcription[index]);
        book.leaves_active_transcription.RemoveAt(index);
    }

    public static void Book_Leaf_InitializeAnimationGraph(Book_Leaf leaf, bool startOnLeft)
    {
        // Create graph
        leaf.animationGraph = PlayableGraph.Create();
        GraphVisualizerClient.Show(leaf.animationGraph, leaf.gameObject.name + " " + leaf.gameObject.transform.GetInstanceID());
        //leaf.animationGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        leaf.playableOutput = AnimationPlayableOutput.Create(leaf.animationGraph, "", leaf.animator);
        leaf.mixer = AnimationMixerPlayable.Create(leaf.animationGraph, 2);
        leaf.playableOutput.SetSourcePlayable(leaf.mixer);

        {
            leaf.animState_leftToRight = new PageTurnAnimationState
            {
                mixerIndex = 0,
                playableClip = AnimationClipPlayable.Create(leaf.animationGraph, leaf.animationClip_leftToRight),
                clipDuration = leaf.animationClip_leftToRight.length,
                currentTime = 0,
                targetTime = 0,
                atRest = true
            };
            leaf.animState_leftToRight.playableClip.SetPlayState(PlayState.Paused);
            leaf.animState_leftToRight.playableClip.SetDuration(leaf.animState_leftToRight.clipDuration);
            leaf.animationGraph.Connect(leaf.animState_leftToRight.playableClip, 0, leaf.mixer, leaf.animState_leftToRight.mixerIndex);
        }

        {
            leaf.animState_rightToLeft = new PageTurnAnimationState
            {
                mixerIndex = 1,
                playableClip = AnimationClipPlayable.Create(leaf.animationGraph, leaf.animationClip_rightToLeft),
                clipDuration = leaf.animationClip_rightToLeft.length,
                currentTime = 0,
                targetTime = 0,
                atRest = true
            };
            leaf.animState_rightToLeft.playableClip.SetPlayState(PlayState.Paused);
            leaf.animState_rightToLeft.playableClip.SetDuration(leaf.animState_rightToLeft.clipDuration);
            leaf.animationGraph.Connect(leaf.animState_rightToLeft.playableClip, 0, leaf.mixer, leaf.animState_rightToLeft.mixerIndex);
        }

        Book_Leaf_SetCurrentAnimState(leaf, startOnLeft);
        leaf.animationGraph.Play();
    }

    public static void Book_Leaf_SetCurrentAnimState(Book_Leaf leaf, bool left)
    {
        leaf.animState_current = left ? leaf.animState_leftToRight : leaf.animState_rightToLeft;
        leaf.mixer.SetInputWeight(0, left ? 1 : 0);
        leaf.mixer.SetInputWeight(1, left ? 0 : 1);
    }

    public static void Book_Leaf_InitializeAsAtRest(Book_Leaf leaf, bool left)
    {
        Book_Leaf_SetCurrentAnimState(leaf, left);
        leaf.animState_current.currentTime = 0;
        leaf.animState_current.playableClip.SetTime(leaf.animState_current.currentTime);
        leaf.animState_current.targetTime = 0;
        leaf.animState_current.atRest = true;
        leaf.isBeingDragged = false;
    }

    public static double MoveTowards(this double current, double target, double maxAbsDelta)
    {
        double delta = target - current;

        if (System.Math.Abs(delta) > maxAbsDelta)
            delta = maxAbsDelta * System.Math.Sign(delta);
        return current + delta;
    }
}