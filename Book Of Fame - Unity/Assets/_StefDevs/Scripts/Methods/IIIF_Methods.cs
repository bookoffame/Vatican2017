using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

public static partial class Methods
{

    /// Get the url of a specified page.
    public static string IIIF_Get_URL_Of_Page(MatchCollection pages, int index)
    {
        return pages[index].Groups[1].Value;
    }

    public static string IIIF_Remove_Tail_From_Web_Address(string newAddress)
    {
        int remaining = 4;
        int index = newAddress.Length - 1;
        while (remaining > 0)
        {
            if (newAddress[index] == '/')
                remaining--;
            index--;
        }
        return newAddress.Substring(0, index + 1);
    }

    // Calculates the web address for the IIIF image with this IIIFImageGet's settings.
    public static string IIIF_Determine_Web_Address_For_Image(IIIF_ImageRequestParams imageRequestParams)
    {
        string location = imageRequestParams.webAddress;
        location = location.Insert(location.Length, "/");
        if (imageRequestParams.cropOffsetX == -1)
            location = location.Insert(location.Length, "full/");
        else
            location = location.Insert(location.Length, imageRequestParams.cropOffsetX.ToString() + ","
                + imageRequestParams.cropOffsetY.ToString() + "," + imageRequestParams.cropWidth.ToString() + "," + imageRequestParams.cropHeight.ToString() + "/");
        if (imageRequestParams.targetWidth == -1 && imageRequestParams.targetHeight == -1)
            location = location.Insert(location.Length, "full/");
        else
        {
            if (imageRequestParams.targetWidth != -1)
                location = location.Insert(location.Length, imageRequestParams.targetWidth.ToString());
            location = location.Insert(location.Length, ",");
            if (imageRequestParams.targetHeight != -1)
                location = location.Insert(location.Length, imageRequestParams.targetHeight.ToString());
            location = location.Insert(location.Length, "/");
        }
        if (imageRequestParams.mirrored)
            location = location.Insert(location.Length, "!");
        location = location.Insert(location.Length, imageRequestParams.rotation.ToString() + "/");
        location = location.Insert(location.Length, imageRequestParams.quality + imageRequestParams.format);
        return location;
    }
}

