using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;
using System.Net;

/// <summary>
/// Stores infomation about the current tool.
/// </summary>
public class ButtonControls : MonoBehaviour {
	/// <summary>
	/// The buttons for the tools.
	/// </summary>
	public Button[] buttons;

	/// <summary>
	/// The images for the tools.
	/// </summary>
	public Image[] images;

	/// <summary>
	/// The Popup window to use to get text input from the user.
	/// </summary>
	public PopUpBox popup;

	/// <summary>
	/// The camera for book mode
	/// </summary>
	public Move bookCam;

	/// <summary>
	/// The switcher between navigation mode and book mode cameras.
	/// </summary>
	public CameraSwitch switcher;

	/// <summary>
	/// The presenter of the IIIF images.
	/// </summary>
	public PageImages presenter;

	/// <summary>
	/// The box used to display tweets.
	/// </summary>
	public GameObject tweetBox;

	/// <summary>
	/// The text in the tweetBox containing the text of the tweet.
	/// </summary>
	public Text tweetText;

	/// <summary>
	/// The Inventory Box.
	/// </summary>
	public InventoryBox inventory;

	public bool minigameStarted;

	private int selected;
	private string popupText;
	private Regex tweetRegex;

	/// <summary>
	/// Is the spotlight on?
	/// </summary>
	public bool isSpotlight = false;

	/// <summary>
	/// The spotlight.
	/// </summary>
	public MoveSpotlight spotlight;

	/// <summary>
	/// The twitter bird.
	/// </summary>
	public GameObject twitterBirdObj;

	/// <summary>
	/// The twitter bird's image.
	/// </summary>
	public Image twitterBird;

	/// <summary>
	/// The twitter bird's idle image.
	/// </summary>
	public Sprite twitterBirdClosed;

	/// <summary>
	/// The twitter bird's talking image.
	/// </summary>
	public Sprite twitterBirdOpen;

	/// <summary>
	/// The sound to play when the twitter bird is talking.
	/// </summary>
	public AudioSource twitterSound;

	/// <summary>
	/// The dialog box to use to display text to the user.
	/// </summary>
	public DialogBox dialog;


	/// <summary>
	/// The current ButtonControls instance.
	/// </summary>
	public static ButtonControls current;

	/// <summary>
	/// The ID for the Light Tool.
	/// </summary>
	public const int LIGHT_TOOL = 0;

	/// <summary>
	/// The ID for the Annotation Tool.
	/// </summary>
	public const int ANNOTATION_TOOL = 1;

	/// <summary>
	/// The ID for the Hand Tool.
	/// </summary>
	public const int HAND_TOOL = 2;

	/// <summary>
	/// The ID for the Inventory Tool.
	/// </summary>
	public const int INVENTORY_TOOL = 3;

	/// <summary>
	/// The ID for the Display Annotations Tool.
	/// </summary>
	public const int READER_TOOL = 4;

	/// <summary>
	/// The ID for the Mini Game Lens Tool.
	/// </summary>
	public const int MINI_GAME_LENS_TOOL = 5;

	/// <summary>
	/// The ID for the Open/Close Book Tool.
	/// </summary>
	public const int SELECTION_TOOL = 6;

	/// <summary>
	/// The ID for the Help Tool.
	/// </summary>
	public const int HELP_TOOL = 7;

	/// <summary>
	/// The ID for the Transcription Tool.
	/// </summary>
	public const int LENS_TOOL = 8;

	/// <summary>
	/// The ID for the Twitter Tool.
	/// </summary>
	public const int TWITTER_TOOL = 9;

	private bool locked;

	// Use this for initialization
	void Start () {
		locked = false;
		minigameStarted = false;
		for (int i = 0; i < buttons.Length; i++)
			buttons [i].image.color = Color.cyan;
		for (int i = 0; i < images.Length; i++)
			images [i].color = new Color (0.3f,0.3f,0.3f,1);
		changeSelected(SELECTION_TOOL);
		current = this;
		tweetRegex = new Regex ("<div class=\"js-tweet-text-container\">\\s*?<p.*?>(.*?)<\\/p>\\s*?<\\/div>");
	}

