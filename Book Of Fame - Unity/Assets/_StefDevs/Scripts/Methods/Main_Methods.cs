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

        //for (int i = -3; i < 3; i++)
        //{
        //    Debug.Log("Translated by " + i + ": " + IIIF_EntryCoordinate.Translate(new IIIF_EntryCoordinate() { leafNumber = 0, isVerso = false }, i));
        //}

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

            //// Flip verso pages horizontally and all pages vertically
            //if (newPage.coordinate.isVerso)
            //{
            //    newPage.material_base.mainTextureScale = new Vector2(-1, -1);
            //    newPage.material_transcription.mainTextureScale = new Vector2(-1, -1);
            //}
            //else
            //{
            //    newPage.material_base.mainTextureScale = new Vector2(1, -1);
            //    newPage.material_transcription.mainTextureScale = new Vector2(1, -1);
            //}

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
        book.ui_bookAccess.button.interactable = false;
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

        // Change out page images when page turn animation is complete
        Methods.Book_Update_Page_Materials(book);

        // Turn off left and right buttons if we have hit boundary of available pages
        book.ui_viewMode.pabeTurnButton_next.interactable = Methods.Book_Can_Turn_Page_Next(book);
        book.ui_viewMode.pabeTurnButton_previous.interactable = Methods.Book_Can_Turn_Page_Previous(book);

        #endregion // Init book

        #region // Init user
        gameData.user = new User()
        {
            agent = gameData.agentObject.data,
            userParams = gameData.assetReferences.userParams.data,
            intent = new User_Intent()
        };
        //gameData.user.agent.currentPosition = gameData.user.agent.transform.position;

        Methods.User_Enter_Mode_Locomotion(gameData.user, false);

        gameData.user.agent.camera_control_locomotion.pitch_current = gameData.user.agent.camera_main.transform.eulerAngles.x;
        gameData.user.agent.camera_control_locomotion.yaw_current = gameData.user.agent.camera_main.transform.eulerAngles.y;
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

        // Syncronize base book and transcription book
        for (int i = 0; i < book.worldRefs.baseMeshSkeleton_transforms.Length; i++)
        {
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localRotation = book.worldRefs.baseMeshSkeleton_transforms[i].localRotation;
            book.worldRefs.transcriptionMeshSkeleton_transforms[i].localPosition = book.worldRefs.baseMeshSkeleton_transforms[i].localPosition;
        }

        #region // Page turning
        if (book.turnPageAnimationPlaying)
        {
            // Detect book animation finishing
            if (book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).IsName("TurnPageRight") 
                || book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).IsName("TurnPageLeft_Reversed")
                || book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).IsName("TurnPageRight_Reversed")
                || book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).IsName("TurnPageLeft"))
            {
                bool animationFinished;
                //if (book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).speed > 0)
                    animationFinished = book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
                //else
                //    animationFinished = book.worldRefs.animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= 0;

                if (animationFinished)
                {
                    book.turnPageAnimationPlaying = false;

                    // Turn all the buttons back on
                    for (int i = 0; i < book.ui_viewMode.buttonsToToggle.Length; i++)
                    {
                        book.ui_viewMode.buttonsToToggle[i].interactable = true;
                    }

                    // Turn off left and right buttons if we have hit boundary of available pages
                    book.ui_viewMode.pabeTurnButton_next.interactable = Methods.Book_Can_Turn_Page_Next(book);
                    book.ui_viewMode.pabeTurnButton_previous.interactable = Methods.Book_Can_Turn_Page_Previous(book);

                    // Change out page images when page turn animation is complete
                    Methods.Book_Update_Page_Materials(book);

                    // Set book back to opened animation state
                    book.worldRefs.animator.Play("Opened", 0);
                }
            }
        }
        else if (book.pageDrag_isDragging)
        {
            // Continue drag
            book.pageDrag_progress = Mathf.InverseLerp(book.pageDrag_mousePos_start_x, book.pageDrag_mousePos_target_x, Input.mousePosition.x / Screen.width);
            string pageTurnAnimationName = book.pageDrag_isLeftPage ?  "TurnPageRight" : "TurnPageLeft";
            book.worldRefs.animator.Play(pageTurnAnimationName, 0, book.pageDrag_progress);

            bool mouseButtonDown = Input.GetMouseButton(0);

            float dragCompletionThreshold = .5f;
            // Detect drag ending
            if (book.pageDrag_progress >= 1)
            {
                book.pageDrag_isDragging = false;

                // Set animation state to opened
                book.worldRefs.animator.Play("Opened", 0);

                // Increment / decrement page number
                if (book.pageDrag_isLeftPage)
                   book.currentRectoEntry =  IIIF_EntryCoordinate.Translate(gameData.book.currentRectoEntry, -2);
                else
                    book.currentRectoEntry = IIIF_EntryCoordinate.Translate(gameData.book.currentRectoEntry, 2);

                // Update materials
                Methods.Book_Update_Page_Materials(book);

                // Turn off left and right buttons if we have hit boundary of available pages
                book.ui_viewMode.pabeTurnButton_next.interactable = Methods.Book_Can_Turn_Page_Next(book);
                book.ui_viewMode.pabeTurnButton_previous.interactable = Methods.Book_Can_Turn_Page_Previous(book);

            }
            else if (!mouseButtonDown)
            {
                book.pageDrag_isDragging = false;

                if (book.pageDrag_progress >= dragCompletionThreshold)
                {
                    // Play animation from wherever it is back to correct position
                    book.worldRefs.animator.Play(pageTurnAnimationName, 0, book.pageDrag_progress);
                    book.turnPageAnimationPlaying = true;
                    // Disable all buttons until page animation is complete
                    for (int i = 0; i < book.ui_viewMode.buttonsToToggle.Length; i++)
                        book.ui_viewMode.buttonsToToggle[i].interactable = false;

                    // Increment / decrement page number
                    if (book.pageDrag_isLeftPage)
                        book.currentRectoEntry = IIIF_EntryCoordinate.Translate(gameData.book.currentRectoEntry, -2);
                    else
                        book.currentRectoEntry = IIIF_EntryCoordinate.Translate(gameData.book.currentRectoEntry, 2);

                }
                else
                {
                    // Play animation IN REVERSE back to its original position
                    book.worldRefs.animator.Play(pageTurnAnimationName + "_Reversed", 0, 1 - book.pageDrag_progress);
                    book.turnPageAnimationPlaying = true;
                    // Disable all buttons until page animation is complete
                    for (int i = 0; i < book.ui_viewMode.buttonsToToggle.Length; i++)
                        book.ui_viewMode.buttonsToToggle[i].interactable = false;
                }
            }

        }
        #endregion // Page turning

        #region // User simulation

        User user = gameData.user;
        Agent agent = user.agent;

        #region // User Intent
        user.intent = gameData.emptyIntent;

        User_Intent newIntent = gameData.emptyIntent;
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
        }
        #endregion // Bookview intent

        user.intent = newIntent;

        #endregion // User Intent


        float fov;
        if (user.agent.isViewingBook)
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            gameData.inputModule.m_cursorPos = Input.mousePosition;


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

        }
        else
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
            gameData.inputModule.m_cursorPos = Vector2.right * Screen.width / 2 + Vector2.up * Screen.height / 2;



            // Camera control
            agent.camera_control_locomotion.pitch_current += -Input.GetAxis("Mouse Y") * user.userParams.lookSensitivity;
            agent.camera_control_locomotion.pitch_current = Mathf.Clamp(agent.camera_control_locomotion.pitch_current, -70, 85);
            agent.camera_control_locomotion.yaw_current += Input.GetAxis("Mouse X") * user.userParams.lookSensitivity;
            agent.camera_control_locomotion.yaw_current %= 360;

            // Move towards max fov
            fov = Mathf.Lerp(agent.camera_main.fieldOfView, userParams.bookView_zoom_fov_max, userParams.bookView_zoom_lerpSpeed * Time.deltaTime);
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

    public static void Main_FixedUpdate(GameData gameData)
    {
        User user = gameData.user;
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
                Vector3 projectedVelocity = agent.rigidbody.velocity + (user.intent.moveIntent_locomotion.normalized * accelAmount * Time.fixedDeltaTime);
                projectedVelocity = projectedVelocity.normalized * Mathf.Clamp(projectedVelocity.magnitude, 0, currentTargetSpeed);
                acceleration += (projectedVelocity - agent.rigidbody.velocity) / Time.fixedDeltaTime;
            }
            else
            {
                acceleration += -agent.rigidbody.velocity * user.userParams.move_decel;
            }

            // Velocity
            agent.rigidbody.velocity += acceleration * Time.fixedDeltaTime;
            #endregion // Player locomotion

            // Camera direction application
            agent.cameras_parent.localEulerAngles = (Vector3.right * agent.camera_control_locomotion.pitch_current) + (Vector3.up * agent.camera_control_locomotion.yaw_current);
        }

    }

    public static void User_Enter_Mode_Book_Viewing(User user, Book book)
    {
        GameData gameData = GameManager.gameDataInstance;
        user.agent.isViewingBook = true;
        user.agent.cameras_root.SetParent(book.worldRefs.cameraSocket, true);
        user.agent.crosshair.enabled = false;
        gameData.book.ui_viewMode.gameObject.SetActive(true);
        // Disable access UI
        gameData.book.ui_bookAccess.gameObject.SetActive(false);
        // Trigger book animation
        gameData.book.worldRefs.animator.Play("Opening", 0);
        gameData.book.turnPageAnimationPlaying = false;

    }

    public static void User_Enter_Mode_Locomotion(User user, bool playAnim = true)
    {
        GameData gameData = GameManager.gameDataInstance;
        user.agent.isViewingBook = false;
        user.agent.cameras_root.SetParent(user.agent.cameraSocket, true);
        user.agent.crosshair.enabled = true;
        gameData.book.ui_viewMode.gameObject.SetActive(false);
        // Enable access UI
        gameData.book.ui_bookAccess.gameObject.SetActive(true);


        if(playAnim)
            gameData.book.worldRefs.animator.Play("CloseState", 0);
        gameData.book.turnPageAnimationPlaying = false;
    }

    public static void Book_TurnPage(bool targetIsNext)
    {
        GameData gameData = GameManager.gameDataInstance;
        Book book = gameData.book;        
        IIIF_EntryCoordinate newRectoCoord = IIIF_EntryCoordinate.Translate(gameData.book.currentRectoEntry, targetIsNext  ? 2 : -2);
        if (!gameData.book.currentlyAccessibleEntries.Contains(newRectoCoord))
        {
            newRectoCoord = targetIsNext  ? gameData.book.maxRectoEntry : gameData.book.minRectoEntry;
        }
        if (newRectoCoord == gameData.book.currentRectoEntry) return;

        // Trigger page turn animation
        gameData.book.worldRefs.animator.Play(targetIsNext ? "TurnPageLeft" : "TurnPageRight", 0);
        gameData.book.turnPageAnimationPlaying = true;

        // Disable all buttons until page animation is complete
        for (int i = 0; i < book.ui_viewMode.buttonsToToggle.Length; i++)
            book.ui_viewMode.buttonsToToggle[i].interactable = false;

        // Change current recto entry
        book.currentRectoEntry = newRectoCoord;
    }

    public static bool Book_Can_Turn_Page_Previous(Book book)
    {
        IIIF_EntryCoordinate newRectoCoord = IIIF_EntryCoordinate.Translate(book.currentRectoEntry, -2);
        return book.currentlyAccessibleEntries.Contains(newRectoCoord);
    }

    public static bool Book_Can_Turn_Page_Next(Book book)
    {
        IIIF_EntryCoordinate newRectoCoord = IIIF_EntryCoordinate.Translate(book.currentRectoEntry, 2);
        return book.currentlyAccessibleEntries.Contains(newRectoCoord);
    }

    public static void Book_ChangePageToRectoPage(IIIF_EntryCoordinate newRectoPageCoord)
    {
        GameData gameData = GameManager.gameDataInstance;


    }

    public static void Start_Drag(GameData gameData, float mousePosition_viewport_x, float panelCenterPosition_viewport_x)
    {
        if (gameData.book.turnPageAnimationPlaying) return;

        bool isLeftPage = mousePosition_viewport_x < panelCenterPosition_viewport_x;
        if (isLeftPage && !Methods.Book_Can_Turn_Page_Previous(gameData.book)) return;
        if (!isLeftPage && !Methods.Book_Can_Turn_Page_Next(gameData.book)) return;

        float panelCenterToMousePos = mousePosition_viewport_x - panelCenterPosition_viewport_x;
        float distanceToComplete = Mathf.Abs(panelCenterToMousePos);
        float minDistance = .25f;
        if (distanceToComplete < minDistance)
            distanceToComplete = minDistance;
        float targetX = panelCenterPosition_viewport_x + (isLeftPage ? distanceToComplete : -distanceToComplete);

        gameData.book.pageDrag_isDragging = true;
        gameData.book.pageDrag_isLeftPage = isLeftPage;
        gameData.book.pageDrag_mousePos_start_x = mousePosition_viewport_x;
        gameData.book.pageDrag_mousePos_target_x = targetX;

        Debug.Log("Starting drag " + (isLeftPage ? "left" : "right"));
    }

    public static void Book_Update_Page_Materials(Book book)
    {

        // Determine whart images to place at each renderer
        IIIF_EntryCoordinate[] entryCoords = new IIIF_EntryCoordinate[book.worldRefs.pageRenderers.Length];
        
        //Debug.Log("Current: " + book.currentRectoEntry);
        //Debug.Log("OpenRectoRendererIndex: " + book.openRectoRendererIndex);
        int delta;
        for (int i = 0; i < book.worldRefs.pageRenderers.Length; i++)
        {
            delta = i - book.openRectoRendererIndex;
            entryCoords[i] = IIIF_EntryCoordinate.Translate(book.currentRectoEntry, delta);

            //Debug.Log("i: " + i + "   delta: " + delta + "   Result coord: " + entryCoords[i]);

            if (book.entries.ContainsKey(entryCoords[i]))
            {
                book.entries[entryCoords[i]].material_base.mainTextureScale = book.pageRenderers_textureScales[i];
                book.entries[entryCoords[i]].material_transcription.mainTextureScale = book.pageRenderers_textureScales[i];
                book.worldRefs.pageRenderers[i].material = book.entries[entryCoords[i]].material_base;
                book.worldRefs.pageRenderers_transcriptions[i].material = book.entries[entryCoords[i]].material_transcription;
            }
            else
            {
                book.worldRefs.pageRenderers[i].material = GameManager.gameDataInstance.bookEntryBaseMaterial;
                book.worldRefs.pageRenderers_transcriptions[i].material = GameManager.gameDataInstance.bookEntryBaseTranscriptionMaterial;
            }
        }

        // Update page numbers
        book.ui_viewMode.pageNumber_previous.text = entryCoords[1].ToString();
        book.ui_viewMode.pageNumber_current_verso.text = entryCoords[2].ToString();
        book.ui_viewMode.pageNumber_current_recto.text = entryCoords[3].ToString();
        book.ui_viewMode.pageNumber_next.text = entryCoords[4].ToString();
    }
}