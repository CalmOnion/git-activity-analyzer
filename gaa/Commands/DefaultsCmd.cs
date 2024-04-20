using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class DefaultsCmd(ConfigFile config) : Command<DefaultsCmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		var defaults = selectedProfile.Defaults;

		ConfigUtils.DisplayData(defaults.ToObfuscated(), $"Defaults -> {selectedProfile.Name}");

		var props = AnsiConsole.Prompt(
			new MultiSelectionPrompt<string>()
				.Title("Select a property to edit")
				.AddChoices(["Author", "Username", "Password"])
		).ToHashSet();

		if (props.Contains("Author"))
		{
			var author = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the author name:")
					.AllowEmpty()
			);

			defaults.Authors = string.IsNullOrWhiteSpace(author) ? null : [author];
		}
		if (props.Contains("Username"))
		{
			defaults.Username = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the username:")
					.AllowEmpty()
			);
		}
		if (props.Contains("Password"))
		{
			defaults.Password = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the password:")
					.AllowEmpty()
					.Secret()
			);
		}

		ConfigUtils.DisplayProfile(selectedProfile);
		ConfigUtils.SaveConfigFile(config);

		return 1;
	}
}
