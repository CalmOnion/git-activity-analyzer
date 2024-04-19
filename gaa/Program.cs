using CalmOnion.GAA.Commands;
using CalmOnion.GAA.Config;
using CalmOnion.GAA.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console.Cli;

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
	);

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(config =>
{
	config.SetInterceptor(new LogInterceptor()); // add the interceptor
	config.AddCommand<HelloCommand>("hello");
	config.AddCommand<RepoCommand>("repo");
});

// Find out of the config file has been created?
// If not, create it.
var userDir = new DirectoryInfo(Environment
	.GetFolderPath(System.Environment.SpecialFolder.UserProfile));

var config = new ConfigEntity();





app.Run([
	"repo", "https://github.com/CalmOnion/git-activity-analyzer.git",
]);

