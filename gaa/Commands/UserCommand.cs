using System.Text.Json;
using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Commands;

public class UserCommand(ConfigFile config) : Command<UserCommand.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
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
		ConfigProfile? profile = config.Profiles
			.FirstOrDefault(x => x.Name == settings.Profile);

		if (profile is null)
		{
			AnsiConsole.WriteLine();
			bool create = AnsiConsole.Confirm($"Profile {settings.Profile} does not exist. Create it?");
			if (create == false)
				return -1;

			profile = new ConfigProfile { Name = settings.Profile };
			config.Profiles.Add(profile);
		}

		if (settings.Author is not null)
			profile.Defaults.Author = settings.Author;

		if (settings.Username is not null)
			profile.Defaults.Username = settings.Username;

		if (settings.Password is not null)
			profile.Defaults.Password = settings.Password;

		ConfigUtils.SaveConfigFile(config);

		var text = JsonSerializer.Serialize(profile.Defaults, ConfigUtils.jsonSerializerOptions);
		var json = new JsonText(text);
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn(profile.Name)
			.AddRow(json);

		return 1;
	}
}
