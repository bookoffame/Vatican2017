using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

[Serializable]
public class GameData
{
    public ImageTestPage_mono imageTestPrefab;
    public Material bookEntryBaseMaterial;

    public AssetReferences assetReferences;
    internal User user;
    internal string book_manifestURL = "http://www.e-codices.unifr.ch/metadata/iiif/fmb-cb-0048/manifest.json";
    public Texture2D book_page_placeholderTexture;
    public int nPagesToBuffer;


    internal IIIF_Manifest bookManifest = new IIIF_Manifest();

    internal Regex manifest_regex = new Regex("\"@id\":\"([^\"]*?)\",\"@type\":\"dctypes:Image\"");

    public Queue<IIIF_ImageDownloadJob> imageDownload_jobQueue = new Queue<IIIF_ImageDownloadJob>();
    internal IIIF_ImageDownloadJob imageDownload_currentJob;
    public Dictionary<IIIF_EntryCoordinate, Book_Entry> book_pages = new Dictionary<IIIF_EntryCoordinate, Book_Entry>();
    internal float timeOfLastProgressUpdate;
}

[Serializable]
public struct IIIF_EntryCoordinate
{
    //public enum PageLeafSide { RECTO, VERSO};
    public bool isVerso;
    public int leafNumber;
    //public PageLeafSide leafSide;

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
}

public class Book_Entry
{
    public IIIF_EntryCoordinate coordinate;
    public Texture2D pageImage_base;
    public Texture2D pageImage_transcription;
    public Material material_base;
    // manifest


}


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
