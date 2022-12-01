using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.Tools.GitHub.GitHubTasks;
using static Nuke.Common.Tools.Git.GitTasks;
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

	Target EnsureChangelogFile => _ => _
		.Unlisted()
		.Executes(() =>
		{
			using var _ = File.CreateText(ChangelogFile);
		});

	Target Release => _ => _
		.Requires(() => GitHubToken)
		.Triggers(Fetch)
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
				Body = "Some body here and there"
			};
			var createdRelease = await GitHubTasks.GitHubClient.Repository.Release.Create(
				"BusHero",
				"nuke.github.release",
				release);

			UploadReleaseAssetToGithub(createdRelease, RootDirectory / "file.txt");
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
