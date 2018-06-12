using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage_Bot.Launchpad
{
	#pragma warning disable CS0649

	class JsonBugTaskCollection
	{
		public JsonBugTask[] entries;
		public string next_collection_link;
		public JsonBugTaskCollection(string url) => next_collection_link = url;
	}

	class JsonBugTask
	{
		public string self_link;
		public string bug_link;
	}

	#pragma warning restore CS0649

	public class BugTask
	{
		public async Task<Bug> GetBug() => await Cache.GetBug(Json.bug_link);

		internal readonly Cache Cache;
		internal readonly JsonBugTask Json;

		internal BugTask(Cache cache, JsonBugTask json) => (Cache, Json) = (cache, json);
	}
}
