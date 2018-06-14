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
		public string status;
		public string bug_link;
		public string web_link;
	}

	#pragma warning restore CS0649

	public enum Status
	{
		Unknown,
		New,
		Incomplete,
		Opinion,
		Invalid,
		WontFix,
		Expired,
		Confirmed,
		Triaged,
		InProgress,
		FixCommitted,
		FixReleased,
	}

	public class BugTask
	{
		static Dictionary<string, Status> StatusMapping = new Dictionary<string, Status>()
		{
			{ "New", Status.New },
			{ "Incomplete", Status.Incomplete },
			{ "Opinion", Status.Opinion },
			{ "Invalid", Status.Invalid },
			{ "Won't Fix", Status.WontFix },
			{ "Expired", Status.Expired },
			{ "Confirmed", Status.Confirmed },
			{ "Triaged", Status.Triaged },
			{ "In Progress", Status.InProgress },
			{ "Fix Committed", Status.FixCommitted },
			{ "Fix Released", Status.FixReleased },
		};

		public Status Status => StatusMapping[Json.status];
		public async Task<Bug> GetBug() => await Cache.GetBug(Json.bug_link);

		internal readonly Cache Cache;
		internal readonly JsonBugTask Json;

		internal BugTask(Cache cache, JsonBugTask json) => (Cache, Json) = (cache, json);
	}
}
