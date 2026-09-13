export interface CatalogPlugin {
  id: string;
  displayName: string;
  description: string;
  publisher: string;
  version: string;
  repositoryUrl?: string;
  domain: 'Core' | 'HomeAutomation';
  category: 'MainServices' | 'Core';
  tags: string[];
}

export const pluginCatalog: CatalogPlugin[] = [
  {
    id: 'core',
    displayName: 'MaNoir Platform Core',
    description: 'Services fondamentaux de la plateforme MaNoir et administration du mesh local.',
    publisher: 'MaNoir',
    version: '0.0.0-dev',
    repositoryUrl: 'https://github.com/manoir-app/MaNoir.Platform',
    domain: 'Core',
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
    domain: 'HomeAutomation',
    category: 'Core',
    tags: ['home-automation', 'scenes', 'devices'],
  },
];

export const catalogDomains = ['all', ...new Set(pluginCatalog.map((plugin) => plugin.domain))];