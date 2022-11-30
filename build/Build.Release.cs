using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using Nuke.Common.Tools.Git;
using Serilog;
using System.IO;

partial class Build
{
	[Parameter]
	readonly bool Major;

	[GitVersion] GitVersion GitVersion;

	[GitRepository] GitRepository GitRepository;

	const string MasterBranch = "main";

	string MajorMinorPatchVersion => Major ? $"{GitVersion.Major + 1}.0.0" : GitVersion.MajorMinorPatch;

	Target ShowVersion => _ => _
		.Executes(() =>
		{
			Log.Information("Version - {Version}", MajorMinorPatchVersion);
		});

	Target Milestone => _ => _
		.Executes(async () =>
		{
			var milestoneTitle = $"v{MajorMinorPatchVersion}";
			var milestone = await GitRepository.GetGitHubMilestone(milestoneTitle);

			await GitRepository.TryCreateGitHubMilestone(milestoneTitle);
			Log.Information("Milestone - '{Milestone}'", milestone);
			Log.Information("Milestone Open issues - '{MilestoneOpenIssues}'", milestone?.OpenIssues);
			Log.Information("Milestone Closed issues - '{MilestoneClosedIssues}'", milestone?.ClosedIssues);
			Log.Information("Milestone State - '{MilestoneState}'", milestone?.State);
		});

	AbsolutePath ChangelogFile => RootDirectory / "CHANGELOG.md";

	Target EnsureChangelogFile => _ => _
		.Unlisted()
		.Executes(() =>
		{
			using var _ = File.CreateText(ChangelogFile);
		});

	Target Release => _ => _
		.Executes(() =>
		{
			Git($"checkout {MasterBranch}");
			Git($"merge --no-ff --no-edit {GitRepository.Branch}");
			Git($"tag {MajorMinorPatchVersion}");
			Git($"push origin {MasterBranch} {MajorMinorPatchVersion}");
		});
}
