using System.Text.Json;
using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Commands;


public class ProfileCmd(ConfigFile config) : Command<ProfileCmd.Settings>
{
	static readonly BiDictionary<string, ProfileAction> ProfileOptions = new()
	{
		["Edit Profile"] = ProfileAction.Edit,
		["Add Profile"] = ProfileAction.Add,
		["Clone Profile"] = ProfileAction.Clone,
		["Remove Profile"] = ProfileAction.Remove,
		["Read Profile"] = ProfileAction.Read,
		["Output config path"] = ProfileAction.OutputConfigPath,
	};

	public class Settings : CommandSettings { }

	public override int Execute(CommandContext context, Settings settings) => Run();

	public int Run()
	{
		var status = SelectAction() switch
		{
			ProfileAction.Edit => EditProfile(),
			ProfileAction.Add => AddProfile(),
			ProfileAction.Clone => CloneProfile(),
			ProfileAction.Remove => RemoveProfile(),
			ProfileAction.Read => ReadProfiles(),
			ProfileAction.OutputConfigPath => OutputConfigPath(),
			_ => 0,
		};

		ConfigUtils.SaveConfigFile(config);

		return status;
	}

	static int OutputConfigPath()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Config Path")
			.AddRow(new Markup($"[link bold teal]{ConfigUtils.path}[/]"));

		AnsiConsole.Write(table);

		return 1;
	}

	ProfileAction SelectAction()
	{
		List<string> actions = [
			ProfileOptions.GetByValue(ProfileAction.Add),
			ProfileOptions.GetByValue(ProfileAction.OutputConfigPath),
		];

		if (config.Profiles.Count > 0)
		{
			actions.Add(ProfileOptions.GetByValue(ProfileAction.Edit));
			actions.Add(ProfileOptions.GetByValue(ProfileAction.Clone));
			actions.Add(ProfileOptions.GetByValue(ProfileAction.Remove));
			actions.Add(ProfileOptions.GetByValue(ProfileAction.Read));
		}

		// Sort the actions by the enum order.
		actions.Sort((a, b) => ProfileOptions.GetByKey(a) - ProfileOptions.GetByKey(b));

		var option = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select an action")
				.AddChoices(actions)
		);

		return ProfileOptions[option];
	}

	int EditProfile()
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		return new ModifyProfile(config, selectedProfile).Run();
	}

	int AddProfile()
	{
		var name = AnsiConsole.Ask<string>("Enter the profile name");
		if (config.Profiles.Any(x => x.Name == name))
		{
			AnsiConsole.MarkupLine($"[red]Profile '{name}' already exists[/]");

			return -1;
		}

		config.Profiles.Add(new ConfigProfile { Name = name });
		return 1;
	}

	int CloneProfile()
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		var name = AnsiConsole.Prompt(
			new TextPrompt<string>("Enter the new profile name.")
				.DefaultValue(selectedProfile.Name)
				.ShowDefaultValue()
		);

		if (config.Profiles.Any(x => x.Name == name))
		{
			AnsiConsole.MarkupLine($"[red]Profile '{name}' already exists[/]");

			return -1;
		}

		var clone = new ConfigProfile
		{
			Name = name,
			Defaults = selectedProfile.Defaults,
			Prompts = selectedProfile.Prompts,
			AzureOpenAi = selectedProfile.AzureOpenAi,
			Repositories = selectedProfile.Repositories,
		};

		var preview = JsonSerializer.Serialize(clone.ToObfuscated(), ConfigUtils.jsonSerializerOptions);
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("New profile info")
			.AddRow(new JsonText(preview));

		AnsiConsole.Write(table);

		bool confirm = AnsiConsole.Confirm("Confirm creating a new profile with the above?");
		if (!confirm)
			return 0;

		config.Profiles.Add(clone);

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int RemoveProfile()
	{
		var selectedProfile = ConfigUtils.SelectProfile(config);
		if (selectedProfile is null)
			return -1;

		ConfigUtils.DisplayProfile(selectedProfile);

		bool confirm = AnsiConsole
			.Confirm($"Are you sure you want to remove profile '{selectedProfile.Name}'?");

		if (!confirm)
			return -1;

		config.Profiles.Remove(selectedProfile);

		if (config.Profiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[red]No profiles exist, creating a new default profile[/]");
			config.Profiles.Add(new ConfigProfile { Name = ConfigFile.DefaultProfileName });
			config.DefaultProfile = ConfigFile.DefaultProfileName;
		}
		else
		{
			if (config.DefaultProfile == selectedProfile.Name)
				config.DefaultProfile = config.Profiles[0].Name;
		}

		return 1;
	}

	int ReadProfiles()
	{
		var profiles = config.Profiles;

		var selectedProfile = ConfigUtils.SelectProfile(config, nullOption: "All");
		if (selectedProfile is not null)
			profiles = [selectedProfile];

		ConfigUtils.DisplayProfiles(profiles);

		return 1;
	}

}


