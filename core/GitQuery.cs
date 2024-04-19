using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace CalmOnion.GAA;

public class GitQuery
{
	/// <summary>
	/// Map information about commits made to a collection of repositories by an Author
	/// </summary>
	public IDictionary<string, IReadOnlyCollection<CommitInfo>> MapCommitInfos(DateTimeOffset fromDate, DateTimeOffset toDate, params RepositoryInfo[] infos) {
		var tasks = infos.Select(info => Task.Factory.StartNew(() => (info, commits: GetCommitInfos(fromDate, toDate, info).ToArray())));

		var results = Task.WhenAll(tasks.ToArray()).Result;

		return results.ToDictionary(
			t => t.info.Name,
			t => t.commits as IReadOnlyCollection<CommitInfo>);
	}

	/// <summary>
	/// Get information about commits made to the repository by the Author
	/// </summary>
	public IEnumerable<CommitInfo> GetCommitInfos(DateTimeOffset fromDate, DateTimeOffset toDate, RepositoryInfo repository)
	{
		var authorEmail = repository.Author is not null && Regex.IsMatch(repository.Author, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase) ? repository.Author : null;
		var authorName = authorEmail is null ? repository.Author : null;

		var tempPath = Path.Combine(Path.GetTempPath(), nameof(CalmOnion), nameof(GAA));
		var tempRepoPath = Path.Combine(tempPath, repository.Name);

		var cloneOptions = new CloneOptions { Checkout = false };
		if (repository.Username is not null && repository.Password is not null) {
			var cred = new UsernamePasswordCredentials { Username = repository.Username, Password = repository.Password };
			cloneOptions.FetchOptions.CredentialsProvider = (_url, _user, _cred) => cred;
		}

		if (!Directory.Exists(tempRepoPath))
			Repository.Clone(repository.Url, tempRepoPath, cloneOptions);

		using var repo = new Repository(tempRepoPath);

		var commits = repo.Commits
			.TakeWhile(c => c.Author.When >= fromDate && c.Author.When < toDate)
			.Where(c =>
			{
				// Filter out commits by other authors.
				if (authorEmail is not null && !c.Author.Email.Equals(authorEmail, StringComparison.OrdinalIgnoreCase))
					return false;
				if (authorName is not null && !c.Author.Name.Equals(authorName, StringComparison.OrdinalIgnoreCase))
					return false;

				// Filter out merge commits.
				if (c.Parents.Count() != 1)
					return false;

				// TODO: What about the first commit of the repository?
				// It would now always be filtered out since it would have 0 parents.

				return true;
			})
			.ToArray();

		foreach (var commit in commits)
		{
			var parent = commit.Parents.Single();
			var patches = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);

			yield return new CommitInfo
			{
				Id = commit.Sha,
				Changes = patches.Select(p => new Change { Path = p.Path, Patch = p.Patch }).ToArray(),
			};
		}
	}
}
