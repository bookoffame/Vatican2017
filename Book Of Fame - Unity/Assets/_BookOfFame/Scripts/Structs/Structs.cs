﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.Playables;
using UnityEngine.Animations;


namespace BookOfFame
{
    [Serializable]
    public struct Clock
    {
        float time;
        UInt64 cycleCount;
    }

    [System.Flags]
    public enum UIEvents
    {
        CLOSE_BOOK = 1 << 0,
        OPEN_BOOK = 1 << 1,
        NEXT_PAGE = 1 << 2,
        PREV_PAGE = 1 << 3,
    }

    [Serializable]
    public struct GameState
    {
        internal Clock clock;
        internal Book book;
        internal LookingGlass lookingGlass;
        internal Queue<IIIF_ImageDownloadJob> imageDownload_jobQueue;
        internal IIIF_ImageDownloadJob imageDownload_currentJob;
        internal bool allDownloadJobsFinished;
        internal User user;
        internal UIEvents uiEvents;
        internal SceneReferences sceneReferences;
        internal PageDragStartEvent pageDragStartEvent;
        internal bool showHelp;
        internal bool showCredits;
    }

    public struct PageDragStartEvent
    {
        internal bool queued;
        internal float mousePosition_viewport_x;
        internal float panelCenterPosition_viewport_x;
    }

    [Serializable]
    public struct GameParams
    {
        public bool debug_onlyDLOnePage;
        //public float maxPhysicsTimeStep;
        public static Regex manifest_regex { get { return new Regex("\"@id\":\"([^\"]*?)\",\"@type\":\"dctypes:Image\""); } }
        public static string book_manifestURL = "http://www.e-codices.unifr.ch/metadata/iiif/fmb-cb-0048/manifest.json";
        public AssetReferences_so assetReferences_so;
        internal AssetReferences assetReferences;
        public float bookopenTime;
        public float pageTurnTime;
        public bool fetchNewManifest;
        public float pageDragMinProgress;
        public double pageTurnStartAndEndFudge;
    }

    [Serializable]
    public struct SceneReferences
    {
        public Book_mono book_mono;
        public Agent_mono agentObject;
        public LookingGlass_mono lookingGlass_mono;
        public VRInputModule inputModule;
        public RectTransform controlsPanel;
        public RectTransform creditsPanel;
    }

    [Serializable]
    public class AssetReferences
    {
        public User_Params_so userParams;
        public TextAsset annotationsSource;
        public TranscriptionRenderer_mono transcription_rendererFab;
        public TranscriptionRenderer_Annotation_mono transcription_annotationFab;
        public ImageTestPage_mono imageTestPrefab;
        public Material bookEntryBaseMaterial;
        public Material bookEntryBaseTranscriptionMaterial;
        public Texture2D book_page_placeholderTexture;
        public Book_Leaf_mono bookLeafPrefab;
        public TextAsset cachedManifest;
    }    

    [Serializable]
    public class User_Params
    {
        public float move_maxSpeed = 1;
        public float move_accel = 100;
        public float move_decel = 40;
        public float lookSensitivity = 2;
        public float bookView_zoom_fov_min = 40;
        public float bookView_zoom_fov_max = 60;
        public float bookView_zoom_lerpSpeed = 5;
        public float bookView_zoom_sensitivity = .1f;
        public Vector2 bookView_offset_limit = new Vector2(.3f,.3f);
        public float bookView_offset_sensitivity = 3;
        public float bookView_offset_lerpSpeed = 3;

        public float camera_toSocketLerpSpeed = 10;

    }

    [Serializable]
    public class User
    {
        internal Agent agent = new Agent();
        internal User_Intent intent = new User_Intent();
        internal User_Params userParams;
    }

    [Serializable]
    public struct User_Intent
    {
        internal static User_Intent emptyIntent;

        internal Vector3 moveInput;
        internal Vector3 moveIntent_locomotion;