enum ProfileAction
{
	Edit,
	Clone,
	Add,
	Remove,
	Read,
	OutputConfigPath,
}


class ModifyProfile(ConfigFile config, ConfigProfile profile)
{
	static readonly BiDictionary<string, ModifyProfileActions> options = new()
	{
		["Set as default profile"] = ModifyProfileActions.SetAsDefault,
		["Edit profile information"] = ModifyProfileActions.Edit,
		["Set profile defaults"] = ModifyProfileActions.SetDefaults,
		["Azure AI configuration"] = ModifyProfileActions.AzureAiConfig,
		["Adjust prompts"] = ModifyProfileActions.AdjustPrompts,
		["Edit repository info"] = ModifyProfileActions.EditRepo,
		["Add repository info"] = ModifyProfileActions.AddRepo,
		["Clone repository info"] = ModifyProfileActions.CloneRepo,
		["Remove repository info"] = ModifyProfileActions.RemoveRepo
	};

	public int Run()
	{
		return SelectAction() switch
		{
			ModifyProfileActions.SetAsDefault => MakeProfileDefault(),
			ModifyProfileActions.Edit => EditProfileInfo(),
			ModifyProfileActions.SetDefaults => SetProfileDefaults(),
			ModifyProfileActions.AzureAiConfig => AzureAiConfig(),
			ModifyProfileActions.AdjustPrompts => AdjustPrompts(),
			ModifyProfileActions.EditRepo => EditRepo(),
			ModifyProfileActions.AddRepo => AddRepo(),
			ModifyProfileActions.CloneRepo => CloneRepo(),
			ModifyProfileActions.RemoveRepo => RemoveRepo(),
			_ => 0,
		};
	}

	ModifyProfileActions SelectAction()
	{
		List<string> actions = [
			options.GetByValue(ModifyProfileActions.SetAsDefault),
			options.GetByValue(ModifyProfileActions.SetDefaults),
			options.GetByValue(ModifyProfileActions.AddRepo),
			options.GetByValue(ModifyProfileActions.AzureAiConfig),
			options.GetByValue(ModifyProfileActions.AdjustPrompts),
		];

		if (profile.Repositories.Count > 0)
		{
			actions.Add(options.GetByValue(ModifyProfileActions.EditRepo));
			actions.Add(options.GetByValue(ModifyProfileActions.CloneRepo));
			actions.Add(options.GetByValue(ModifyProfileActions.RemoveRepo));
		}

		// Sort the actions by the enum order.
		actions.Sort((a, b) => options.GetByKey(a) - options.GetByKey(b));

		var option = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Modifying profile")
				.AddChoices(actions)
		);

