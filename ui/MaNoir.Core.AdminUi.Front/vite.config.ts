import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  base: '/front/',
  plugins: [react()],
  server: {
    port: 5175,
  },
});