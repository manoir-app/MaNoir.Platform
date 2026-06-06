import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router/dom';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/tokens.css';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/base.css';
import './i18n';
import { createAdminRouter } from './router';
import { getRouterBasePath } from './runtimeConfig';

const router = createAdminRouter(getRouterBasePath());

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>,
);