	/// <summary>
	/// Causes a popup window to be display, prompting the user for input.
	/// </summary>
	public IEnumerator PopUp(){
		int old = selected;
		clearSelected ();
		bookCam.setActivated (false);
		switcher.gameObject.SetActive (false);
		popup.gameObject.SetActive (true);
		popup.reset ();
		yield return popup.StartCoroutine ("PopUp");
		popupText =  popup.getText ();
		popup.gameObject.SetActive (false);
		bookCam.setActivated (true);
		switcher.gameObject.SetActive (true);
		changeSelected(old);
	}

	/// <summary>
	/// Gets the text from the popup window.
	/// </summary>
	/// <returns>The text inputed into the popup window.</returns>
	public string getPopupText(){
		return popupText;
	}

	/// <summary>
	/// Gets the currently selected tool.
	/// </summary>
	/// <returns>The integer id of the currently selected tool.</returns>
	public int getSelected(){
		return selected;
	}


	/// <summary>
	/// Changes the currently selected tool.
	/// </summary>
	/// <param name="newSelected">The tool to select.</param>
	public void changeSelected(int newSelected){
		if (locked)
			return;
		switch (newSelected) {
		case LIGHT_TOOL:
			isSpotlight = !isSpotlight;
			if (isSpotlight)
				images [newSelected].color = new Color (1, 1, 1, 1);
			else
				images [newSelected].color = new Color (0.3f, 0.3f, 0.3f, 1);
			break;
				
		case INVENTORY_TOOL:
			if (minigameStarted)
				inventory.Show ();
			break;

		case READER_TOOL:
			presenter.ShowAnnotations (true);
			goto default;

		case HELP_TOOL:
			dialog.Show("Controls:" +
				"Left/Right Arrow Keys to move Left/Right.\n" +
				"Up/Down Arrow Keys to move Up/Down.\n" +
				"Z/X to zoom In/Out.\n" +
				"\n" +
				"Buttons:\n" +
				"Hand: Grab the pages with the cursor to turn to the next/previous page.\n" +
				"Magnify Glass: See a transcription of the text.\n" +
				"Note with \"A\" and +: Select a region to add an annotation to the local annotation file.\n" +
				"Note with \"A\": View local annotations.\n" +
				"Bird: Clicking on it shows/hides a bird. Click the bird to get the latest Tweet from our account.\n" +
				"?: Show this help dialog.");
			break;

		case TWITTER_TOOL:
			twitterBirdObj.SetActive (!twitterBirdObj.activeInHierarchy);
			if (twitterBirdObj.activeInHierarchy)
				images [newSelected].color = new Color (1, 1, 1, 1);
			else
				images [newSelected].color = new Color (0.3f, 0.3f, 0.3f, 1);
			break;

		default:
			clearLast ();
			if (selected != newSelected) {
				selected = newSelected;
				buttons [selected].image.color = Color.green;
				images [selected].color = new Color (1, 1, 1, 1);
			} else {
				selected = -1;
			}
			break;
		}

	}

	/// <summary>
	/// Makes no tool be selected.
	/// </summary>
	public void clearSelected(){
		clearLast ();
		selected = -1;
	}

	/// <summary>
	/// Shows the latest tweet from our twitter account.
	/// </summary>
	public void ShowLatestTweet(){
		WebClient client = new WebClient ();
		string data = client.DownloadString ("https://twitter.com/RealBookOfFame");
		MatchCollection tweets = tweetRegex.Matches (data);
		twitterBird.sprite = twitterBirdOpen;
		tweetText.text = tweets[0].Groups[1].Value;
		tweetBox.SetActive (true);
		twitterSound.Play ();
		StartCoroutine (HideTweetBox ());
	}

	/// <summary>
	/// Hides the tweet box.
	/// </summary>
	public IEnumerator HideTweetBox(){
		yield return new WaitForSeconds (5);
		twitterBird.sprite = twitterBirdClosed;
		tweetBox.SetActive (false);
	}

	public void setLocked(bool isLocked){
		locked = isLocked;
	}

	private void clearLast()
	{
		if (selected == READER_TOOL)
			presenter.ShowAnnotations (false);
		if (selected != -1) {
			buttons [selected].image.color = Color.cyan;
			images [selected].color = new Color (0.3f,0.3f,0.3f,1);
		}
	}
}
