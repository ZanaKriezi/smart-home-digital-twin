import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// During development, /api calls are forwarded to the ASP.NET Core API.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: { '/api': 'http://localhost:5080' },
  },
});
