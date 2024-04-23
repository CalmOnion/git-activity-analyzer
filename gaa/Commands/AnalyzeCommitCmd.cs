using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class AnalyzeCommitCmd(ConfigFile config, GitQuery query)
: Command<AnalyzeCommitCmd.Settings>
{
	readonly ConfigFile config = config;
	AzureAiService? ai;

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

		if (scope.Profile.AzureOpenAi is null
			|| scope.Profile.AzureOpenAi.ApiKey is null
			|| scope.Profile.AzureOpenAi.Resource is null
			|| scope.Profile.AzureOpenAi.Deployment is null
		)
		{
			AnsiConsole.MarkupLine("[red]Azure OpenAI configuration is missing[/]");

			return -1;
		}

		ai = new AzureAiService(
			scope.Profile.AzureOpenAi.ApiKey,
			scope.Profile.AzureOpenAi.Resource,
			scope.Profile.AzureOpenAi.Deployment
		);

		PickCommit(scope);

		return 1;
	}

	int PickCommit(RepositoryScope scope)
	{
		if (ai is null)
			throw new InvalidOperationException("Azure AI service not initialized");

		AnsiConsole.MarkupLine($"[bold]Analyzing commits from {scope.From:g} to {scope.To:g}[/]"
			+ $" in [bold]{scope.Repositories.Single().Name}[/]");

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
			.Spinner(Spinner.Known.Dots)
			.Start("Thinking...", ctx =>
			{
				analysis = ai.ExplainCommit(
					commit,
					prompt: scope.Profile.Prompts.ExplanationPrompt,
					maxTokens: scope.Profile.Prompts.MaxTokens
				);
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
