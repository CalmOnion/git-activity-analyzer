using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Config;


public class ConfigUtils
{
	public static readonly string path = Path.Combine(new DirectoryInfo(Environment
		.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName, ".gaaconfig");

	public static ConfigFile? LoadConfigFile()
	{
		ConfigFile? config;

		ConfigFile initConfig()
		{
			config = new();
			config.Profiles.Add(new());
			File.WriteAllText(path, ConfigToString(config));

			return config;
		}

		if (File.Exists(path) == false)
			return initConfig();

		string text = File.ReadAllText(path);

		try
		{
			config = ConfigFromString<ConfigFile>(text);
		}
		catch (Exception)
		{
			AnsiConsole.MarkupLine("[red]Config file is corrupted[/]");
			bool confirm = AnsiConsole.Confirm("Would you like to reset the config file?");
			if (confirm)
				return initConfig();

			return null;
		}

		return config;
	}

	public static void SaveConfigFile(ConfigFile config)
	{
		var path = ConfigUtils.path;
		var serialized = ConfigToString(config);

		File.WriteAllText(path, serialized);
	}

	public static string ConfigToString(object data) =>
		JsonSerializer.Serialize(data, jsonSerializerOptions);

	public static T? ConfigFromString<T>(string text) =>
		JsonSerializer.Deserialize<T>(text, jsonSerializerOptions);

	public static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true,
	};

	public static ConfigProfile? SelectProfile(ConfigFile config, string? nullOption = null)
	{
		var profileNames = config.Profiles.Select(p => p.Name).ToList();
		if (nullOption is not null)
			profileNames.Insert(0, nullOption);

		var selectedProfileName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select a profile")
				.PageSize(10)
				.MoreChoicesText("More")
				.AddChoices(profileNames)
		);

		if (selectedProfileName == nullOption)
			return null;

		return config.Profiles
			.FirstOrDefault(x => x.Name == selectedProfileName);
	}

	public static RepositoryInfo[] SelectRepository(ConfigProfile profile, bool multiSelect = false)
	{
		var urls = profile.Repositories.Select(r => r.Url).ToList();
		if (multiSelect)
		{
			var selectedUrls = AnsiConsole.Prompt(
				new MultiSelectionPrompt<string>()
					.Title("Select repositories")
					.PageSize(10)
					.Required()
					.MoreChoicesText("More")
					.AddChoices(urls)
			);

			return profile.Repositories
				.Where(x => selectedUrls.Contains(x.Url)).ToArray();
		}

		var selectedUrl = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select a repository")
				.PageSize(10)
				.MoreChoicesText("More")
				.AddChoices(urls)
		);

		var first = profile.Repositories
			.FirstOrDefault(x => x.Url == selectedUrl);

		return first is not null ? [first] : [];
	}

	public static (DateTimeOffset from, DateTimeOffset to)? SelectTimeframe()
	{
		var option = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select a date range option")
				.MoreChoicesText("Scroll down for more options")
				.AddChoices(DateRangeOptions.Keys)
		);

		return DateRangeOptions[option] switch
		{
			DateRangeOption.LastSevenDays => (DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now),
			_ => null,
		};
	}

	static readonly IReadOnlyDictionary<string, DateRangeOption> DateRangeOptions = new Dictionary<string, DateRangeOption>()
	{
		//["This Week"] = DateRangeOption.ThisWeek,
		//["Last Week"] = DateRangeOption.LastWeek,
		["Last 7 days"] = DateRangeOption.LastSevenDays,
		//["Calendar"] = DateRangeOption.Calendar,
		//["From - To"] = DateRangeOption.FromTo,
	};

	public static RepositoryScope? SelectScope(
		ConfigFile config,
		bool multiSelectRepository = true
	)
	{
		var profile = SelectProfile(config);
		if (profile is null)
			return null;

		var repositories = SelectRepository(profile, multiSelectRepository);
		if (repositories.Length == 0)
			return null;

		var timeframe = SelectTimeframe();

		if (timeframe is null)
			return null;

		return new()
		{
			From = timeframe.Value.from,
			To = timeframe.Value.to,
			Repositories = repositories
		};
	}



	public static HashSet<string> SelectPropertiesToEdit(string[] properties)
	{
		var props = AnsiConsole.Prompt(
			new MultiSelectionPrompt<string>()
				.Title("Select a property to edit")
				.AddChoices(properties)
		).ToHashSet();

		return props;
	}

	public static void DisplayData(object data, string header)
	{
		var json = new JsonText(ConfigToString(data));
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn(header)
			.AddRow(json);

		AnsiConsole.Write(table);
	}

	public static void DisplayProfile(ConfigProfile profile)
	{
		var json = new JsonText(ConfigToString(profile.ToObfuscated()));
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn($"Profile -> {profile.Name}")
			.AddRow(json);

		AnsiConsole.Write(table);
	}

	public static void DisplayProfiles(List<ConfigProfile> profiles)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn($"Profiles");

		foreach (var profile in profiles)
			table.AddRow(new JsonText(ConfigToString(profile.ToObfuscated())));

		AnsiConsole.Write(table);
	}

}


public record RepositoryScope
{
	public DateTimeOffset From { get; init; }
	public DateTimeOffset To { get; init; }
	public RepositoryInfo[] Repositories { get; init; } = [];

}


enum DateRangeOption
{
	ThisWeek,
	LastWeek,
	LastSevenDays,
	Calendar,
	FromTo,
}