using CalmOnion.GAA.Commands;
using CalmOnion.GAA.Config;
using CalmOnion.GAA.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;


// Load the config file, creating it if it does not exist?
ConfigFile? config = ConfigUtils.LoadConfigFile()
	?? throw new InvalidOperationException("Config file could not be loaded");

var serviceCollection = new ServiceCollection()
	.AddSingleton(config);

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(config =>
{
	config.AddCommand<RepositoryCmd>("repo");
	config.AddCommand<DefaultsCmd>("defaults");
	config.AddCommand<ListCmd>("list");
	config.AddCommand<AnalyzeCmd>("analyze");
	config.AddCommand<ProfileCmd>("profile");
	config.AddCommand<AzureAICmd>("azureai");
});

app.Run(args);
