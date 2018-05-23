using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
			await LogInToLaunchpad(config.GetSection("launchpad"));
		}

		static HttpClient Client = new HttpClient();
		static async Task LogInToLaunchpad(IConfigurationSection config)
		{
			if ((config["oauth_token"] ?? "").Length > 0 && (config["oauth_token_secret"] ?? "").Length > 0) {
				return;
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
		}
	}
}
