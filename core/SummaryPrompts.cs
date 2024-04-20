namespace CalmOnion.GAA;

public static class SummaryPrompts
{
	public const string Technical = """
	Please summarize my activity by grouping activity for
	the same sort of thing made across the different repositories
	and commits and describing what I've done in your own words,
	as if you were me. Include the overall theme of the changes.
	""";

	public const string Manager = """
	Please summarize my activity for my non-technical manager
	by describing what I've done in your own words,
	as if you were me. Include the overall theme of the changes.
	""";
}
