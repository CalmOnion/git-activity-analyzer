using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace CalmOnion.GAA;

public class GitQuery
{
	protected static readonly Regex EmailExpression = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

	/// <summary>
	/// Get authors from the repository.
	/// </summary>
	public ISet<Actor> GetRepositoryAuthors(RepositoryInfo repository)
	{
		using var repo = GetRepository(repository);

		var authors = new HashSet<Actor>();

		foreach (var commit in repo.Commits)
		{
			authors.Add(new Actor
			{
				Name = commit.Author.Name,
				Email = commit.Author.Email
			});
		}

		return authors;
	}

	/// <summary>
	/// Map information about commits made to a collection of repositories by an Author
	/// </summary>
	public IDictionary<RepositoryInfo, ICollection<CommitInfo>> GetCommitsByRepository(DateTimeOffset fromDate, DateTimeOffset toDate, params RepositoryInfo[] infos)
	{
		var tasks = infos.Select(info => Task.Factory.StartNew(() => (info, commits: GetCommits(fromDate, toDate, info).ToArray() as ICollection<CommitInfo>)));

		var results = Task.WhenAll(tasks.ToArray()).Result;

		return results.ToDictionary(t => t.info, t => t.commits);
	}

	/// <summary>
	/// Get information about commits made to the repository by the Author
	/// </summary>
	public IEnumerable<CommitInfo> GetCommits(DateTimeOffset fromDate, DateTimeOffset toDate, RepositoryInfo repository)
	{
		var authorEmails = new HashSet<string>();
		var authorNames = new HashSet<string>();

		if (repository.Authors is not null)
		{
			foreach (var author in repository.Authors)
			{
				if (EmailExpression.IsMatch(author))
					authorEmails.Add(author);
				else
					authorNames.Add(author);
			}
		}

		using var repo = GetRepository(repository);

		var commits = repo.Commits
			.SkipWhile(c => c.Author.When > toDate)
			.TakeWhile(c => c.Author.When >= fromDate)
			.Where(c =>
			{
				// Filter out commits by other authors.
				if (authorEmails.Count > 0 && !authorEmails.Contains(c.Author.Email, StringComparer.OrdinalIgnoreCase))
					return false;
				if (authorNames.Count > 0 && !authorNames.Contains(c.Author.Name, StringComparer.OrdinalIgnoreCase))
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
				Id = commit.Sha[0..8],
				Message = commit.Message,
				Changes = patches.Select(p => new Change { Path = p.Path, Patch = p.Patch }).ToArray(),
				Date = commit.Author.When,
				Author = new Actor
				{
					Name = commit.Author.Name,
					Email = commit.Author.Email,
				},
			};
		}
	}

	protected Repository GetRepository(RepositoryInfo repository, bool fetchLatest = true)
	{
		var tempPath = Path.Combine(Path.GetTempPath(), nameof(CalmOnion), nameof(GAA));
		var tempRepoPath = Path.Combine(tempPath, repository.Name);

		var cloneOptions = new CloneOptions { Checkout = false };
		if (repository.Username is not null && repository.Password is not null)
		{
			var cred = new UsernamePasswordCredentials { Username = repository.Username, Password = repository.Password };
			cloneOptions.FetchOptions.CredentialsProvider = (_url, _user, _cred) => cred;
		}

		var existed = Directory.Exists(tempRepoPath);
		if (!existed)
			Repository.Clone(repository.Url, tempRepoPath, cloneOptions);

		var repo = new Repository(tempRepoPath);

		if (existed && fetchLatest)
		{
			var fetchOptions = new FetchOptions
			{
				Prune = true,
				TagFetchMode = TagFetchMode.Auto,
			};

			if (repository.Username is not null && repository.Password is not null)
			{
				var cred = new UsernamePasswordCredentials { Username = repository.Username, Password = repository.Password };
				fetchOptions.CredentialsProvider = (_url, _user, _cred) => cred;
			}

			var refSpecs = $"+refs/heads/*:refs/remotes/origin/*";
			Commands.Fetch(repo, "origin", [refSpecs], fetchOptions, "Fetching remote");
		}

		return repo;
	}
}
