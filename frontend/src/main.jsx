import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'

console.log('🚀 RayBus Frontend başlatılıyor...')

try {
  const rootElement = document.getElementById('root')
  if (!rootElement) {
    throw new Error('Root element bulunamadı!')
  }
  
  const root = ReactDOM.createRoot(rootElement)
  root.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  )
  console.log('✅ RayBus Frontend başarıyla yüklendi!')
} catch (error) {
  console.error('❌ RayBus Frontend yüklenirken hata:', error)
  document.body.innerHTML = `
    <div style="padding: 20px; color: white; background: #1e293b; min-height: 100vh;">
      <h1>❌ Hata Oluştu</h1>
      <p>${error.message}</p>
      <p>Lütfen tarayıcı console'unu (F12) açın ve hata detaylarını kontrol edin.</p>
    </div>
  `
}


