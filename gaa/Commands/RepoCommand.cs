using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;

public class RepoCommand : Command<RepoCommand.Settings>
{
	public class Settings : CommandSettings
	{
		[CommandArgument(0, "<Url>")]
		public string? Url { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		if (settings.Url is not null)
		{
			AnsiConsole.MarkupLine($"Adding repository [blue]{settings.Url}[/]");
		}

		return 0;
	}
}