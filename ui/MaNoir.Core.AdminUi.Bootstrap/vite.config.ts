import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_CORE_API_PROXY_TARGET?.trim() || 'http://localhost:5243';

  return {
    base: '/bootstrap/',
    plugins: [react()],
    resolve: {
      dedupe: ['react', 'react-dom'],
      alias: {
        '@manoir-app/core-admin-ui-kit': fileURLToPath(new URL('../MaNoir.Core.AdminUi.Kit/src/index.ts', import.meta.url)),
        '@radix-ui/react-label': fileURLToPath(new URL('./node_modules/@radix-ui/react-label/dist/index.js', import.meta.url)),
        '@radix-ui/react-slot': fileURLToPath(new URL('./node_modules/@radix-ui/react-slot/dist/index.js', import.meta.url)),
        react: fileURLToPath(new URL('./node_modules/react/', import.meta.url)),
        'react-dom': fileURLToPath(new URL('./node_modules/react-dom/', import.meta.url)),
      },
    },
    server: {
      port: 5174,
      proxy: {
        '/api/core': {
          target: proxyTarget,
          changeOrigin: false,
          secure: false,
        },
      },
    },
  };
});