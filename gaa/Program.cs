using CalmOnion.GAA.Commands;
using CalmOnion.GAA.Config;
using CalmOnion.GAA.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;


// Load the config file, creating it if it does not exist?
ConfigFile? config = ConfigUtils.LoadConfigFile()
	?? throw new InvalidOperationException("Config file could not be loaded");

// to retrieve the log file name, we must first parse the command settings
// this will require us to delay setting the file path for the file writer.
// With serilog we can use an enricher and Serilog.Sinks.Map to dynamically
// pull this setting.
var serviceCollection = new ServiceCollection()
	.AddLogging(configure =>
			configure.AddSerilog(new LoggerConfiguration()
				// log level will be dynamically be controlled by our log interceptor upon running
				.MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
				// the log enricher will add a new property with the log file path from the settings
				// that we can use to set the path dynamically
				.Enrich.With<LoggingEnricher>()
				// serilog.sinks.map will defer the configuration of the sink to be ondemand
				// allowing us to look at the properties set by the enricher to set the path appropriately
				.WriteTo.Map(LoggingEnricher.LogFilePathPropertyName,
					(logFilePath, wt) => wt.File($"{logFilePath}"), 1)
				.CreateLogger()
			)
	)
	.AddSingleton(config);

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(config =>
{
	config.SetInterceptor(new LogInterceptor());
	config.AddCommand<RepositoryCmd>("repo");
	config.AddCommand<DefaultsCmd>("defaults");
	config.AddCommand<ListCmd>("list");
	config.AddCommand<AnalyzeCmd>("analyze");
	config.AddCommand<ProfileCmd>("profile");
	config.AddCommand<AzureAICmd>("azureai");
});

app.Run(args);
