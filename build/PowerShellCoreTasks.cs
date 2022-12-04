using Nuke.Common.Tools.PowerShell;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.PowerShell.PowerShellTasks;
using System.Collections.Generic;

public static class PowerShellCoreTasks
{
	public static IReadOnlyCollection<Output> PowerShellCore(
		Configure<PowerShellSettings> configurator) => PowerShell(settings => configurator(settings)
			.SetProcessToolPath("pwsh")
			.SetNoProfile(true)
			.SetNoLogo(true));
}
