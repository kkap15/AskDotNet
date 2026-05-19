import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'AskDotNet',
        short_name: 'AskDotNet',
        description: 'C# documentation assistant',
        theme_color: '#0a0a0a',
        background_color: '#0a0a0a',
        display: 'standalone',
        orientation: 'portrait',
        icons: [
          {
            src: '/frontend/public/icon-192.png',
            sizes: '192x192',
            type: 'images/png'
          },
          {
            src: '/frontend/public/icon-512.png',
            sizes: '512x512',
            type: 'images/png',
          },
          {
            src: '/frontend/public/apple-touch-icon.png',
            sizes: '180x180',
            type: 'images/png'
          }
        ],
      },
    }),
  ],
})
