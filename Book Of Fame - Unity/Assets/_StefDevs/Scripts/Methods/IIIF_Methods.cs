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

    /* public static IIIF_Transcription_Element[] IIIF_Annotations_For_IIIFEntry(string annotationManifest, string url, IIIF_ImageRequestParams imageRequestParams)
    {
        if (annotationManifest.Length > 1)
            annotationManifest = annotationManifest.Substring(1);

        Regex regex = new Regex("{(\\s|.)*?\"@type\": \"oa:Annotation\",(\\s|.)*?\"@type\": \"cnt:ContentAsText\"," +
                      "(\\s|.)*?\"chars\": \"([^\"]*?)\",(\\s|.)*?\"on\": \""
                      + Regex.Escape(url) + "#xywh=(\\d*?),(\\d*?),(\\d*?),(\\d*?)\"(\\s|.)*?}");

        List<IIIF_Transcription_Element> list = new List<IIIF_Transcription_Element>();

        foreach (string s in IIIF_Annotation_Elements_Of_Annotation_Manifest_As_Strings(annotationManifest))
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
    */

    /* public static string[] IIIF_Annotation_Elements_Of_Annotation_Manifest_As_Strings(string manifest)
    {
        //string toReplace = (char)34 + "en" + (char)34 + "," + System.Environment.NewLine + "}";
        //string replaceWith = (char)34 + "en" + (char)34 + System.Environment.NewLine + "},";
        //string newString = manifest.Replace("@", "");
        //Debug.Log("Here we go");
        //IIIF_AnnotationManifestFromJSON newThing = new IIIF_AnnotationManifestFromJSON();
        //JsonUtility.FromJsonOverwrite(manifest, newThing);

        //return new string[0];

        // get all the positions of left brackets
        // pair lefts with rights
        List<int> leftBracketPositions = new List<int>();
        List<int> rightBracketPositions = new List<int>();


        // Skip first, find every second one
        bool skipNext = true;
        for (int i = 0; i < manifest.Length; i++)
        {
            if (manifest[i] == '{')
            {
                if(!skipNext)
                    leftBracketPositions.Add(i);
                skipNext = !skipNext;
            }
        }

        // get all the positions of right brackets in decrementing order
        // Skip first, find every second one
        skipNext = true;
        for (int i = manifest.Length - 1; i >= 0; i--)
        {
            if (manifest[i] == '}')
            {
                if(!skipNext)
                    rightBracketPositions.Add(i);
                skipNext = !skipNext;
            }
        }
        // Reverse it to aligh with left bracket array
        rightBracketPositions.Reverse();


        string[] contentItems = new string[leftBracketPositions.Count];
        int n = 0;
        int start;
        int end;
        int length;
        for (int i = 0; i < leftBracketPositions.Count; i ++)
        {
            start = leftBracketPositions[i] + 1;
            end = rightBracketPositions[i] - 1;
            length = end - start;
            contentItems[n] = manifest.Substring(start, length);
            n++;
        }

        return contentItems;
    }
    */
}

