namespace CalmOnion.GAA;

public record CommitInfo
{
	public required string Id { get; init; }
	public required IReadOnlyCollection<Change> Changes{ get; init; }
}

public record Change {
	public required string Path { get; init; }
	public required string Patch { get; init; }
}
