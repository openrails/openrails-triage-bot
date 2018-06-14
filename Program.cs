using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Open_Rails_Triage_Bot
{
	class Program
	{
		static void Main(string[] args)
		{
			var config = new CommandLineParser.Arguments.FileArgument('c', "config")
			{
				DefaultValue = new FileInfo("config.json")
			};

			var commandLineParser = new CommandLineParser.CommandLineParser()
			{
				Arguments = {
					config,
				}
			};

			try
			{
				commandLineParser.ParseCommandLine(args);

				AsyncMain(new ConfigurationBuilder()
					.AddJsonFile(config.Value.FullName, true)
					.Build()).Wait();
			}
			catch (CommandLineParser.Exceptions.CommandLineException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		static async Task AsyncMain(IConfigurationRoot config)
		{
			var launchpadConfig = config.GetSection("launchpad");

			var loggedIn = await LogInToLaunchpad(launchpadConfig);
			if (!loggedIn) {
				return;
			}

			var launchpad = new Launchpad.Cache(launchpadConfig["oauth_token"], launchpadConfig["oauth_token_secret"]);
			var project = await launchpad.GetProject($"https://api.launchpad.net/devel/{launchpadConfig["project"]}");

			foreach (var bugTask in await project.GetRecentBugTasks())
			{
				var bug = await bugTask.GetBug();
				var attachments = await bug.GetAttachments();

				Console.WriteLine($"{bugTask.Json.web_link} - {bug.Name}");

				if (IsBugCrashMissingLog(bug, bugTask, attachments))
				{
					await bug.AddUniqueMessage(
						"Automated response (ORTB-C1)",
						"Hello human, I am the Open Rails Triage Bot (https://github.com/openrails/openrails-triage-bot).\n" +
						"\n" +
						"It looks to me like you are reporting a crash in Open Rails, but I don't see a log file attached to this bug. To help my human friends diagnose what has gone wrong, it would be greatly appreciated if you could attach the complete 'OpenRailsLog.txt' file from your desktop to this bug.\n" +
						"\n" +
						"If you have provided the log file and I've missed it, don't worry - I won't ask about this again and the humans will know what to do.\n"
					);
				}
			}
		}

		static bool IsBugCrashMissingLog(Launchpad.Bug bug, Launchpad.BugTask bugTask, List<Launchpad.Attachment> attachments)
		{
			var now = DateTimeOffset.UtcNow;
			var crash = new Regex(@"\b(?:crash|crashes|crashed)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			var logContents = "This is a log file for Open Rails. Please include this file in bug reports.";
			var log = new Regex(@"\b(?:OpenRailsLog\.txt)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			return (now - bug.Created).TotalDays < 7 &&
				bugTask.Status == Launchpad.Status.New &&
				(crash.IsMatch(bug.Name) || crash.IsMatch(bug.Description)) &&
				!bug.Description.Contains(logContents) &&
				!attachments.Any(attachment => log.IsMatch(attachment.Name));
		}

		static HttpClient Client = new HttpClient();
		static async Task<bool> LogInToLaunchpad(IConfigurationSection config)
		{
			if ((config["oauth_token"] ?? "").Length > 0 && (config["oauth_token_secret"] ?? "").Length > 0) {
				return true;
			}

			var requestToken = await Client.PostAsync(
				"https://launchpad.net/+request-token",
				new FormUrlEncodedContent(new Dictionary<string, string> {
					{ "oauth_consumer_key", "Open Rails Triage Bot" },
					{ "oauth_signature_method", "PLAINTEXT" },
					{ "oauth_signature", "&" }
				})
			);
			var decodedRequest = HttpUtility.ParseQueryString(await requestToken.Content.ReadAsStringAsync());

			Console.WriteLine("Please open the following URL in a web browser logged in to the Launchpad account this bot should use:");
			Console.WriteLine();
			Console.WriteLine($"    https://launchpad.net/+authorize-token?oauth_token={decodedRequest["oauth_token"]}");
			Console.WriteLine();
			Console.WriteLine("Press enter once the authorization has been made inside Launchpad");
			Console.ReadLine();

			var accessToken = await Client.PostAsync(
				"https://launchpad.net/+access-token",
				new FormUrlEncodedContent(new Dictionary<string, string> {
					{ "oauth_token", decodedRequest["oauth_token"] },
					{ "oauth_consumer_key", "Open Rails Triage Bot" },
					{ "oauth_signature_method", "PLAINTEXT" },
					{ "oauth_signature", "&" + decodedRequest["oauth_token_secret"] }
				})
			);
			var decodedAccess = HttpUtility.ParseQueryString(await accessToken.Content.ReadAsStringAsync());

			Console.WriteLine();
			Console.WriteLine("Please record the following values in the configuration file:");
			Console.WriteLine();
			Console.WriteLine("{");
			Console.WriteLine("    \"launchpad\": {");
			Console.WriteLine($"        \"oauth_token\": \"{decodedAccess["oauth_token"]}\",");
			Console.WriteLine($"        \"oauth_token_secret\": \"{decodedAccess["oauth_token_secret"]}\"");
			Console.WriteLine("    }");
			Console.WriteLine("}");

			return false;
		}
	}
}
