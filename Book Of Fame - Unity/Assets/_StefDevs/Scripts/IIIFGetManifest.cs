using System;
using System.Net;
using System.Text.RegularExpressions;

namespace AssemblyCSharp
{
	/// <summary>
	/// Gets the manifest for a IIIF manuscript.
	/// </summary>
	public class IIIFGetManifest
	{
		private WebClient client;
		private Regex regex;
		private MatchCollection pages;

		public IIIFGetManifest ()
		{
			client = new WebClient ();
			regex = new Regex ("\"@id\":\"([^\"]*?)\",\"@type\":\"dctypes:Image\"");
			//regex = new Regex ("\"service\":(\\s|\\R)*{(\\s|\\R)*\"@id\":(\\s|\\R)*?\"([^\"]*?)");
		}

		/// <summary>
		/// Download the manifest from a specified url.
		/// </summary>
		/// <param name="url">The URL to download the IIIF manifest from.</param>
		public void download(string url)
		{
			string manifest = client.DownloadString (url);
			pages = regex.Matches (manifest);
		}

		/// <summary>
		/// Get the url of a specified page.
		/// </summary>
		/// <returns>The page url.</returns>
		/// <param name="index">The index of the page.</param>
		public string getPage(int index)
		{
			return pages [index].Groups [1].Value;
		}

		/// <summary>
		/// Gets the number of pages.
		/// </summary>
		/// <returns>The number of pages.</returns>
		public int getNumOfPages(){
			return pages.Count;
		}
	}
}

