using Nuke.Common;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.DotNet;

partial class Build : NukeBuild
{
	public static int Main() => Execute<Build>(x => x.Compile);

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			DotNetClean(_ => _
				.SetProject(RootDirectory / "App.Console" / "App.Console.csproj"));
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(_ => _
				.SetProjectFile(RootDirectory / "App.Console" / "App.Console.csproj"));
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(_ => _
				.SetProjectFile(RootDirectory / "App.Console" / "App.Console.csproj"));
		});

	Target Publish => _ => _
		.Executes(() =>
		{
			DotNetPublish(_ => _
				.SetProject(RootDirectory / "App.Console")
				.SetOutput("publish"));
		});
}