		return options[option];
	}

	int MakeProfileDefault()
	{
		config.DefaultProfile = profile.Name;

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int EditProfileInfo()
	{
		var props = ConfigUtils.SelectPropertiesToEdit(["Name"]);

		if (props.Contains("Name"))
		{
			var name = AnsiConsole.Ask<string>("Enter the profile name");
			if (config.Profiles.Any(x => x.Name == name))
			{
				AnsiConsole.MarkupLine($"[red]Profile '{name}' already exists[/]");

				return EditProfileInfo();
			}

			profile.Name = name;
		}

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int SetProfileDefaults()
	{
		var defaults = profile.Defaults;

		ConfigUtils.DisplayData(defaults.ToObfuscated(), $"Defaults -> {profile.Name}");

		var props = AnsiConsole.Prompt(
			new MultiSelectionPrompt<string>()
				.Title("Select a property to edit")
				.AddChoices(["Author", "Username", "Password"])
		).ToHashSet();

		if (props.Contains("Author"))
		{
			var author = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the author name:")
					.AllowEmpty()
			);

			defaults.Authors = string.IsNullOrWhiteSpace(author) ? null : [author];
		}
		if (props.Contains("Username"))
		{
			defaults.Username = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the username:")
					.AllowEmpty()
			);
		}
		if (props.Contains("Password"))
		{
			defaults.Password = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the password:")
					.AllowEmpty()
					.Secret()
			);
		}

		ConfigUtils.DisplayProfile(profile);
		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int AzureAiConfig()
	{
		ConfigUtils.DisplayData(
			profile.AzureOpenAi.ToObfuscated(),
			"Current Azure AI Info"
		);

		var props = ConfigUtils
			.SelectPropertiesToEdit(["ApiKey", "Resource", "Deployment"]);

		if (props.Contains("ApiKey"))
		{
			var apiKey = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the API key:")
					.AllowEmpty()
			);

			profile.AzureOpenAi.ApiKey = string.IsNullOrWhiteSpace(apiKey)
				? null : apiKey;
		}

		if (props.Contains("Resource"))
		{
			var resource = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the resource:")
					.AllowEmpty()
			);

			profile.AzureOpenAi.Resource = string.IsNullOrWhiteSpace(resource)
				? null : resource;
		}

		if (props.Contains("Deployment"))
		{
			var deployment = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the deployment:")
					.AllowEmpty()
			);

			profile.AzureOpenAi.Deployment = string.IsNullOrWhiteSpace(deployment)
				? null : deployment;
		}

		ConfigUtils.SaveConfigFile(config);

		ConfigUtils.DisplayData(
			profile.AzureOpenAi.ToObfuscated(),
			"New Azure AI Info"
		);

		return 1;
	}

	int AdjustPrompts()
	{
		var prompts = profile.Prompts;

		ConfigUtils.DisplayData(prompts, "Current Prompt Info");

		var props = ConfigUtils
			.SelectPropertiesToEdit(["ExplanationPrompt", "SummaryPrompt", "MaxTokens"]);

		if (props.Contains("ExplanationPrompt"))
		{
			var explanationPrompt = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the explanation prompt:")
					.AllowEmpty()
			);

			prompts.ExplanationPrompt = string.IsNullOrWhiteSpace(explanationPrompt)
				? null : explanationPrompt;
		}
		if (props.Contains("SummaryPrompt"))
		{
			var summaryPrompt = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the summary prompt:")
					.AllowEmpty()
			);

			prompts.SummaryPrompt = string.IsNullOrWhiteSpace(summaryPrompt)
				? null : summaryPrompt;
		}
		if (props.Contains("MaxTokens"))
		{
			var maxTokens = AnsiConsole.Prompt(
				new TextPrompt<int?>("Enter the max tokens:")
					.AllowEmpty()
			);

			prompts.MaxTokens = maxTokens;
		}

		ConfigUtils.SaveConfigFile(config);

		ConfigUtils.DisplayData(prompts, "New Prompt Info");

		return 1;
	}

	int EditRepo()
	{
		var repos = ConfigUtils.SelectRepository(profile);
		if (repos.Length == 0)
			return 0;

		var repo = repos.Single();

		ConfigUtils.DisplayData(repo.ToObfuscated(), "Current Repository Info");

		var props = ConfigUtils
			.SelectPropertiesToEdit(["Author", "Username", "Password"]);

		if (props.Contains("Author"))
		{
			var author = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the author name:")
					.AllowEmpty()
			);

			repo.Authors = string.IsNullOrWhiteSpace(author) ? null : [author];
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

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int AddRepo()
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
			Authors = string.IsNullOrWhiteSpace(author) ? null : [author],
			Username = string.IsNullOrWhiteSpace(username) ? null : username,
			Password = string.IsNullOrWhiteSpace(password) ? null : password,
		});

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int CloneRepo()
	{
		var repos = ConfigUtils.SelectRepository(profile);
		var repo = repos.Single();

		var url = AnsiConsole.Prompt(
			new TextPrompt<string>("Enter the new repository URL:")
				.DefaultValue(repo.Url)
				.ShowDefaultValue()
		);

		if (profile.Repositories.Any(x => x.Url == url))
		{
			AnsiConsole.MarkupLine($"[red]Repository '{url}' already exists[/]");

			return -1;
		}

		var clone = new RepositoryInfo
		{
			Url = url,
			Authors = repo.Authors,
			Username = repo.Username,
			Password = repo.Password,
		};

		var preview = JsonSerializer.Serialize(clone.ToObfuscated(), ConfigUtils.jsonSerializerOptions);
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("New Repository Info")
			.AddRow(new JsonText(preview));

		AnsiConsole.Write(table);

		bool confirm = AnsiConsole.Confirm("Confirm creating a new repository info with the above?");
		if (!confirm)
			return 0;

		profile.Repositories.Add(clone);

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

	int RemoveRepo()
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

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}

}


enum ModifyProfileActions
{
	AdjustPrompts,
	EditRepo,
	CloneRepo,
	AddRepo,
	RemoveRepo,
	Edit,
	SetAsDefault,
	SetDefaults,
	AzureAiConfig,
}
