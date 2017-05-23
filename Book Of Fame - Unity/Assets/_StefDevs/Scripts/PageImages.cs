using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using AssemblyCSharp;

/// Presents the IIIF images from a manifest on 6 pages.

public class PageImages : MonoBehaviour
{
    /// The pages to present the IIIF images on.
    public Renderer[] pages;

    public ImageBufferer buffer;

    private IIIFImageGet iiifImage;

    /// Retrieve infomation about annotations on the pages.
    public Annotation[] annotation;


    /// The annotation drawers.
    public AnnotationDrawer[] drawers;


    /// The left transcription.
    public TranscriptionTool leftTrans;


    /// The right transcription.
    public TranscriptionTool rightTrans;


    /// The text that display the page number.
    public Text pageDisplay;

    public int start, end;


    /// The rotation required for each page.
    public int[] rotations;

    private ArrayList annotations;
    private int curr;
    private string transcription;

    // Use this for initialization
    void Start()
    {
        annotations = new ArrayList();
        iiifImage = ScriptableObject.CreateInstance<IIIFImageGet>();
        StartCoroutine(init());
    }

    private IEnumerator init()
    {
        for (int i = 0; i < 6; i++)
        {
            pages[i].enabled = true;
        }
        pages[0].material.mainTexture = buffer.GetImage(0, true);
        pages[1].material.mainTexture = buffer.GetImage(1, false);
        pages[2].material.mainTexture = buffer.GetImage(2, true);
        pages[3].material.mainTexture = buffer.GetImage(3, false);
        pages[4].material.mainTexture = buffer.GetImage(4, true);
        pages[5].material.mainTexture = buffer.GetImage(5, false);
        transcription = Resources.Load<TextAsset>("Transcriptions/anno").text;
        GotoPage(83);
        yield return new WaitUntil(() => true);
    }

    void Update()
    {
        pages[0].material.mainTexture = buffer.GetImage(0, true);
        pages[1].material.mainTexture = buffer.GetImage(1, false);
        pages[2].material.mainTexture = buffer.GetImage(2, true);
        pages[3].material.mainTexture = buffer.GetImage(3, false);
        pages[4].material.mainTexture = buffer.GetImage(4, true);
        pages[5].material.mainTexture = buffer.GetImage(5, false);
    }

    public bool OnPage(int pageNum)
    {
        return curr - 2 == pageNum;
    }

    public void GotoPage(int pageNum)
    {
        buffer.GotoPage(pageNum);
        curr = pageNum + 1;
        StartCoroutine(TurnPageLeft());
    }


    /// Shifts page's textures to the left and loads the next two pages.
    public IEnumerator TurnPageLeft()
    {
        if (curr < end)
        {
            buffer.TurnPageLeft();
            curr++;
            annotation[0].UpdateWebAddress(iiifImage.removeTail(buffer.GetURL(curr * 2 - 1)));
            annotation[1].UpdateWebAddress(iiifImage.removeTail(buffer.GetURL(curr * 2)));
            UpdateAnnotations();
            UpdatePageDisplay();
            yield return new WaitUntil(() => true);
        }
    }


    /// Shifts page's textures to the right and loads the previous two pages.
    public IEnumerator TurnPageRight()
    {
        if (curr > start)
        {
            buffer.TurnPageRight();
            curr--;
            annotation[0].UpdateWebAddress(iiifImage.removeTail(buffer.GetURL(curr * 2 - 1)));
            annotation[1].UpdateWebAddress(iiifImage.removeTail(buffer.GetURL(curr * 2)));
            UpdateAnnotations();
            UpdatePageDisplay();
            yield return new WaitUntil(() => true);
        }
    }


    /// Shows/Hides the annotations.
    public void ShowAnnotations(bool isShowing)
    {
        foreach (AnnotationDrawer d in drawers)
        {
            d.ShowAnnotations(isShowing);
            d.enabled = isShowing;
        }
    }


    /// Updates the annotations that are being drawn.
    public void UpdateAnnotations()
    {
        if (curr != 0)
            for (int i = 0; i < drawers.Length; i++)
            {
                drawers[i].UpdatesAnnotations(GetAnnotations(i));
            }
        else
            drawers[0].UpdatesAnnotations(GetAnnotations(0));

        Annotation.AnnotationBox empty;

        empty.contents = "";
        empty.x = 0;
        empty.y = 0;
        empty.w = 0;
        empty.h = 0;

        annotations = annotation[0].GetAnnotations(transcription, annotation[0].webAddress);
        leftTrans.UpdatesTranscriptions(GetAnnotationsBoxArray());

        annotations = annotation[1].GetAnnotations(transcription, annotation[1].webAddress);
        rightTrans.UpdatesTranscriptions(GetAnnotationsBoxArray());
    }

    public void AddNewAnnotation(int page, Annotation.AnnotationBox anno)
    {
        ButtonControls.current.changeSelected(ButtonControls.READER_TOOL);
        drawers[page].AddNewAnnotation(anno);
    }


    /// Gets the annotations for a specified page.
    public Annotation.AnnotationBox[] GetAnnotations(int which)
    {
        if (File.Exists(annotation[which].LocalAnnotationFile()))
            annotations = annotation[which].GetAnnotations(File.ReadAllText(annotation[which].LocalAnnotationFile()), annotation[which].webAddress);
        return GetAnnotationsBoxArray();
    }

    private Annotation.AnnotationBox[] GetAnnotationsBoxArray()
    {
        Annotation.AnnotationBox[] output = new Annotation.AnnotationBox[annotations.Count];
        for (int i = 0; i < output.Length; i++)
            output[i] = (Annotation.AnnotationBox)annotations[i];
        return output;
    }

    private void UpdatePageDisplay()
    {
        pageDisplay.text = (curr - 3).ToString() + "v : " + (curr - 2).ToString() + "r";
    }
}
