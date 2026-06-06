import { resolve } from 'node:path';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_CORE_API_PROXY_TARGET?.trim() || 'http://localhost:5243';
  const frontNodeModules = resolve(__dirname, 'node_modules');

  return {
    base: '/front/',
    plugins: [react()],
    resolve: {
      alias: {
        '@manoir-app/core-admin-ui-kit/app-brand-header': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/AppBrandHeader.tsx'),
        '@manoir-app/core-admin-ui-kit/auth-session-store': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/stores/authSessionStore.ts'),
        '@manoir-app/core-admin-ui-kit/default-admin-shell': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/DefaultAdminShell.tsx'),
        '@manoir-app/core-admin-ui-kit/dialog': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/Dialog.tsx'),
        '@manoir-app/core-admin-ui-kit/inline-tabs': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/InlineTabs.tsx'),
        '@manoir-app/core-admin-ui-kit/login-page': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/LoginPage.tsx'),
        '@manoir-app/core-admin-ui-kit/shell-header': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/ShellHeader.tsx'),
        '@manoir-app/core-admin-ui-kit/sidebar-nav': resolve(__dirname, '../MaNoir.Core.AdminUi.Kit/src/components/SidebarNav.tsx'),
        react: resolve(frontNodeModules, 'react'),
        'react-dom': resolve(frontNodeModules, 'react-dom'),
        'react/jsx-dev-runtime': resolve(frontNodeModules, 'react/jsx-dev-runtime.js'),
        'react/jsx-runtime': resolve(frontNodeModules, 'react/jsx-runtime.js'),
      },
      dedupe: ['react', 'react-dom'],
    },
    server: {
      port: 5175,
      proxy: {
        '/api/core': {
          target: proxyTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  };
});