        internal Vector2 moveIntent_bookView;
        internal float zoomIntent;
        internal bool turnPage_right;
        internal bool turnPage_left;

        internal bool lookingGlass_toggle;
    }

    [Serializable]
    public class Agent
    {
        public Camera camera_main;
        public Camera camera_ui;
        public Camera camera_transcription;
        public Transform cameras_root;
        public Transform cameras_parent;
        public Transform cameraSocket;
        public Transform transform;
        public SpriteRenderer crosshair;
        public Rigidbody rigidbody;
        internal bool isViewingBook;
        internal Agent_Camera_ControlData_LocomotionMode camera_control_locomotion = new Agent_Camera_ControlData_LocomotionMode();
        internal Agent_Camera_ControlData_ViewingMode camera_control_bookViewing = new Agent_Camera_ControlData_ViewingMode();
    }

    public class Agent_Camera_ControlData_LocomotionMode
    {
        internal float initialFoV;
        internal float pitch_current;
        internal float yaw_current;
    }

    public class Agent_Camera_ControlData_ViewingMode
    {
        internal float zoom_current;
        internal float zoom_target;
        internal Transform viewPointAnchor;
        internal Vector3 positionOffset_target;
        //internal Vector3 positionOffset_current;
    }

    [Serializable]
    public class LookingGlass
    {
        public Transform transform;
        public GameObject gameObject;
        public Collider collider;
        public float distanceAboveBook = .2f;
        public float lerpFactor = 3;
        
        internal Vector3 position_selectionOffset;
        internal Vector3 position_target_worldSpace;

        internal bool isActive;
        internal bool isBeingDragged;
        internal Plane positioningPlane ;
    }

    #region // Book
    [Serializable]
    public class Book
    {
        public Book_WorldRefs worldRefs;
        public Book_UI_BookAccess ui_bookAccess;
        public Book_UI_ViewModeUI ui_viewMode;
        public AnimationClip closedToOpen;
        public AnimationClip openToClosed;
        internal string manifestURL = "http://www.e-codices.unifr.ch/metadata/iiif/fmb-cb-0048/manifest.json";
        internal IIIF_Manifest manifest = new IIIF_Manifest();

        public IIIF_EntryCoordinate firstEntry;
        public IIIF_EntryCoordinate lastEntry;
        internal uint firstAallowedLeaf;
        internal uint lastAllowedLeaf;
        //internal List<IIIF_EntryCoordinate> currentlyAccessibleEntries = new List<IIIF_EntryCoordinate>();
        internal Dictionary<IIIF_EntryCoordinate, Book_Entry> entries = new Dictionary<IIIF_EntryCoordinate, Book_Entry>();
        // Currently visible entries
        public List<Book_Leaf> leaves_active_normal;
        internal List<Book_Leaf> leaves_active_transcription;
        internal Stack<Book_Leaf> leaves_pool;
        //internal Dictionary<uint, Book_Leaf> leaves = new Dictionary<uint, Book_Leaf>();
        internal Dictionary<IIIF_EntryCoordinate, IIIF_ImageRequestParams> page_imageRequestParams = new Dictionary<IIIF_EntryCoordinate, IIIF_ImageRequestParams>();
        internal List<Book_Entry> entriesDebugList;

        internal int nInitialDownloadJobs;
        //internal bool turnPageAnimationPlaying;
        internal float pageDrag_mousePos_target_x;
        internal float pageDrag_mousePos_start_x;
        internal float pageDrag_progress;

        internal PlayableGraph anim_graph;
        internal AnimationClipPlayable anim_playableClip;
        internal float anim_openAmount;
        internal bool anim_targetIsClosed;
        internal bool anim_changeStateQueued;
    }

    [Serializable]
    public class Book_WorldRefs
    {
        internal Transform transform;
        public Transform leafParent_normal;
        public Transform leafParent_transcription;
        public Transform leafParent_leftLeavesWhenClosed;
        public Animator animator;
        public Transform baseMeshSkeleton_root;
        public Transform transcriptionMeshSkeleton_root;
        internal Transform[] baseMeshSkeleton_transforms;
        internal Transform[] transcriptionMeshSkeleton_transforms;
        public Transform cameraSocket;        
    }

