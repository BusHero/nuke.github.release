using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.Tools.GitHub.GitHubTasks;
using Serilog;
using System.IO;
using Nuke.Common.CI.GitHubActions;
using System.Net.Http.Headers;
using Octokit;
using Microsoft.AspNetCore.StaticFiles;

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

	Target Release => _ => _
		.Executes(() =>
		{
			var credentials = new Credentials(GitHubActions.Token);
			GitHubTasks.GitHubClient = new GitHubClient(
				new Octokit.ProductHeaderValue(nameof(NukeBuild)),
				new Octokit.Internal.InMemoryCredentialStore(credentials));
			var release = new NewRelease(MajorMinorPatchVersion)
			{
				Name = $"Release {MajorMinorPatchVersion}",
				Prerelease = true,
				Body = "Some body here and there"
			};
			var createdRelease = GitHubTasks.GitHubClient.Repository.Release.Create(
				GitHubActions.RepositoryOwner,
				"nuke.github.release",
				release).Result;

			UploadReleaseAssetToGithub(createdRelease, RootDirectory / "file.txt");
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
