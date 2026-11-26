import { useTranslation } from 'react-i18next';
import { StatusPill } from '@manoir-app/core-admin-ui-kit/status-pill';
import type { InstalledPluginModel } from '../lib/api';

interface PluginRequirementsProps {
  installedPlugins: InstalledPluginModel[];
  requiredPluginIds: string[];
}

/** Shows one "requires X" hint per required plugin, with whether the requirement is currently satisfied. */
export function PluginRequirements({ installedPlugins, requiredPluginIds }: PluginRequirementsProps) {
  const { t } = useTranslation();

  return requiredPluginIds.map((pluginId) => {
    const plugin = installedPlugins.find((candidate) => candidate.id.toLowerCase() === pluginId.toLowerCase());
    // Same rule as the Core API navigation: installed, enabled and healthy.
    const isSatisfied = Boolean(plugin?.isEnabled && plugin.isHealthy);

    return (
      <StatusPill key={pluginId} tone={isSatisfied ? 'success' : 'warning'}>
        {t(isSatisfied ? 'extensions.requirements.satisfied' : 'extensions.requirements.unsatisfied', { plugin: plugin?.label || pluginId })}
      </StatusPill>
    );
  });
}