    [Serializable]
    public class Book_Leaf
    {
        public GameObject gameObject;
        public Animator animator;
        public Renderer renderer_verso;
        public Renderer renderer_recto;
        public AnimationClip animationClip_leftToRight;
        public AnimationClip animationClip_rightToLeft;
        internal PlayableGraph animationGraph;
        internal AnimationPlayableOutput playableOutput;
        internal AnimationMixerPlayable mixer;
        internal bool isBeingDragged;
        internal PageTurnAnimationState animState_leftToRight;
        internal PageTurnAnimationState animState_rightToLeft;
        internal PageTurnAnimationState animState_current;
        internal uint leafNumber;
        public bool debug_currentAnimStateIsLeftToRight;
    }

    [Serializable]
    public class PageTurnAnimationState
    {
        internal int mixerIndex;
        internal AnimationClipPlayable playableClip;
        internal double clipDuration;
        internal double currentTime;
        internal double targetTime;
        internal bool atRest;
    }

    [Serializable]
    public class Book_UI_ViewModeUI
    {
        public GameObject gameObject;
        public TMP_Text pageNumber_next;
        public TMP_Text pageNumber_previous;
        public TMP_Text pageNumber_current_recto;
        public TMP_Text pageNumber_current_verso;
        public Button[] buttonsToToggle;
        public Button pageTurnButton_next;
        public Button pageTurnButton_previous;
    }

    [Serializable]
    public class Book_UI_BookAccess
    {
        public GameObject gameObject;
        public Button button;
        public Animator animator;
        public Image image_loadingBar;
        public Color image_loadingBar_hue_empty;
        public Color image_loadingBar_hue_full;
        public Image image_lock;
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
    #endregion // Book

    #region // IIIF
    [Serializable]
    public struct IIIF_EntryCoordinate
    {
        //public enum PageLeafSide { RECTO, VERSO};
        public bool isVerso;
        public uint leafNumber;
        //public PageLeafSide leafSide;

        public static IIIF_EntryCoordinate Translate(IIIF_EntryCoordinate coord, int amount)
        {
            if (amount == 0) return coord;

            IIIF_EntryCoordinate result = coord;

            if (amount > 0)
            {
                for (int i = 0; i < amount; i++)
                {
                    //Debug.Log("adding");
                    result++;
                }
            }
            else if (amount < 0)
            {
                for (int i = amount; i < 0; i++)
                {
                    //Debug.Log("subtracting");
                    result--;
                }
            }
            return result;
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

        public static bool operator ==(IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            return coord1.isVerso == coord2.isVerso && coord1.leafNumber == coord2.leafNumber;
        }
        public static bool operator !=(IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            return coord1.isVerso != coord2.isVerso || coord1.leafNumber != coord2.leafNumber;
        }

        public static bool operator > (IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            if(coord1.leafNumber == coord2.leafNumber)
                return !coord1.isVerso && coord2.isVerso;
            else
                return coord1.leafNumber > coord2.leafNumber;
        }
        public static bool operator < (IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            if (coord1.leafNumber == coord2.leafNumber)
                return coord1.isVerso && !coord2.isVerso;
            else
                return coord1.leafNumber < coord2.leafNumber;
        }
        public static bool operator <=(IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            return coord1 == coord2 || coord1 < coord2;
        }
        public static bool operator >=(IIIF_EntryCoordinate coord1, IIIF_EntryCoordinate coord2)
        {
            return coord1 == coord2 || coord1 > coord2;
        }

        public override string ToString()
        {
            return leafNumber + (isVerso ? "v" : "r");
        }
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

        public static IIIF_ImageRequestParams Default = new IIIF_ImageRequestParams()
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
    #endregion // IIIF
}