import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Snackbar from './Snackbar'
import './LoginModal.css'

const LoginModal = ({ isOpen, onClose, onLogin }) => {
  const navigate = useNavigate()
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    })
    setError('')
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    if (!formData.email || !formData.password) {
      setError('Lütfen tüm alanları doldurun')
      return
    }

    setLoading(true)
    try {
      const response = await axios.post('/api/user/login', {
        email: formData.email,
        password: formData.password
      })

      // Backend PascalCase döndürüyor: {Success: true, Message: "...", Data: {...}, Errors: []}
      const success = response.data?.Success || response.data?.success
      const userData = response.data?.Data || response.data?.data
      const token = userData?.Token || userData?.token
      
      console.log('🔍 Login response:', response.data)
      console.log('🔍 UserData:', userData)
      
      if (success && userData) {
        // UserID kontrolü
        const userId = userData.UserID ?? userData.userID ?? userData.UserId ?? userData.userId
        if (!userId || userId <= 0) {
          console.error('❌ Geçersiz UserID:', userId, 'userData:', userData)
          const errorMsg = 'Kullanıcı bilgisi alınamadı. Lütfen tekrar deneyin.'
          setError(errorMsg)
          setSnackbar({
            isOpen: true,
            message: errorMsg,
            type: 'error'
          })
          return
        }
        
        // Token varsa localStorage'a kaydet
        if (token) {
          // Token formatını kontrol et
          const tokenParts = token.split('.')
          if (tokenParts.length !== 3) {
            console.error('❌ Geçersiz token formatı! Token 3 parçadan oluşmalı (JWT)')
            console.error('Token:', token)
            const errorMsg = 'Token formatı geçersiz. Lütfen tekrar deneyin.'
            setError(errorMsg)
            setSnackbar({
              isOpen: true,
              message: errorMsg,
              type: 'error'
            })
            return
          }
          
          // Token'ı decode et ve kontrol et
          try {
            const payload = JSON.parse(atob(tokenParts[1]))
            console.log('✅ Token decode başarılı:', {
              exp: payload.exp,
              expDate: new Date(payload.exp * 1000),
              now: new Date(),
              role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
            })
          } catch (e) {
            console.error('❌ Token decode hatası:', e)
            const errorMsg = 'Token işlenirken hata oluştu. Lütfen tekrar deneyin.'
            setError(errorMsg)
            setSnackbar({
              isOpen: true,
              message: errorMsg,
              type: 'error'
            })
            return
          }
          
          localStorage.setItem('raybus_token', token)
          console.log('✅ Token localStorage\'a kaydedildi:', token.substring(0, 50) + '...')
        } else {
          console.warn('⚠️ Login response\'da token yok!')
          const errorMsg = 'Token alınamadı. Lütfen tekrar deneyin.'
          setError(errorMsg)
          setSnackbar({
            isOpen: true,
            message: errorMsg,
            type: 'error'
          })
          return
        }
        
        const roleName = userData.RoleName ?? userData.roleName ?? ''
        
        console.log('🔍 Login - RoleName kontrolü:', {
          userData,
          roleName,
          userDataKeys: Object.keys(userData || {})
        })
        
        const loginUserData = {
          UserID: userId,
          id: userId, // Backward compatibility
          email: userData.Email ?? userData.email ?? '',
          name: userData.FullName ?? userData.fullName ?? '',
          FullName: userData.FullName ?? userData.fullName ?? '',
          role: roleName,
          RoleName: roleName,
          roleName: roleName // Tüm varyasyonları ekle
        }
        
        console.log('🔍 LoginUserData (localStorage\'a kaydedilecek):', loginUserData)
        
        setSnackbar({
          isOpen: true,
          message: 'Giriş başarılı! Hoş geldiniz.',
          type: 'success'
        })
        onLogin(loginUserData)
        setTimeout(() => {
          onClose()
          setFormData({ email: '', password: '' })
          setError('')
        }, 1000)
      } else {
        const errorMessage = response.data?.Message || response.data?.message || 'Giriş başarısız'
        setError(errorMessage)
        setSnackbar({
          isOpen: true,
          message: errorMessage,
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Giriş hatası:', error)
      const errorMessage = error.response?.data?.message || error.response?.data?.Message || 'Giriş yapılırken bir hata oluştu'
      setError(errorMessage)
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  if (!isOpen) return null

  return (
    <div className="login-modal-overlay" onClick={onClose}>
      <div className="login-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="login-modal-header">
          <h2>🔐 Giriş Yap</h2>
          <button className="login-modal-close" onClick={onClose}>×</button>
        </div>
        
        <div className="login-modal-body">
          <p className="login-subtitle">Hesabınıza giriş yaparak seyahatlerinize devam edin</p>

          <form onSubmit={handleSubmit} className="login-form">
            {error && (
              <div className="login-error-message">
                <span className="error-icon">⚠️</span>
                {error}
              </div>
            )}

            <div className="form-group">
              <label htmlFor="email">E-posta Adresi</label>
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="ornek@email.com"
                className={error && !formData.email ? 'error' : ''}
                required
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">Şifre</label>
              <input
                type="password"
                id="password"
                name="password"
                value={formData.password}
                onChange={handleChange}
                placeholder="••••••••"
                className={error && !formData.password ? 'error' : ''}
                required
                disabled={loading}
              />
            </div>

            <div className="login-modal-actions">
              <button
                type="submit"
                className="btn btn-primary login-submit"
                disabled={loading}
              >
                {loading ? (
                  <>
                    <span className="spinner"></span>
                    Giriş yapılıyor...
                  </>
                ) : (
                  '🚀 Giriş Yap'
                )}
              </button>
            </div>
          </form>

          <div className="login-modal-footer">
            <p className="footer-text">
              Hesabınız yok mu?{' '}
              <a 
                href="#" 
                onClick={(e) => { 
                  e.preventDefault(); 
                  onClose(); 
                  navigate('/register'); 
                }}
                className="register-link"
              >
                Kayıt Ol
              </a>
            </p>
          </div>
        </div>
      </div>
      
      <Snackbar
        isOpen={snackbar.isOpen}
        message={snackbar.message}
        type={snackbar.type}
        onClose={() => setSnackbar({ ...snackbar, isOpen: false })}
      />
    </div>
  )
}

export default LoginModal
