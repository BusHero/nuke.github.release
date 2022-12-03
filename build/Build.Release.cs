using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.Tools.GitHub.GitHubTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.IO.FileSystemTasks;
using Serilog;
using System.IO;
using Nuke.Common.CI.GitHubActions;
using Octokit;
using Microsoft.AspNetCore.StaticFiles;
using System.Threading;
using System.Collections.Generic;

partial class Build
{
	[Parameter]
	readonly bool Major;

	[GitVersion] GitVersion GitVersion;

	[GitRepository] GitRepository Repository;

	[Parameter, Secret] readonly string GitHubToken = GitHubActions.Instance?.Token;

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

	private string ReleaseBranch => $"release/{MajorMinorPatchVersion}";

	Target EnsureReleaseBranch => _ => _
		.Executes(() =>
		{
			Thread.Sleep(1000);
		});


	Target Changelog => _ => _
		.Unlisted()
		.DependsOn(EnsureReleaseBranch, EnsureGithubClient)
		.Executes(async () =>
		{
			Touch(ChangelogFile);
			FinalizeChangelog(ChangelogFile, MajorMinorPatchVersion, Repository);

			var title = $"chore: Finalize {Path.GetFileName(ChangelogFile)} for {MajorMinorPatchVersion}";
			Git($"switch -c {ReleaseBranch}");
			Git($"add {ChangelogFile}");
			Git($""" commit -m "{title}" """,
				environmentVariables: new Dictionary<string, string>()
				{
					["GIT_COMMITTER_NAME"] = "changelog task",
					["GIT_COMMITTER_EMAIL"] = "changelog@task.com",
					["GIT_AUTHOR_NAME"] = "changelog task",
					["GIT_AUTHOR_EMAIL "] = "changelog@task.com",
				});
			Git($"push --set-upstream origin {ReleaseBranch}");
			var pr = await GitHubTasks.GitHubClient.PullRequest.Create(
				"BusHero",
				"nuke.github.release",
				new NewPullRequest(
				title,
				$"BusHero:{ReleaseBranch}",
				"master"
			));
			await GitHubTasks.GitHubClient.PullRequest.Merge(
				"BusHero",
				"nuke.github.release",
				pr.Number,
				new MergePullRequest
				{
					MergeMethod = PullRequestMergeMethod.Squash
				});
			Git("switch master");
			Git($"push origin --delete {ReleaseBranch}");
			Git($"branch -D {ReleaseBranch}");
			Git("pull");
		});

	Target EnsureGithubClient => _ => _
		.Requires(() => GitHubToken)
		.Executes(() =>
		{
			var credentials = new Credentials(GitHubToken);
			GitHubTasks.GitHubClient = new GitHubClient(
				new Octokit.ProductHeaderValue(nameof(NukeBuild)),
				new Octokit.Internal.InMemoryCredentialStore(credentials));
		});

	Target Release => _ => _
		.Requires(() => GitHubToken)
		.DependsOn(Zip, Changelog, EnsureReleaseBranch, EnsureGithubClient)
		.Triggers(Fetch)
		.Requires(() => Repository.IsOnMainOrMasterBranch())
		.Executes(async () =>
		{
			var release = new NewRelease(MajorMinorPatchVersion)
			{
				Name = $"Release {MajorMinorPatchVersion}",
				Draft = true,
				Body = $"""
					# This is the new release!

					Assets:
					{Path.GetFileName(Asset)} - {AssetChecksum}
					"""
			};
			var createdRelease = await GitHubTasks.GitHubClient.Repository.Release.Create(
				"BusHero",
				"nuke.github.release",
				release);

			UploadReleaseAssetToGithub(createdRelease, Asset);
			await GitHubTasks.GitHubClient.Repository.Release.Edit(
				"BusHero",
				"nuke.github.release",
				createdRelease.Id,
				new ReleaseUpdate
				{
					Draft = false,
				});
		});

	Target Fetch => _ => _
		.Executes(() =>
		{
			Git("fetch");
		});

	Target RemoveAsset => _ => _
		.Executes(() =>
		{
			DeleteFile(Asset);
		});

	private void UploadReleaseAssetToGithub(Release release, AbsolutePath asset)
	{
		if (!FileSystemTasks.FileExists(asset))
			return;

		if (!new FileExtensionContentTypeProvider()
			.TryGetContentType(asset, out var assetContentType))
		{
			assetContentType = "application/x-binary";
		}

		var releaseUpload = new ReleaseAssetUpload
		{
			ContentType = assetContentType,
			FileName = Path.GetFileName(asset),
			RawData = File.OpenRead(asset)
		};
		GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, releaseUpload);
	}
}
