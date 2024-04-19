using CalmOnion.GAA.Config;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;

public class AnalyzeCommand(ConfigFile config) : Command<AnalyzeCommand.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		return 1;
	}
}
