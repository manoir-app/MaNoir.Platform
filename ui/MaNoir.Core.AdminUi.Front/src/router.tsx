import { Navigate, createBrowserRouter } from 'react-router';
import { App } from './App';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
import { ConsoleHomePage } from './pages/ConsoleHomePage';
import { NavigationPlaceholderPage } from './pages/NavigationPlaceholderPage';
import { PlatformHealthPage } from './pages/PlatformHealthPage';
import { PlatformLogsPage } from './pages/PlatformLogsPage';
import { InstalledPluginsPage } from './pages/InstalledPluginsPage';
import { PluginCatalogPage } from './pages/PluginCatalogPage';
import { MeshPlacesPage } from './pages/MeshPlacesPage';

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
              element: <Navigate replace to="/platform/surveillance/services" />,
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
              path: 'platform/house/screens',
              element: <NavigationPlaceholderPage kind="houseScreens" />,
            },
            {
              path: 'platform/house/local',
              element: <NavigationPlaceholderPage kind="houseLocal" />,
            },
            {
              path: 'platform/people/users',
              element: <NavigationPlaceholderPage kind="peopleUsers" />,
            },
            {
              path: 'platform/surveillance/logs',
              Component: PlatformLogsPage,
            },
            {
              path: 'platform/system/about',
              element: <NavigationPlaceholderPage kind="systemAbout" />,
            },
            {
              path: 'platform/surveillance/agents',
              element: <Navigate replace to="/platform/surveillance/services" />,
            },
            {
              path: 'platform/surveillance/services',
              Component: PlatformHealthPage,
            },
            {
              path: 'platform/extensions/catalog',
              Component: PluginCatalogPage,
            },
            {
              path: 'platform/extensions/installed',
              Component: InstalledPluginsPage,
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