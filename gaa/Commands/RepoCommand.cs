using CalmOnion.GAA.Config;
using Spectre.Console.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace CalmOnion.GAA.Commands;


public class RepoCommand(ConfigFile config) : Command<RepoCommand.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
		[CommandArgument(0, "<Url>")]
		public required string Url { get; set; }

		[CommandOption("--profile")]
		public string Profile { get; set; } = ConfigFile.DefaultProfile;

		[CommandOption("--author")]
		public string? Author { get; set; }

		[CommandOption("--username")]
		public string? Username { get; set; }

		[CommandOption("--password")]
		public string? Password { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		ConfigProfile? profile = ConfigUtils.GetOrCreateProfile(config, settings.Profile);
		if (profile is null)
			return -1;

		CreateOrOverwriteRepositoryInfo(profile, settings);

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	public static void CreateOrOverwriteRepositoryInfo(ConfigProfile profile, Settings settings, bool ask = true)
	{
		var existingRepo = profile.Repositories
			.FirstOrDefault(x => x.Url == settings.Url);

		RepositoryInfo newRepo = new()
		{
			Url = settings.Url,
			Author = settings.Author,
			Username = settings.Username,
			Password = settings.Password
		};

		if (existingRepo is not null)
		{
			// existing and new info is the same.
			if (existingRepo == newRepo)
				return;

			var rule = new Rule();
			AnsiConsole.Write(rule);
			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine($"Repository `{settings.Url}` already exists.");

			OutputJsonFromTo(existingRepo, newRepo);

			bool overwrite = AnsiConsole.Confirm("Overwrite?");
			if (overwrite == false)
				return;

			existingRepo.Author = settings.Author;
			existingRepo.Username = settings.Username;
			existingRepo.Password = settings.Password;
		}
		else
		{
			profile.Repositories.Add(newRepo);
		}

		AnsiConsole.WriteLine();
		OutputJsonToConsole(profile, $"Profile: {profile.Name} ");
	}

	protected static void OutputJsonToConsole(object data, string header = "")
	{
		var text = JsonSerializer.Serialize(data, ConfigUtils.jsonSerializerOptions);
		var json = new JsonText(text);

		var table = new Table().Border(TableBorder.Rounded);
		table.AddColumn(header);
		table.AddRow(json);

		AnsiConsole.Write(table);
	}

	protected static void OutputJsonFromTo(object from, object to)
	{
		var textFrom = JsonSerializer.Serialize(from, ConfigUtils.jsonSerializerOptions);
		var jsonFrom = new JsonText(textFrom);

		var textTo = JsonSerializer.Serialize(to, ConfigUtils.jsonSerializerOptions);
		var jsonTo = new JsonText(textTo);

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Current")
			.AddColumn("Overwritten")
			.AddRow(jsonFrom, jsonTo);

		AnsiConsole.Write(table);
	}

}