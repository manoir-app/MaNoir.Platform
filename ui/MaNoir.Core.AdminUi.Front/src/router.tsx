import { Navigate, createBrowserRouter } from 'react-router';
import { App } from './App';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
import { AgentRegistryPage } from './pages/AgentRegistryPage';
import { ConsoleHomePage } from './pages/ConsoleHomePage';
import { PlatformHealthPage } from './pages/PlatformHealthPage';

export const router = createBrowserRouter(
  [
    {
      path: '/',
      Component: App,
      children: [
        {
          Component: AuthenticatedLayout,
          children: [
            {
              index: true,
              Component: ConsoleHomePage,
            },
            {
              path: 'console',
              Component: ConsoleHomePage,
            },
            {
              path: 'system/health',
              Component: PlatformHealthPage,
            },
            {
              path: 'system/agents',
              Component: AgentRegistryPage,
            },
          ],
        },
        {
          path: '*',
          element: <Navigate replace to="/" />,
        },
      ],
    },
  ],
  {
    basename: import.meta.env.BASE_URL,
  },
);