export interface CatalogPlugin {
  id: string;
  displayName: string;
  description: string;
  publisher: string;
  version: string;
  repositoryUrl?: string;
  /** Publication group of the plugin catalog. It says nothing about the application domains where the plugin is used. */
  catalogGroup: 'Core' | 'HomeAutomation';
  category: 'MainServices' | 'Core';
  tags: string[];
  /** Ids of the plugins this one relies on, when the catalog announces them. */
  requires?: string[];
}

export const pluginCatalog: CatalogPlugin[] = [
  {
    id: 'core',
    displayName: 'MaNoir Platform Core',
    description: 'Services fondamentaux de la plateforme MaNoir et administration du mesh local.',
    publisher: 'MaNoir',
    version: '0.0.0-dev',
    repositoryUrl: 'https://github.com/manoir-app/MaNoir.Platform',
    catalogGroup: 'Core',
    category: 'MainServices',
    tags: ['core', 'platform', 'administration'],
  },
  {
    id: 'home-automation',
    displayName: 'Home Automation',
    description: 'Scènes, déclencheurs et intégrations pour automatiser la maison.',
    publisher: 'MaNoir',
    version: '0.0.0-dev',
    repositoryUrl: 'https://github.com/manoir-app/Manoir.HomeAutomation.Core',
    catalogGroup: 'HomeAutomation',
    category: 'Core',
    tags: ['home-automation', 'scenes', 'devices'],
  },
];

export const catalogGroups = ['all', ...new Set(pluginCatalog.map((plugin) => plugin.catalogGroup))];