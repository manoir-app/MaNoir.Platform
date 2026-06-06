import { Navigate, createBrowserRouter } from 'react-router';
import { App } from './App';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
import { AgentRegistryPage } from './pages/AgentRegistryPage';
import { ConsoleHomePage } from './pages/ConsoleHomePage';
import { MeshPlacesPage } from './pages/MeshPlacesPage';
import { NavigationPlaceholderPage } from './pages/NavigationPlaceholderPage';
import { PlatformHealthPage } from './pages/PlatformHealthPage';

const routes = [
    {
      path: '/',
      Component: App,
      children: [
        {
          Component: AuthenticatedLayout,
          children: [
            {
              index: true,
              element: <Navigate replace to="/platform/mesh/status" />,
            },
            {
              path: 'console',
              element: <Navigate replace to="/platform/mesh/status" />,
            },
            {
              path: 'system/health',
              element: <Navigate replace to="/platform/surveillance/services" />,
            },
            {
              path: 'system/agents',
              element: <Navigate replace to="/platform/surveillance/agents" />,
            },
            {
              path: 'platform',
              element: <Navigate replace to="/platform/mesh/status" />,
            },
            {
              path: 'platform/mesh/status',
              Component: ConsoleHomePage,
            },
            {
              path: 'platform/mesh/places',
              Component: MeshPlacesPage,
            },
            {
              path: 'platform/surveillance/agents',
              Component: AgentRegistryPage,
            },
            {
              path: 'platform/surveillance/services',
              Component: PlatformHealthPage,
            },
            {
              path: 'platform/extensions/catalog',
              element: (
                <NavigationPlaceholderPage
                  descriptionKey="placeholders.extensionsCatalog.description"
                  eyebrowKey="placeholders.extensionsCatalog.eyebrow"
                  titleKey="placeholders.extensionsCatalog.title"
                />
              ),
            },
          ],
        },
        {
          path: '*',
          element: <Navigate replace to="/" />,
        },
      ],
    },
  ];

export function createAdminRouter(basename: string) {
  return createBrowserRouter(routes, { basename });
}