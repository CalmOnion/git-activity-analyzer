using System.Text.Json;
using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Commands;


public class RepositoryCmd(ConfigFile config) : Command<RepositoryCmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		var selectedAction = SelectAction(selectedProfile);
		var _ = selectedAction switch
		{
			RepoAction.Add => AddRepo(selectedProfile),
			RepoAction.Edit => EditRepo(selectedProfile),
			RepoAction.Remove => RemoveRepo(selectedProfile),
			_ => 0,
		};

		ConfigUtils.SaveConfigFile(config);

		return _;
	}

	static RepoAction SelectAction(ConfigProfile profile)
	{
		List<string> actions = [RepoAction.Add.ToString()];
		if (profile.Repositories.Count > 0)
		{
			actions.Add(RepoAction.Edit.ToString());
			actions.Add(RepoAction.Remove.ToString());
		}

		return Enum.Parse<RepoAction>(AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select an action")
				.AddChoices(actions)
		));
	}

	static int AddRepo(ConfigProfile profile)
	{
		var url = AnsiConsole.Ask<string>("Enter the repository URL");

		if (profile.Repositories.Any(x => x.Url == url))
		{
			AnsiConsole.MarkupLine($"[red]Repository '{url}' already exists[/]");

			return -1;
		}

		var author = AnsiConsole.Prompt(
			new TextPrompt<string>("Enter the author name:")
				.AllowEmpty()
		);
		var username = AnsiConsole.Prompt(
			new TextPrompt<string>("Enter the username:")
				.AllowEmpty()
		);
		var password = AnsiConsole.Prompt(
			new TextPrompt<string>("Enter the password:")
				.AllowEmpty()
				.Secret()
		);

		profile.Repositories.Add(new RepositoryInfo
		{
			Url = url,
			Author = string.IsNullOrWhiteSpace(author) ? null : author,
			Username = string.IsNullOrWhiteSpace(username) ? null : username,
			Password = string.IsNullOrWhiteSpace(password) ? null : password,
		});

		return 1;
	}

	static int EditRepo(ConfigProfile profile)
	{
		var repo = ConfigUtils.SelectRepository(profile);
		if (repo is null)
			return 0;

		ConfigUtils.DisplayData(repo.ToObfuscated(), "Current Repository Info");

		var props = ConfigUtils
			.SelectPropertiesToEdit(["Author", "Username", "Password"]);

		if (props.Contains("Author"))
		{
			var author = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the author name:")
					.AllowEmpty()
			);

			repo.Author = string.IsNullOrWhiteSpace(author) ? null : author;
		}

		if (props.Contains("Username"))
		{
			var username = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the username:")
					.AllowEmpty()
			);

			repo.Username = string.IsNullOrWhiteSpace(username) ? null : username;
		}

		if (props.Contains("Password"))
		{
			var password = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the password:")
					.AllowEmpty()
					.Secret()
			);

			repo.Password = string.IsNullOrWhiteSpace(password) ? null : password;
		}

		return 1;
	}

	static int RemoveRepo(ConfigProfile profile)
	{
		var url = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select a repository to remove.")
				.AddChoices(profile.Repositories.Select(r => r.Url))
		);

		var repo = profile.Repositories.First(x => x.Url == url);
		var repoText = JsonSerializer.Serialize(repo.ToObfuscated(), ConfigUtils.jsonSerializerOptions);

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Current Repository Info")
			.AddRow(new JsonText(repoText));

		AnsiConsole.Write(table);

		bool confirm = AnsiConsole.Confirm("Are you sure you want to remove this repository?");
		if (!confirm)
			return 0;

		profile.Repositories.Remove(repo);

		return 1;
	}
}


enum RepoAction
{
	Add,
	Edit,
	Remove
}
