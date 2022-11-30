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

	static string GetSecretValue(string secret)
				=> $"${{{{ secrets.{secret.SplitCamelHumpsWithKnownWords().JoinUnderscore().ToUpperInvariant()} }}}}";

	Target Release => _ => _
		.Executes(() =>
		{
			// Log.Information("Action - {Action}", GitHubActions.Action);
			// Log.Information("Actor - {Actor}", GitHubActions.Actor);
			// Log.Information("BaseRef - {BaseRef}", GitHubActions.BaseRef);
			// Log.Information("EventName - {EventName}", GitHubActions.EventName);
			// Log.Information("EventPath - {EventPath}", GitHubActions.EventPath);
			// Log.Information("GitHubEvent - {GitHubEvent}", GitHubActions.GitHubEvent);
			// Log.Information("HeadRef - {HeadRef}", GitHubActions.HeadRef);
			// Log.Information("Home - {Home}", GitHubActions.Home);
			// Log.Information("IsPullRequest - {IsPullRequest}", GitHubActions.IsPullRequest);
			// Log.Information("Job - {Job}", GitHubActions.Job);
			// Log.Information("JobId - {JobId}", GitHubActions.JobId);
			// Log.Information("PullRequestAction - {PullRequestAction}", GitHubActions.PullRequestAction);
			// Log.Information("PullRequestNumber - {PullRequestNumber}", GitHubActions.PullRequestNumber);
			// Log.Information("Ref - {Ref}", GitHubActions.Ref);
			// Log.Information("Repository - {Repository}", GitHubActions.Repository);
			// Log.Information("RepositoryOwner - {RepositoryOwner}", GitHubActions.RepositoryOwner);
			// Log.Information("RunId - {RunId}", GitHubActions.RunId);
			// Log.Information("RunNumber - {RunNumber}", GitHubActions.RunNumber);
			// Log.Information("ServerUrl - {ServerUrl}", GitHubActions.ServerUrl);
			// Log.Information("Sha - {Sha}", GitHubActions.Sha);
			Log.Information("Token - {Token}", GitHubActions.Token);
			// Log.Information("Workflow - {Workflow}", GitHubActions.Workflow);
			// Log.Information("Workspace - {Workspace}", GitHubActions.Workspace);
		});
}
