using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class AnalyzeCommitCmd(ConfigFile config, GitQuery query, AzureAiService ai) : Command<AnalyzeCommitCmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
		[CommandOption("-o|--outFile <OutFile>")]
		public string? OutFile { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var scope = ConfigUtils.SelectScope(config, false);
		if (scope is null)
			return -1;

		PickCommit(scope);

		return 1;
	}

	int PickCommit(RepositoryScope scope)
	{
		AnsiConsole.MarkupLine($"[bold]Analyzing commits from {scope.From:g} to {scope.To:g}[/] in [bold]{scope.Repositories.Single().Name}[/]");

		var commits = query.GetCommits(scope.From, scope.To, scope.Repositories.Single());

		var dict = commits.ToDictionary(
			c => $"[teal]{c.Date:dd-MM-yy hh:mm}[/] | [bold]{c.Message}[/]",
			c => c
		);

		if (dict.Count == 0)
		{
			AnsiConsole.MarkupLine("[red]No commits found[/]");

			return 0;
		}

		AnsiConsole.MarkupLine($"[bold]Found {dict.Count} commits[/]");

		var key = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select a commit to analyze")
				.PageSize(10)
				.MoreChoicesText("Scroll down for more commits")
				.AddChoices(dict.Keys)
		);

		var commit = dict[key];

		string? analysis = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("Thinking...", ctx =>
			{
				analysis = ai.ExplainCommit(commit);
			});

		if (analysis is null)
		{
			AnsiConsole.MarkupLine("[red]No analysis available[/]");
			return 0;
		}

		var table = new Table().Border(TableBorder.Rounded);
		table.AddColumn($"[teal]{commit.Date:dd-MM-yy hh:mm}[/] | [bold]{commit.Message}[/]");
		table.AddRow(analysis);
		table.Expand();

		AnsiConsole.Write(table);

		return 0;
	}

}
