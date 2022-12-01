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

	Target Changelog => _ => _
		.Executes(() =>
		{
			Touch(ChangelogFile);
			FinalizeChangelog(ChangelogFile, MajorMinorPatchVersion, Repository);

			Git($"add {ChangelogFile}");
			Git($"""
			commit -m "chore: Finalize {Path.GetFileName(ChangelogFile)} for {MajorMinorPatchVersion}"
			""");
		});

	Target Release => _ => _
		.Requires(() => GitHubToken)
		.DependsOn(Zip)
		.DependsOn(Changelog)
		.Triggers(Fetch)
		.Requires(() => Repository.IsOnMainOrMasterBranch())
		.Executes(async () =>
		{
			var credentials = new Credentials(GitHubToken);
			GitHubTasks.GitHubClient = new GitHubClient(
				new Octokit.ProductHeaderValue(nameof(NukeBuild)),
				new Octokit.Internal.InMemoryCredentialStore(credentials));
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
