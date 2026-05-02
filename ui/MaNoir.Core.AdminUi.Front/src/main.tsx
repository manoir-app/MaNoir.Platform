import React from 'react';
import ReactDOM from 'react-dom/client';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/tokens.css';
import '../../MaNoir.Core.AdminUi.Kit/src/styles/base.css';
import { App } from './App';

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);