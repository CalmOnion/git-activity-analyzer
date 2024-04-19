namespace CalmOnion.GAA;


public record RepositoryInfo
{
	public required string Url { get; init; }
	public string? Author { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
}