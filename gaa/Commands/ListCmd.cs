using CalmOnion.GAA.Config;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class ListCmd(ConfigFile config) : Command<ListCmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var profiles = config.Profiles;

		var selectedProfile = ConfigUtils.SelectProfile(config, nullOption: "All");
		if (selectedProfile is not null)
			profiles = [selectedProfile];

		ConfigUtils.DisplayProfiles(profiles);

		return 1;
	}
}
