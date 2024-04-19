using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;

public class RemoveRepoCommand(ConfigFile config) : Command<RemoveRepoCommand.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
		[CommandOption("--profile")]
		public string? Profile { get; set; }
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		ConfigProfile? profile = config.Profiles.FirstOrDefault(x => x.Name == settings.Profile);

		if (profile is null)
		{
			var availableProfileNames = config.Profiles
				.FindAll(p => p.Repositories.Count > 0)
				.Select(x => x.Name);

			if (!availableProfileNames.Any())
			{
				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine("[red]No profiles with repositories found.[/]");

				return -1;
			}

			var profileName = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("Select a profile")
					.PageSize(10)
					.MoreChoicesText("More")
					.AddChoices(availableProfileNames)
			);

			profile = config.Profiles.First(x => x.Name == profileName);
		}

		List<RepositoryInfo> repos = profile.Repositories;

		if (repos.Count == 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[red]Profile: {profile.Name} does not have any repositories.[/]");
			return -1;
		}

		var reposToDelete = AnsiConsole.Prompt(
			new MultiSelectionPrompt<string>()
				.Title("Select repositories to remove")
				.PageSize(10)
				.MoreChoicesText("More")
				.AddChoices(repos.Select(x => x.Url))
		);

		foreach (var repoUrl in reposToDelete)
		{
			RepositoryInfo? repo = repos.FirstOrDefault(x => x.Url == repoUrl);
			if (repo is not null)
				repos.Remove(repo);
		}

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}
}
