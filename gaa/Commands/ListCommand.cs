using System.Text.Json;
using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Commands;


public class ListCommand(ConfigFile config) : Command<ListCommand.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
		[CommandOption("--profile")]
		public string? Profile { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Profiles");

		static JsonText profileToJsonText(ConfigProfile profile)
		{
			return profile.ToJsonText();
		}

		if (settings.Profile is not null)
		{
			ConfigProfile? profile = config.Profiles
				.FirstOrDefault(x => x.Name == settings.Profile);

			if (profile is null)
			{
				AnsiConsole.MarkupLine($"[red]Profile {settings.Profile} does not exist.[/]");

				return -1;
			}

			table.AddRow(profileToJsonText(profile));
		}
		else
		{
			config.Profiles.ForEach(profile => table.AddRow(profileToJsonText(profile)));
		}

		AnsiConsole.Write(table);

		return 1;
	}
}
