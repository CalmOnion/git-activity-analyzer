using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace CalmOnion.GAA.Config;


public record ConfigFile
{
	public const string DefaultProfileName = "default";
	public string Version { get; init; } = "1.0";
	public string DefaultProfile { get; set; } = DefaultProfileName;
	public AzureOpenAIConfig AzureOpenAI { get; init; } = new();
	public List<ConfigProfile> Profiles { get; init; } = [];
	public ConfigFile ToObfuscated() => new()
	{
		Version = Version,
		AzureOpenAI = AzureOpenAI.ToObfuscated(),
		Profiles = Profiles.Select(x => x.ToObfuscated()).ToList()
	};
}


public record AzureOpenAIConfig
{
	public string? ApiKey { get; set; }
	public string? Resource { get; set; }
	public string? Deployment { get; set; }
	public AzureOpenAIConfig ToObfuscated() => new()
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


public record ProfileDefaults
{
	public string? Author { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public ProfileDefaults ToObfuscated() => new()
	{
		Author = Author,
		Username = Username,
		Password = Password is not null ? "********" : null
	};
}
