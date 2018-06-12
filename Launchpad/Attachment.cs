using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage_Bot.Launchpad
{
	#pragma warning disable CS0649

	class JsonAttachmentCollection
	{
		public JsonAttachment[] entries;
		public string next_collection_link;
		public JsonAttachmentCollection(string url) => next_collection_link = url;
	}

	class JsonAttachment
	{
		public string self_link;
		public string title;
	}

	#pragma warning restore CS0649

	public class Attachment
	{
		public string Name => Json.title;

		internal readonly Cache Cache;
		internal readonly JsonAttachment Json;

		internal Attachment(Cache cache, JsonAttachment json) => (Cache, Json) = (cache, json);
	}
}
