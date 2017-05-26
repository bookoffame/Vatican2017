using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;


public static partial class Methods
{

    public static void Main_Start(GameData gameData)
    {
        // Get the manifest describing our request
        WebClient client = new WebClient();
        // Download manifest into single string
        gameData.bookManifest.manifestString = client.DownloadString(gameData.book_manifestURL);
        // Parse string for page descriptions using a defined regex
        gameData.bookManifest.pageDescriptions = gameData.manifest_regex.Matches(gameData.bookManifest.manifestString);

        // Queue up downloads for each page described in the manifest
        IIIF_ImageRequestParams imageRequestParams = new IIIF_ImageRequestParams()
        {
            cropOffsetY = 210,
            cropWidth = 2900,
            cropHeight = 4000,
            targetWidth = 2900 / 2,
            targetHeight = 4000 / 2,
            rotation = 0,
            mirrored = false,
            quality = "default",
            format = ".jpg"
        };
        Book_Entry newPage;

        /// TODO (Stef) :: replace this assumption making hack (specifying desired page description indicies manually) with proper parsing of manifest to determine which page descriptions to download 
        IIIF_EntryCoordinate currentEntryCoordinate = new IIIF_EntryCoordinate() { isVerso = true, leafNumber = 81 };
        // 81 verso
        int descriptionIndex_first = 167;
        // 88 recto
        int descriptionIndex_last = 178;
        for (int i = descriptionIndex_first; i <= descriptionIndex_last; i++)
        {
            // creat a new book page
            newPage = new Book_Entry() { coordinate = currentEntryCoordinate, material_base = new Material(gameData.bookEntryBaseMaterial)};
            gameData.book_pages.Add(newPage.coordinate, newPage);

            // verso images get offset of 60, recto gets 175
            imageRequestParams.cropOffsetX = newPage.coordinate.isVerso ? 175 : 60;
            imageRequestParams.webAddress = Methods.IIIF_Remove_Tail_From_Web_Address(gameData.bookManifest.pageDescriptions[i].Groups[1].Value);

            // Create a download job
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

            // Increment current entry coord
            Debug.Log(currentEntryCoordinate.ToString());
            currentEntryCoordinate++;
        }
    }

    public static void Main_Update(GameData gameData)
    {
        #region // Image download job maintenance
        if (gameData.imageDownload_currentJob != null)
        {
            IIIF_ImageDownloadJob downloadJob = gameData.imageDownload_currentJob;
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
                    gameData.book_pages[downloadJob.targetPageCoordinate].pageImage_base = downloadJob.iiif_www.texture;
                    gameData.book_pages[downloadJob.targetPageCoordinate].material_base.mainTexture = gameData.book_pages[downloadJob.targetPageCoordinate].pageImage_base;
                    ImageTestPage_mono imageTest = GameObject.Instantiate(gameData.imageTestPrefab) as ImageTestPage_mono;
                    imageTest.transform.localScale = new Vector3((float)downloadJob.iiif_www.texture.width / (float)downloadJob.iiif_www.texture.height, 1, 1);
                    imageTest.gameObject.name = "Image Test - " + downloadJob.targetPageCoordinate;
                    imageTest.transform.position = (Vector3.up * 1.1f * (downloadJob.targetPageCoordinate.leafNumber - 81 - (downloadJob.targetPageCoordinate.isVerso ? 0 : 1))) + Vector3.right * .4f * (downloadJob.targetPageCoordinate.isVerso ? -1 : 1);
                    imageTest.renderer.material = gameData.book_pages[downloadJob.targetPageCoordinate].material_base;
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
