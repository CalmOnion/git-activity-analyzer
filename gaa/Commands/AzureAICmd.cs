using CalmOnion.GAA.Config;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CalmOnion.GAA.Commands;


public class AzureAICmd(ConfigFile config) : Command<AzureAICmd.Settings>
{
	readonly ConfigFile config = config;

	public class Settings : CommandSettings
	{
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		ConfigUtils.DisplayData(config.AzureOpenAI.ToObfuscated(), "Current Azure AI Info");

		var props = ConfigUtils
			.SelectPropertiesToEdit(["ApiKey", "Resource", "Deployment"]);

		if (props.Contains("ApiKey"))
		{
			var apiKey = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the API key:")
					.AllowEmpty()
			);

			config.AzureOpenAI.ApiKey = string.IsNullOrWhiteSpace(apiKey)
				? null : apiKey;
		}

		if (props.Contains("Resource"))
		{
			var resource = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the resource:")
					.AllowEmpty()
			);

			config.AzureOpenAI.Resource = string.IsNullOrWhiteSpace(resource)
				? null : resource;
		}

		if (props.Contains("Deployment"))
		{
			var deployment = AnsiConsole.Prompt(
				new TextPrompt<string?>("Enter the deployment:")
					.AllowEmpty()
			);

			config.AzureOpenAI.Deployment = string.IsNullOrWhiteSpace(deployment)
				? null : deployment;
		}

		ConfigUtils.SaveConfigFile(config);

		return 1;
	}
}
