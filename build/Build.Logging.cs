using Nuke.Common;
using Nuke.Common.Execution;
using Serilog;
using Nuke.Common.Tools.PowerShell;
using static PowerShellCoreTasks;

partial class Build
{
	Target ExecuteTests => _ => _
		.Executes(() =>
		{
			PowerShellCoreTasks.PowerShellCore(_ => _
				.SetFile(RootDirectory / "runners" / "tests.runner.ps1"));
		});

	Target ExecuteTestsFixLogging => _ => _
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
			PowerShellCoreTasks.PowerShellCore(_ => _
				.SetFile(RootDirectory / "runners" / "tests.runner.ps1"));
		});

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
