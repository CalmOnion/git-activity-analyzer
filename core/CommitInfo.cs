namespace CalmOnion.GAA;

public record CommitInfo
{
	public required string Id { get; init; }
	public required string Message { get; init; }
	public required IReadOnlyCollection<Change> Changes { get; init; }
	public required DateTimeOffset Date { get; init; }
	public required Actor Author { get; init; }
}

public record Change
{
	public required string Path { get; init; }
	public required string Patch { get; init; }
}

public record Actor
{
	public required string Name { get; init; }
	public required string Email { get; init; }
}