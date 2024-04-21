using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class ConfigPathCmd : Command<ConfigPathCmd.Settings>
{
	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Config Path")
			.AddRow(new Markup($"[link bold teal]{ConfigUtils.path}[/]"));

		AnsiConsole.Write(table);

		return 0;
	}
}