using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace StarTrad.Tool
{
	/// <summary>
	/// Makes HTTP requests against the "traduction.circuspes.fr" website.
	/// </summary>
	internal class CircuspesClient
	{
		public const string HOST = "https://traduction.circuspes.fr";

		/// <summary>
		/// Asynchronously makes a GET request against the website.
		/// </summary>
		/// <param name="route"></param>
		/// <returns></returns>
		public static Task<string?> GetRequestAsync(string route)
		{
			return Task.Run(() => GetRequest(route));
		}

		/// <summary>
		/// Makes a GET request against the website.
		/// </summary>
		/// <param name="route"></param>
		/// <returns></returns>
		public static string? GetRequest(string route)
		{
			if (route[0] != '/') {
				route = '/' + route;
			}

			HttpWebRequest request;
			string? response = null;

			try {
				request = (HttpWebRequest)WebRequest.Create(HOST + route);
				CircuspesClient.AddUserAgentHeader(request.Headers);
			} catch (UriFormatException) {
				return null;
			}

			try {
				using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse()) {
					if (webResponse.StatusCode != HttpStatusCode.OK) {
						return null;
					}

					using (Stream stream = webResponse.GetResponseStream())
					using (StreamReader reader = new StreamReader(stream)) {
						response = reader.ReadToEnd();
					}
				}
			} catch (WebException) {
				return null;
			}

			return response;
		}

		public static void AddUserAgentHeader(WebHeaderCollection headers)
		{
			if (App.assemblyFileVersion != null && App.assemblyFileVersion.Length > 0) {
				headers.Add(HttpRequestHeader.UserAgent, "StarTrad/" + App.assemblyFileVersion);
			}
		}
	}
}
