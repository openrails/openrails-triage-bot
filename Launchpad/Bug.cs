using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Open_Rails_Triage_Bot.Launchpad
{
#pragma warning disable CS0649

	class JsonBug
	{
		public string self_link = "";
		public string title = "";
		public string description = "";
		public DateTimeOffset date_created;
		public string messages_collection_link = "";
		public string attachments_collection_link = "";
	}

#pragma warning restore CS0649

	public class Bug
	{
		public string Name => Json.title;
		public string Description => Json.description;
		public DateTimeOffset Created => Json.date_created;
		public async Task<List<Message>> GetMessages() => await Cache.GetMessageCollection(Json.messages_collection_link);
		public async Task<List<Attachment>> GetAttachments() => await Cache.GetAttachmentCollection(Json.attachments_collection_link);

		public async Task AddUniqueMessage(string name, string description)
		{
			var messages = await GetMessages();
			if (!messages.Any(message => message.Name == name))
			{
				AddMessage(name, description);
			}
		}

		void AddMessage(string name, string description) => Cache.Post(Json.self_link, new Dictionary<string, string> {
			{ "ws.op", "newMessage" },
			{ "subject", name },
			{ "content", description },
		});

		internal readonly Cache Cache;
		internal readonly JsonBug Json;

		internal Bug(Cache cache, JsonBug json) => (Cache, Json) = (cache, json);
	}
}
