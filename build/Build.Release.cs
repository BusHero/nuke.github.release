using FluentAssertions.Execution;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Octokit;
using Serilog;

partial class Build
{
	[Parameter]
	readonly bool Major;

	[GitVersion] GitVersion GitVersion;

	[GitRepository] GitRepository GitRepository;

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

			using var _ = new AssertionScope();
			Assert.True(milestone.OpenIssues == 0);
			Assert.True(milestone.ClosedIssues != 0);
			Assert.True(milestone.State == ItemState.Closed);
		});
}
