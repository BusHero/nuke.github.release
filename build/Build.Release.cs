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

	[GitRepository] GitRepository Repository;

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
			var milestone = await Repository.GetGitHubMilestone(milestoneTitle);

			await Repository.TryCreateGitHubMilestone(milestoneTitle);
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


	Target ShowRepositoryInfo => _ => _
		.Executes(() =>
		{
			Log.Information("Commit = {Value}", Repository.Commit);
			Log.Information("Branch = {Value}", Repository.Branch);
			Log.Information("Tags = {Value}", Repository.Tags);

			Log.Information("main branch = {Value}", Repository.IsOnMainBranch());
			Log.Information("main/master branch = {Value}", Repository.IsOnMainOrMasterBranch());
			Log.Information("release/* branch = {Value}", Repository.IsOnReleaseBranch());
			Log.Information("hotfix/* branch = {Value}", Repository.IsOnHotfixBranch());

			Log.Information("Https URL = {Value}", Repository.HttpsUrl);
			Log.Information("SSH URL = {Value}", Repository.SshUrl);
		});

	Target ShowActionInfo => _ => _
		.Executes(() =>
		{
			Log.Information("Token - {Token}", GitHubActions.Token);
			Log.Information("Action - {Action}", GitHubActions.Action);
			Log.Information("Actor - {Actor}", GitHubActions.Actor);
			Log.Information("BaseRef - {BaseRef}", GitHubActions.BaseRef);
			Log.Information("EventName - {EventName}", GitHubActions.EventName);
			Log.Information("EventPath - {EventPath}", GitHubActions.EventPath);
			Log.Information("GitHubEvent - {GitHubEvent}", GitHubActions.GitHubEvent);
			Log.Information("HeadRef - {HeadRef}", GitHubActions.HeadRef);
			Log.Information("Home - {Home}", GitHubActions.Home);
			Log.Information("IsPullRequest - {IsPullRequest}", GitHubActions.IsPullRequest);
			Log.Information("Job - {Job}", GitHubActions.Job);
			Log.Information("JobId - {JobId}", GitHubActions.JobId);
			Log.Information("PullRequestAction - {PullRequestAction}", GitHubActions.PullRequestAction);
			Log.Information("PullRequestNumber - {PullRequestNumber}", GitHubActions.PullRequestNumber);
			Log.Information("Ref - {Ref}", GitHubActions.Ref);
			Log.Information("Repository - {Repository}", GitHubActions.Repository);
			Log.Information("RepositoryOwner - {RepositoryOwner}", GitHubActions.RepositoryOwner);
			Log.Information("RunId - {RunId}", GitHubActions.RunId);
			Log.Information("RunNumber - {RunNumber}", GitHubActions.RunNumber);
			Log.Information("ServerUrl - {ServerUrl}", GitHubActions.ServerUrl);
			Log.Information("Sha - {Sha}", GitHubActions.Sha);
			Log.Information("Token - {Token}", GitHubActions.Token);
			Log.Information("Workflow - {Workflow}", GitHubActions.Workflow);
			Log.Information("Workspace - {Workspace}", GitHubActions.Workspace);
		});

	Target Release => _ => _
	 	.DependsOn(ShowRepositoryInfo, ShowActionInfo)
		.Executes(() =>
		{
			// var credentials = new Credentials(GitHubActions.Token);
			// GitHubTasks.GitHubClient = new GitHubClient(
			// 	new ProductHeaderValue(nameof(NukeBuild)),
			// 	new Octokit.Internal.InMemoryCredentialStore(credentials));
			// var release = new NewRelease(MajorMinorPatchVersion)
			// {
			// 	Name = $"Release {MajorMinorPatchVersion}",
			// 	Prerelease = true,
			// 	Body = "Some body here and there"
			// };
			// var createdRelease = GitHubTasks.GitHubClient.Repository.Release.Create(
			// 	GitHubActions.RepositoryOwner,
			// 	GitRepository.Identifier,
			// 	NewRelease).Result;
		});
}
