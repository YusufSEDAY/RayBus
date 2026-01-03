import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    open: true,
    proxy: {
      '/api': {
        // Backend'in çalıştığı portu kontrol et
        // Backend HTTPS'de çalışıyor (https://localhost:7049)
        target: 'https://localhost:7049', // Backend HTTPS portu
        changeOrigin: true,
        secure: false, // HTTPS için self-signed certificate'ları kabul et
        ws: true, // WebSocket desteği
        rewrite: (path) => path.replace(/^\/api/, '/api'), // Path'i olduğu gibi bırak
        configure: (proxy, _options) => {
          // Sadece development'ta ve kritik hatalarda log
          if (process.env.NODE_ENV === 'development') {
            proxy.on('error', (err, req) => {
              console.error('❌ Proxy hatası:', err.message, req.url)
            })
            
            proxy.on('proxyRes', (proxyRes, req) => {
              // Sadece hata durumlarında log
              if (proxyRes.statusCode >= 400) {
                console.error(`❌ Proxy: ${proxyRes.statusCode} - ${req.url}`)
              }
            })
          }
        }
      }
    }
  }
})

