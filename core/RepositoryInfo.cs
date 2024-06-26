namespace CalmOnion.GAA;


public record RepositoryInfo
{
	public required string Url { get; init; }
	public string Name  => Path.GetFileNameWithoutExtension(Url);
	public string[]? Authors { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public RepositoryInfo ToObfuscated() => new()
	{
		Url = Url,
		Authors = Authors,
		Username = Username,
		Password = Password is not null ? "********" : null
	};
}