using Spectre.Console;

namespace CalmOnion.GAA.Config;


public record ConfigFile
{
	public const string DefaultProfileName = "default";
	public string Version { get; init; } = "1.0";
	public string DefaultProfile { get; set; } = DefaultProfileName;
	public List<ConfigProfile> Profiles { get; init; } = [];
	public ConfigFile ToObfuscated() => new()
	{
		Version = Version,
		Profiles = Profiles.Select(x => x.ToObfuscated()).ToList()
	};
}


public record AzureOpenAiConfig
{
	public string? ApiKey { get; set; }
	public string? Resource { get; set; }
	public string? Deployment { get; set; }
	public AzureOpenAiConfig ToObfuscated() => new()
	{
		ApiKey = ApiKey is not null ? "********" : null,
		Resource = Resource,
		Deployment = Deployment
	};
}


public record ConfigProfile
{
	public string Name { get; set; } = ConfigFile.DefaultProfileName;
	public ProfileDefaults Defaults { get; init; } = new();
	public ProfilePrompts Prompts { get; init; } = new();
	public AzureOpenAiConfig AzureOpenAi { get; set; } = new();
	public List<RepositoryInfo> Repositories { get; init; } = [];
	public ConfigProfile ToObfuscated() => new()
	{
		Name = Name,
		Defaults = Defaults.ToObfuscated(),
		Prompts = Prompts,
		AzureOpenAi = AzureOpenAi.ToObfuscated(),
		Repositories = Repositories.Select(x => x.ToObfuscated()).ToList()
	};
}


public record ProfileDefaults
{
	public string[]? Authors { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public ProfileDefaults ToObfuscated() => new()
	{
		Authors = Authors,
		Username = Username,
		Password = Password is not null ? "********" : null
	};
}


public record ProfilePrompts
{
	public string? ExplanationPrompt { get; set; }
	public string? SummaryPrompt { get; set; }
	public int? MaxTokens { get; set; }
}