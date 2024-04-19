using System.Text.Json;
using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

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

		return 1;
	}

	static int EditProfile(ConfigFile config)
	{

		return 1;
	}

	static int RemoveProfile(ConfigFile config)
	{

		return 1;
	}

	static int MakeProfileDefault(ConfigFile config)
	{
		throw new NotImplementedException();
	}

}


enum ProfileAction
{
	Add,
	Edit,
	Remove,
	MakeDefault
}