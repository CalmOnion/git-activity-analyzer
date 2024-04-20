using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class ProfileCmd(ConfigFile config) : Command<ProfileCmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var selectedAction = SelectAction(config);

		var _ = selectedAction switch
		{
			ProfileAction.Add => AddProfile(config),
			ProfileAction.Edit => EditProfile(config),
			ProfileAction.Remove => RemoveProfile(config),
			ProfileAction.MakeDefault => MakeProfileDefault(config),
			_ => 0,
		};

		ConfigUtils.SaveConfigFile(config);

		return _;
	}

	static ProfileAction SelectAction(ConfigFile config)
	{
		List<string> actions = [ProfileAction.Add.ToString()];
		if (config.Profiles.Count > 0)
		{
			actions.Add(ProfileAction.Edit.ToString());
			actions.Add(ProfileAction.Remove.ToString());
			actions.Add(ProfileAction.MakeDefault.ToString());
		}

		return Enum.Parse<ProfileAction>(AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select an action")
				.AddChoices(actions)
		));
	}

	static int AddProfile(ConfigFile config)
	{
		var name = AnsiConsole.Ask<string>("Enter the profile name");
		if (config.Profiles.Any(x => x.Name == name))
		{
			AnsiConsole.MarkupLine($"[red]Profile '{name}' already exists[/]");

			return -1;
		}

		config.Profiles.Add(new ConfigProfile { Name = name });

		return 1;
	}

	static int EditProfile(ConfigFile config)
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		var props = ConfigUtils.SelectPropertiesToEdit(["Name"]);

		if (props.Contains("Name"))
		{
			var name = AnsiConsole.Ask<string>("Enter the profile name");
			if (config.Profiles.Any(x => x.Name == name))
			{
				AnsiConsole.MarkupLine($"[red]Profile '{name}' already exists[/]");

				return -1;
			}

			selectedProfile.Name = name;
		}

		return 1;
	}

	static int RemoveProfile(ConfigFile config)
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		ConfigUtils.DisplayProfile(selectedProfile);

		bool confirm = AnsiConsole
			.Confirm($"Are you sure you want to remove profile '{selectedProfile.Name}'?");

		if (!confirm)
			return -1;

		config.Profiles.Remove(selectedProfile);

		if (config.Profiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[red]No profiles exist, creating a new default profile[/]");
			config.Profiles.Add(new ConfigProfile { Name = ConfigFile.DefaultProfileName });
			config.DefaultProfile = ConfigFile.DefaultProfileName;
		}
		else
		{
			if (config.DefaultProfile == selectedProfile.Name)
				config.DefaultProfile = config.Profiles[0].Name;
		}

		return 1;
	}

	static int MakeProfileDefault(ConfigFile config)
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		config.DefaultProfile = selectedProfile.Name;

		return 1;
	}

}


enum ProfileAction
{
	Add,
	Edit,
	Remove,
	MakeDefault
}