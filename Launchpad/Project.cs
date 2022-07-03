using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open_Rails_Triage_Bot.Launchpad
{
#pragma warning disable CS0649

	class JsonProject
	{
		public string self_link = "";
	}

#pragma warning restore CS0649

	public class Project
	{
		public Task<List<BugTask>> GetRecentBugTasks() => Cache.GetBugTaskCollection(Json.self_link + "?ws.op=searchTasks&status=New&status=Incomplete&status=Opinion&status=Invalid&status=Won't+Fix&status=Expired&status=Confirmed&status=Triaged&status=In+Progress&status=Fix+Committed&status=Fix+Released&modified_since=" + DateTime.UtcNow.AddDays(-7).ToString("s"));

		internal readonly Cache Cache;
		internal readonly JsonProject Json;

		internal Project(Cache cache, JsonProject json) => (Cache, Json) = (cache, json);
	}
}
