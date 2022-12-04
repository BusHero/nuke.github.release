using Nuke.Common;
using Nuke.Common.Execution;
using Serilog;

partial class Build
{
	Target Logs => _ => _
		.Executes(() =>
		{
			var currentLogger = Log.Logger;
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console(
					outputTemplate: "{Message:l}{NewLine}{Exception}",
					applyThemeToRedirectedOutput: true)
				.ConfigureInMemory(this)
				.ConfigureFiles(this)
				.ConfigureLevel()
				.ConfigureFilter()
				.CreateLogger();
			Log.Verbose("This is a verbose message");
			Log.Debug("This is a debug message");
			Log.Information("This is an information message");
			Log.Warning("This is a warning message");
			Log.Error("This is an error message");

			Log.Logger = currentLogger;
		});
}
