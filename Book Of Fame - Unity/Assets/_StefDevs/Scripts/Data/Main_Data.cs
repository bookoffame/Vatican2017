using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace StefDevs
{
    [Serializable]
    public class GameData
    {
        public ImageTestPage_mono imageTestPrefab;
        public Material bookEntryBaseMaterial;
        public Material bookEntryBaseTranscriptionMaterial;
        internal string book_manifestURL = "http://www.e-codices.unifr.ch/metadata/iiif/fmb-cb-0048/manifest.json";

        public Book_mono book_mono;
        internal Book book;

        public AssetReferences assetReferences;
        internal User user;
        public Texture2D book_page_placeholderTexture;

        internal Regex manifest_regex = new Regex("\"@id\":\"([^\"]*?)\",\"@type\":\"dctypes:Image\"");

        public Queue<IIIF_ImageDownloadJob> imageDownload_jobQueue = new Queue<IIIF_ImageDownloadJob>();
        internal IIIF_ImageDownloadJob imageDownload_currentJob;

        internal float timeOfLastProgressUpdate;

        internal string LocalAnnotationFile
        {
            get
            {
                return Application.persistentDataPath + "/anno.json";
            }
        }


        internal IIIF_ImageRequestParams defaultImageRequestParams = new IIIF_ImageRequestParams()
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
    }


    [Serializable]
    public class TranscriptionRenderer
    {
        public GameObject gameObject;
        public Camera camera;
        public Canvas canvas;
    }

    [Serializable]
    public class TranscriptionRenderer_Annotation
    {
        public RectTransform transform;
        public TMPro.TMP_Text textMesh;
    }

    [Serializable]
    public struct IIIF_EntryCoordinate
    {
        //public enum PageLeafSide { RECTO, VERSO};
        public bool isVerso;
        public int leafNumber;
        //public PageLeafSide leafSide;

        public static IIIF_EntryCoordinate Translate(IIIF_EntryCoordinate coord, int amount)
        {
            coord.leafNumber += amount / 2;
            if (amount % 2 == 0)
                coord.isVerso = !coord.isVerso;
            return coord;
        }

        public static IIIF_EntryCoordinate operator ++(IIIF_EntryCoordinate coord)
        {
            if (coord.isVerso)
                coord.leafNumber++;

            coord.isVerso = !coord.isVerso;
            return coord;
        }

        public static IIIF_EntryCoordinate operator --(IIIF_EntryCoordinate coord)
        {
            if (!coord.isVerso)
                coord.leafNumber--;

            coord.isVerso = !coord.isVerso;
            return coord;
        }        

        public override string ToString()
        {
            return leafNumber + (isVerso ? "v" : "r");
        }
    }

    [Serializable]
    public class User_Camera
    {
        public Camera camera;
    }

    [Serializable]
    public class User
    {
        internal User_Camera playerCamera;
    }

    [Serializable]
    public class User_Params
    {
        public float moveSpeed;
    }

    [Serializable]
    public class AssetReferences
    {
        public User_Params_so userParams;
        public TranscriptionRenderer_mono transcription_rendererFab;
        public TranscriptionRenderer_Annotation_mono transcription_annotationFab;
    }

    [Serializable]
    public class Book
    {
        public Book_WorldRefs worldRefs;
        public int openRectoRendererIndex = 3;
        internal string manifestURL = "http://www.e-codices.unifr.ch/metadata/iiif/fmb-cb-0048/manifest.json";

        internal IIIF_Manifest manifest = new IIIF_Manifest();

        internal IIIF_EntryCoordinate minRectoEntry;
        internal IIIF_EntryCoordinate maxRectoEntry;
        internal IIIF_EntryCoordinate currentRectoEntry;
        internal List<IIIF_EntryCoordinate> currentlyAccessibleEntries = new List<IIIF_EntryCoordinate>();
        internal Dictionary<IIIF_EntryCoordinate, Book_Entry> entries = new Dictionary<IIIF_EntryCoordinate, Book_Entry>();
        internal Dictionary<IIIF_EntryCoordinate, IIIF_ImageRequestParams> page_imageRequestParams = new Dictionary<IIIF_EntryCoordinate, IIIF_ImageRequestParams>();
        public List<Book_Entry> entriesDebugList;
    }

    [Serializable]
    public class Book_Entry
    {
        internal IIIF_EntryCoordinate coordinate;
        internal Texture2D pageImage_base;
        internal Texture2D pageImage_transcription;
        internal Material material_base;
        internal Material material_transcription;
        internal IIIF_Transcription_Element[] transcriptionElements;
        // manifest
    }

    [Serializable]
    public class Book_WorldRefs
    {
        public Animator animator;
        public Renderer[] pageRenderers;
        public Book_PopupTheatre_mono popupTheatre;
    }

    [Serializable]
    public class Book_PopupTheatre
    {
        public GameObject gameObject;
        public Animator animator;
        public Transform popupCameraPos;
        public ParticleSystem smoke;
    }


    [Serializable]
    public class IIIF_Transcription_Element
    {
        internal string content;
        internal Rect boundingBox_normalizedInPageSpace;
    }

    [Serializable]
    public struct IIIF_ImageRequestParams
    {
        // The root web address to get the image from.
        public string webAddress;
        // The horizontal crop offset. -1 If not used.
        public int cropOffsetX;
        // The vertical crop offset.
        public int cropOffsetY;
        // The width of the crop.
        public int cropWidth;
        // The height of the crop.
        public int cropHeight;
        // The width of the target image.
        public int targetWidth;
        // The height of the target image.
        public int targetHeight;
        // Is the image reflected?.
        public bool mirrored;
        // The rotation of the image.
        public int rotation;
        // The quality of the image.
        public string quality;
        // The format of the image.
        public string format;
    }

    public class IIIF_Manifest
    {
        internal string manifestString;
        internal MatchCollection pageDescriptions;
    }

    public class IIIF_ImageDownloadJob
    {
        /// The image obtained from the IIIF server.
        internal IIIF_ImageRequestParams imageRequestParams;
        internal IIIF_EntryCoordinate targetPageCoordinate;
        internal Texture2D resultTexture;
        internal WWW iiif_www;
        internal string targetUrl;
    }

    [Serializable]
    public class IIIF_AnnotationManifestFromJSON
    {
        //public string context;
        //public string id;
        //public string type;
        public IIIF_AnnotationManifestFromJSON_AnnotationElement[] resources;
    }

    [Serializable]
    public class IIIF_AnnotationManifestFromJSON_AnnotationElement
    {
        //public string id;
        //public string type;
        public IIIF_AnnotationManifestFromJSON_AnnotationElement_Resource resource;
        public string on;
    }
    [Serializable]
    public class IIIF_AnnotationManifestFromJSON_AnnotationElement_Resource
    {
        //public string id;
        //public string type;
        public string chars;
        //public string language;
    }
}