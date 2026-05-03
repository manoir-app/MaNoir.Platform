import { Navigate, createBrowserRouter } from 'react-router';
import { App } from './App';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
import { ConsoleHomePage } from './pages/ConsoleHomePage';

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