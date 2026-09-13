using MaNoir.Core.Contracts.Models.Contributions;
using System.Collections.Generic;
using System.Linq;

namespace MaNoir.Core.Contributions;

public static class PluginHealthEvaluator
{
    public static bool IsHealthy(IReadOnlyCollection<DeployedComponent> components)
    {
        return components != null && components.Any(component =>
            component?.Status is DeployedComponentStatus.Running or DeployedComponentStatus.Healthy);
    }
}