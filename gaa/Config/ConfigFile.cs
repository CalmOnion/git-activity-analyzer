using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Config;


public record ConfigFile
{
	public string Version { get; init; } = "1.0";
	public static string DefaultProfile { get; set; } = "default";
	public List<ConfigProfile> Profiles { get; init; } = [];
	public ConfigFile ToObfuscated() => new()
	{
		Version = Version,
		Profiles = Profiles.Select(x => x.ToObfuscated()).ToList()
	};
}

public record ChatGPTConfig
{
	public string? ApiKey { get; set; }
	public string? Endpoint { get; set; }
	public string? Deployment { get; set; }
}


public record ConfigProfile
{
	public string Name { get; init; } = ConfigFile.DefaultProfile;
	public UserDefaults Defaults { get; init; } = new();
	public List<RepositoryInfo> Repositories { get; init; } = [];
	public ConfigProfile ToObfuscated() => new()
	{
		Name = Name,
		Defaults = Defaults.ToObfuscated(),
		Repositories = Repositories.Select(x => x.ToObfuscated()).ToList()
	};
	public JsonText ToJsonText() =>
		new(JsonSerializer.Serialize(ToObfuscated(), ConfigUtils.jsonSerializerOptions));
}


public record UserDefaults
{
	public string? Author { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public UserDefaults ToObfuscated() => new()
	{
		Author = Author,
		Username = Username,
		Password = "********"
	};
}


public class ConfigUtils
{
	public static readonly string path = Path.Combine(new DirectoryInfo(Environment
		.GetFolderPath(Environment.SpecialFolder.UserProfile)).FullName, ".gaaconfig");

	public static ConfigFile? LoadConfigFile()
	{
		string path = ConfigUtils.path;
		ConfigFile? config;

		if (File.Exists(path) == false)
		{
			config = new();
			config.Profiles.Add(new());
			File.WriteAllText(path, JsonSerializer.Serialize(config));

			return config;
		}

		var text = File.ReadAllText(path);
		config = JsonSerializer.Deserialize<ConfigFile>(text, jsonSerializerOptions);

		return config;
	}

	public static void SaveConfigFile(ConfigFile config)
	{
		string path = ConfigUtils.path;
		string serialized = JsonSerializer.Serialize(config, jsonSerializerOptions);

		File.WriteAllText(path, serialized);
	}

	public static ConfigProfile? GetOrCreateProfile(ConfigFile config, string profileName, bool ask = true)
	{
		ConfigProfile? profile = config.Profiles
			.FirstOrDefault(x => x.Name == profileName);

		if (profile is not null)
			return profile;

		bool create = ask && AnsiConsole.Confirm($"Profile {profileName} does not exist. Create it?");
		if (create == false)
			return null;

		profile = new() { Name = profileName };
		config.Profiles.Add(profile);

		return profile;
	}

	public static string GetConfigAsString() =>
		JsonSerializer.Serialize(LoadConfigFile(), jsonSerializerOptions);

	public static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = true,
	};
}