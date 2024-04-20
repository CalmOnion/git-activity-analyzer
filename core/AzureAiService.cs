namespace CalmOnion.GAA;

using System.Text;
using Azure;
using Azure.AI.OpenAI;

public class AzureAiService(string ApiKey, string ResourceName, string DeploymentName)
{
	private readonly Lazy<OpenAIClient> _client = new(() =>
	{
		var endpoint = $"https://{ResourceName}.openai.azure.com/";
		return new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(ApiKey), new OpenAIClientOptions(OpenAIClientOptions.ServiceVersion.V2024_04_01_Preview));
	});

	public OpenAIClient Client => _client.Value;

	public string? GetCommitSummary(IDictionary<RepositoryInfo, ICollection<CommitInfo>> commitsByRepo, string prompt = SummaryPrompts.Technical)
	{
		var sb = new StringBuilder();
		foreach (var (repo, commits) in commitsByRepo)
		{
			sb.AppendLine($"In the '${repo.Name} repository I've made these changes:");
			RenderCommitInfos(sb, commits, false);
		}

		var completionOptions = new ChatCompletionsOptions(DeploymentName, [
			new ChatRequestSystemMessage("You are a helpful assistant."),
			new ChatRequestUserMessage(sb.ToString()),
			new ChatRequestUserMessage(prompt),
		])
		{
			MaxTokens = 2048,
		};
		var response = Client.GetChatCompletions(completionOptions) ?? throw new Exception("No response from Azure open-AI.");

		var summary = response.Value.Choices[0]?.Message?.Content;

		return summary;
	}

	public void RenderCommitInfos(StringBuilder sb, ICollection<CommitInfo> infos, bool includeDiffs = false)
	{
		foreach (var info in infos)
			RenderCommitInfo(sb, info, includeDiffs);
	}

	private static void RenderCommitInfo(StringBuilder sb, CommitInfo info, bool includeDiffs = false)
	{
		sb.Append("commit ");
		sb.Append(info.Id);
		sb.Append(": ");
		sb.AppendLine(info.Message);
		if (includeDiffs)
		{
			sb.AppendLine("diffs: ");
			foreach (var change in info.Changes)
			{
				sb.AppendLine(change.Patch);
				sb.AppendLine();
			}
		}
	}
}
