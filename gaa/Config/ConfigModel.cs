namespace CalmOnion.GAA.Config;

public record ConfigEntity
{
	readonly List<ConfigRepository> repositories = [];
}

public record ConfigRepository
{
	public string Url { get; set; } = "";
}