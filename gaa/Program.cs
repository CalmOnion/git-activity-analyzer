using CalmOnion.GAA;
using CalmOnion.GAA.Commands;
using CalmOnion.GAA.Config;
using CalmOnion.GAA.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;


// Load the config file, creating it if it does not exist?
ConfigFile? config = ConfigUtils.LoadConfigFile()
	?? throw new InvalidOperationException("Config file could not be loaded");

var serviceCollection = new ServiceCollection()
	.AddSingleton<GitQuery>()
	.AddSingleton(config);

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(config =>
{
	config.AddCommand<ProfileCmd>("profile")
		.WithDescription("Guided command for modifying config profiles");

	config.AddCommand<AnalyzeCommitCmd>("analyze-commit")
		.WithDescription("Analyze commits in a repository");
});

app.Run(args);
