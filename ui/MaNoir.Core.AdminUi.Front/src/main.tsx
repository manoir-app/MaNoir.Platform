import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router/dom';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/tokens.css';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/base.css';
import './i18n';
import { router } from './router';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>,
);