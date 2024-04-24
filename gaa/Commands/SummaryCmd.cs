using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class SummaryCmd(ConfigFile config, GitQuery query)
: Command<SummaryCmd.Settings>
{
	readonly ConfigFile config = config;
	Settings settings = new();
	AzureAiService? ai;

	public class Settings : CommandSettings
	{
		[CommandOption("-o|--outFile <OutFile>")]
		public string? OutFile { get; init; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		this.settings = settings;

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

		SummarizeCommits(scope);

		return 1;
	}

	int SummarizeCommits(RepositoryScope scope)
	{
		if (ai is null)
			throw new InvalidOperationException("Azure AI service not initialized");

		AnsiConsole.MarkupLine($"[bold]Summarizing commits from {scope.From:g} to {scope.To:g}[/]"
			+ $" in [bold]{scope.Repositories.Single().Name}[/]");

		var commits = query.GetCommits(scope.From, scope.To, scope.Repositories.Single());

		var table = new Table()
			.Border(TableBorder.Minimal)
			.AddColumn("Date")
			.AddColumn("Message");

		foreach (var c in commits)
			table.AddRow($"[teal]{c.Date:dd-MM-yy hh:mm}[/]", $"[bold]{c.Message.EscapeMarkup()}[/]");

		AnsiConsole.WriteLine();
		AnsiConsole.Write(table);

		var confirm = AnsiConsole.Confirm("Do you want to create a summary from these commits?");
		if (!confirm)
			return 0;

		string? summary = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.Start("Thinking...", ctx =>
			{
				summary = ai.SummarizeCommits(
					new Dictionary<RepositoryInfo, ICollection<CommitInfo>>
					{
						[scope.Repositories.Single()] = commits.ToArray()
					},
					prompt: scope.Profile.Prompts.SummaryPrompt,
					maxTokens: scope.Profile.Prompts.MaxTokens
				);
			});

		if (summary is null)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[red]No summary available[/]");

			return 0;
		}

		table = new Table().Border(TableBorder.Minimal);
		table.AddColumn($"[teal bold]Summary[/]");
		table.AddRow(summary);

		AnsiConsole.WriteLine();
		AnsiConsole.Write(table);

		if (settings.OutFile is not null)
		{
			File.WriteAllText(settings.OutFile, summary);
			AnsiConsole.MarkupLine($"[bold]Summary saved to {settings.OutFile}[/]");
		}

		return 1;
	}

}
