using Nuke.Common;
using Nuke.Common.Tools.GitVersion;
using Serilog;

partial class Build
{
	[Parameter]
	readonly bool Major;

	[GitVersion]
	GitVersion GitVersion;

	string MajorMinorPatchVersion => Major ? $"{GitVersion.Major + 1}.0.0" : GitVersion.MajorMinorPatch;

	Target ShowVersion => _ => _
		.Executes(() =>
		{
			Log.Information("Version - {Version}", MajorMinorPatchVersion);
		});
}
