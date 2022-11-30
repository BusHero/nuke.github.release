using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.Tools.GitHub.GitHubTasks;
using Serilog;
using System.IO;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Utilities;
using Octokit;

[GitHubActions(
	"foo",
	GitHubActionsImage.UbuntuLatest,
	On = new[] { GitHubActionsTrigger.Push },
	InvokedTargets = new[] { nameof(Compile) },
	EnableGitHubToken = true)]
partial class Build
{
	[Parameter]
	readonly bool Major;

	[GitVersion] GitVersion GitVersion;

	[GitRepository] GitRepository GitRepository;

	GitHubActions GitHubActions => GitHubActions.Instance;

	const string MasterBranch = "master";

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
			var credentials = new Credentials(GitHubActions.Token);
			GitHubTasks.GitHubClient = new GitHubClient(
				new ProductHeaderValue(nameof(NukeBuild)),
				new Octokit.Internal.InMemoryCredentialStore(credentials));
		});
}
