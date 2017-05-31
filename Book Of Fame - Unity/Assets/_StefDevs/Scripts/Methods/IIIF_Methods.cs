using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using StefDevs;

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

    public static IIIF_Transcription_Element[] IIIF_Annotations_For_IIIFEntry(string annotationManifest, string url, IIIF_ImageRequestParams imageRequestParams)
    {
        if (annotationManifest.Length > 1)
            annotationManifest = annotationManifest.Substring(1);

        Regex regex = new Regex("{(\\s|.)*?\"@type\": \"oa:Annotation\",(\\s|.)*?\"@type\": \"cnt:ContentAsText\"," +
                      "(\\s|.)*?\"chars\": \"([^\"]*?)\",(\\s|.)*?\"on\": \""
                      + Regex.Escape(url) + "#xywh=(\\d*?),(\\d*?),(\\d*?),(\\d*?)\"(\\s|.)*?}");

        List<IIIF_Transcription_Element> list = new List<IIIF_Transcription_Element>();

        foreach (string s in IIIF_GetContentBetweenBraces(annotationManifest))
        {
            if (s.Equals(annotationManifest))
                continue;
            MatchCollection matches = regex.Matches(s);
            foreach (Match m in matches)
            {
                list.Add(
                    new IIIF_Transcription_Element()
                    {
                        content = m.Groups[4].ToString(),
                        boundingBox_normalizedInPageSpace = new Rect(
                            (float)int.Parse(m.Groups[6].ToString()) / imageRequestParams.targetWidth,
                            (float)int.Parse(m.Groups[7].ToString()) / imageRequestParams.targetHeight,
                            (float)int.Parse(m.Groups[8].ToString()) / imageRequestParams.targetWidth,
                            (float)int.Parse(m.Groups[9].ToString()) / imageRequestParams.targetHeight
                            )
                    }
                    );
            }
        }
        return list.ToArray();
    }

    /// <returns>Each pair of "{" "}" braces.</returns>
    public static IEnumerable IIIF_GetContentBetweenBraces(string s)
    {
        int count = 0;

        //The position of the last "{" brace
        ArrayList last = new ArrayList();

        for (int i = 0; i < s.Length; i++)
        {
            //If we find a "{", add its position to the end of last
            if (s[i] == '{')
            {
                last.Add(i);
                count++;
            }
            //Else if we find a "}", group it with its corresponding "{" and return the string between them
            else if (s[i] == '}' && count > 0)
            {
                count--;
                int start = (int)last[count];
                last.RemoveAt(count);
                yield return s.Substring(start, i - start + 1);
            }
        }
    }
}

