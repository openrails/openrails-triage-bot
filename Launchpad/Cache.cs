using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Open_Rails_Triage_Bot.Launchpad
{
	public class Cache
	{
		readonly string OAuthToken;
		readonly string OAuthTokenSecret;

		public Cache(string oauthToken, string oauthTokenSecret)
		{
			OAuthToken = oauthToken;
			OAuthTokenSecret = oauthTokenSecret;
		}

		AuthenticationHeaderValue GetAuthorizationHeader() {
			return new AuthenticationHeaderValue("OAuth", $"realm=\"https://api.launchpad.net/\",oauth_consumer_key=\"Open Rails Triage Bot\",oauth_token=\"{OAuthToken}\",oauth_signature_method=\"PLAINTEXT\",oauth_signature=\"%26{OAuthTokenSecret}\",oauth_timestamp=\"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\",oauth_nonce=\"0\",oauth_version=\"1.0\"");
		}

		HttpClient Client = new HttpClient();
		Dictionary<string, Project> Projects = new Dictionary<string, Project>();
		Dictionary<string, List<BugTask>> BugTaskCollections = new Dictionary<string, List<BugTask>>();
		Dictionary<string, BugTask> BugTasks = new Dictionary<string, BugTask>();
		Dictionary<string, Bug> Bugs = new Dictionary<string, Bug>();
		Dictionary<string, List<Message>> MessageCollections = new Dictionary<string, List<Message>>();
		Dictionary<string, Message> Messages = new Dictionary<string, Message>();
		Dictionary<string, List<Attachment>> AttachmentCollections = new Dictionary<string, List<Attachment>>();
		Dictionary<string, Attachment> Attachments = new Dictionary<string, Attachment>();

		internal async Task<T> Get<T>(string url)
		{
			var response = await Client.GetAsync(url);
			var text = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(text);
		}

		internal async Task Post(string url, Dictionary<string, string> data)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Authorization = GetAuthorizationHeader();
			request.Content = new FormUrlEncodedContent(data);

			Console.WriteLine($"{request.Method} {request.RequestUri}");
			foreach (var keyPair in data)
				Console.WriteLine($"{keyPair.Key} = {keyPair.Value}");

			// var response = await Client.SendAsync(request);
			// Console.WriteLine(response);
			// response.EnsureSuccessStatusCode();
		}

		public async Task<Project> GetProject(string url)
		{
			if (!Projects.ContainsKey(url))
				Projects[url] = new Project(this, await Get<JsonProject>(url));
			return Projects[url];
		}

		public async Task<List<BugTask>> GetBugTaskCollection(string url)
		{
			if (!BugTaskCollections.ContainsKey(url))
			{
				var collection = new List<BugTask>();
				var json = new JsonBugTaskCollection(url);
				do
				{
					json = await Get<JsonBugTaskCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(BugTask => FromJson(BugTask)));
				} while (json.next_collection_link != null);
				BugTaskCollections[url] = collection;
			}
			return BugTaskCollections[url];
		}

		internal BugTask FromJson(JsonBugTask json)
		{
			return BugTasks[json.self_link] = new BugTask(this, json);
		}

		public async Task<Bug> GetBug(string url)
		{
			if (!Bugs.ContainsKey(url))
				Bugs[url] = new Bug(this, await Get<JsonBug>(url));
			return Bugs[url];
		}

		public async Task<List<Message>> GetMessageCollection(string url)
		{
			if (!MessageCollections.ContainsKey(url))
			{
				var collection = new List<Message>();
				var json = new JsonMessageCollection(url);
				do
				{
					json = await Get<JsonMessageCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Message => FromJson(Message)));
				} while (json.next_collection_link != null);
				MessageCollections[url] = collection;
			}
			return MessageCollections[url];
		}

		internal Message FromJson(JsonMessage json)
		{
			return Messages[json.self_link] = new Message(this, json);
		}

		public async Task<List<Attachment>> GetAttachmentCollection(string url)
		{
			if (!AttachmentCollections.ContainsKey(url))
			{
				var collection = new List<Attachment>();
				var json = new JsonAttachmentCollection(url);
				do
				{
					json = await Get<JsonAttachmentCollection>(json.next_collection_link);
					collection.AddRange(json.entries.Select(Attachment => FromJson(Attachment)));
				} while (json.next_collection_link != null);
				AttachmentCollections[url] = collection;
			}
			return AttachmentCollections[url];
		}

		internal Attachment FromJson(JsonAttachment json)
		{
			return Attachments[json.self_link] = new Attachment(this, json);
		}
	}
}
