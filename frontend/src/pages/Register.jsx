import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { userAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './Register.css'

const Register = () => {
  const navigate = useNavigate()
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phone: '',
    roleName: 'Kullanıcı' // 'Kullanıcı' veya 'Şirket'
  })
  const [errors, setErrors] = useState({})
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  const handleChange = (e) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))
    // Hata mesajını temizle
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }))
    }
  }

  const validateForm = () => {
    const newErrors = {}
    const nameLabel = formData.roleName === 'Şirket' ? 'Şirket Adı' : 'Ad Soyad'

    // Ad Soyad / Şirket Adı kontrolü
    if (!formData.fullName.trim()) {
      newErrors.fullName = `${nameLabel} gereklidir`
    } else if (formData.fullName.trim().length < 3) {
      newErrors.fullName = `${nameLabel} en az 3 karakter olmalıdır`
    }

    // Email kontrolü
    if (!formData.email.trim()) {
      newErrors.email = 'Email gereklidir'
    } else {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
      if (!emailRegex.test(formData.email)) {
        newErrors.email = 'Geçerli bir email adresi giriniz'
      }
    }

    // Şifre kontrolü
    if (!formData.password) {
      newErrors.password = 'Şifre gereklidir'
    } else if (formData.password.length < 6) {
      newErrors.password = 'Şifre en az 6 karakter olmalıdır'
    }

    // Şifre tekrar kontrolü
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Şifre tekrarı gereklidir'
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Şifreler eşleşmiyor'
    }

    // Telefon kontrolü (zorunlu)
    if (!formData.phone.trim()) {
      newErrors.phone = 'Telefon numarası gereklidir'
    } else {
      const phoneRegex = /^[0-9+\-\s()]+$/
      if (!phoneRegex.test(formData.phone)) {
        newErrors.phone = 'Geçerli bir telefon numarası giriniz'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    
    if (!validateForm()) {
      return
    }

    setLoading(true)
    setErrors({})

    try {
      const response = await userAPI.register({
        FullName: formData.fullName.trim(),
        Email: formData.email.trim().toLowerCase(),
        Password: formData.password,
        Phone: formData.phone.trim() || null,
        RoleName: formData.roleName
      })

      if (response.data?.Success || response.data?.success) {
        const userData = response.data.Data || response.data.data
        const token = userData?.Token || userData?.token
        
        // Token varsa localStorage'a kaydet
        if (token) {
          localStorage.setItem('raybus_token', token)
        }
        
        // Kullanıcı bilgilerini localStorage'a kaydet
        if (userData) {
          const userInfo = {
            UserID: userData.UserID,
            id: userData.UserID,
            email: userData.Email,
            name: userData.FullName,
            FullName: userData.FullName,
            role: userData.RoleName,
            RoleName: userData.RoleName
          }
          localStorage.setItem('raybus_user', JSON.stringify(userInfo))
        }
        
        setSuccess(true)
        setSnackbar({
          isOpen: true,
          message: 'Kayıt işlemi başarıyla tamamlandı! Ana sayfaya yönlendiriliyorsunuz...',
          type: 'success'
        })
        // 2 saniye sonra ana sayfaya yönlendir
        setTimeout(() => {
          navigate('/')
        }, 2000)
      } else {
        const errorMessage = response.data?.Message || response.data?.message || 'Kayıt işlemi başarısız oldu'
        setErrors({ 
          general: errorMessage,
          ...(response.data?.Errors || [])
        })
        setSnackbar({
          isOpen: true,
          message: errorMessage,
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Kayıt hatası:', error)
      console.error('Hata detayları:', {
        message: error.message,
        response: error.response,
        data: error.response?.data,
        status: error.response?.status
      })
      
      let errorMessage = 'Kayıt yapılırken bir hata oluştu'
      const errorsList = []
      
      // Backend'den gelen hata mesajını al
      if (error.response?.data) {
        const responseData = error.response.data
        
        // ApiResponse formatı
        if (responseData.Message || responseData.message) {
          errorMessage = responseData.Message || responseData.message
        }
        
        // Errors array'i varsa ekle
        if (responseData.Errors && Array.isArray(responseData.Errors)) {
          errorsList.push(...responseData.Errors)
        }
        
        // ModelState hataları varsa ekle
        if (responseData.errors) {
          Object.keys(responseData.errors).forEach(key => {
            const fieldErrors = responseData.errors[key]
            if (Array.isArray(fieldErrors)) {
              errorsList.push(...fieldErrors)
            }
          })
        }
      } else if (error.message) {
        errorMessage = error.message
      }
      
      // Network hatası
      if (!error.response) {
        errorMessage = 'Sunucuya ulaşılamadı. Backend çalışıyor mu?'
        errorsList.push('Lütfen backend\'in çalıştığından emin olun')
      }
      
      // Email zaten kullanılıyor kontrolü
      if (errorMessage.toLowerCase().includes('email') || 
          errorMessage.toLowerCase().includes('kullanılıyor') ||
          errorMessage.toLowerCase().includes('already')) {
        setErrors({ 
          email: 'Bu email adresi zaten kullanılıyor',
          general: errorsList.length > 0 ? errorsList.join(', ') : undefined
        })
        setSnackbar({
          isOpen: true,
          message: 'Bu email adresi zaten kullanılıyor. Lütfen farklı bir email adresi deneyin.',
          type: 'error'
        })
      } else if (errorMessage.toLowerCase().includes('rol') || 
                 errorMessage.toLowerCase().includes('role')) {
        setErrors({ 
          roleName: errorMessage,
          general: errorsList.length > 0 ? errorsList.join(', ') : undefined
        })
        setSnackbar({
          isOpen: true,
          message: errorMessage,
          type: 'error'
        })
      } else {
        const finalMessage = errorsList.length > 0 ? `${errorMessage}: ${errorsList.join(', ')}` : errorMessage
        setErrors({ 
          general: finalMessage
        })
        setSnackbar({
          isOpen: true,
          message: finalMessage,
          type: 'error'
        })
      }
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div className="register-page">
        <div className="register-background">
          <div className="register-container">
            <div className="success-card">
              <div className="success-icon">🎉</div>
              <h2>Kayıt Başarılı!</h2>
              <p>Hesabınız başarıyla oluşturuldu. Ana sayfaya yönlendiriliyorsunuz...</p>
              <div className="success-animation">
                <div className="checkmark">
                  <svg viewBox="0 0 52 52">
                    <circle cx="26" cy="26" r="25" fill="none" stroke="var(--primary-color)" strokeWidth="2"/>
                    <path fill="none" stroke="var(--primary-color)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" d="M14.1 27.2l7.1 7.2 16.7-16.8"/>
                  </svg>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="register-page">
      <div className="register-background">
        <div className="register-container">
          <div className="register-wrapper">
            <div className="register-left">
              <div className="register-hero">
                <div className="hero-icon">🚄</div>
                <h1>RayBus'a Hoş Geldiniz!</h1>
                <p>Hızlı, güvenli ve konforlu yolculuk deneyimi için hemen kayıt olun</p>
                <div className="hero-features">
                  <div className="feature-item">
                    <span className="feature-icon">✓</span>
                    <span>Güvenli ödeme</span>
                  </div>
                  <div className="feature-item">
                    <span className="feature-icon">✓</span>
                    <span>Anında rezervasyon</span>
                  </div>
                  <div className="feature-item">
                    <span className="feature-icon">✓</span>
                    <span>7/24 müşteri desteği</span>
                  </div>
                </div>
              </div>
            </div>
            
            <div className="register-right">
              <div className="register-card">
                <div className="register-header">
                  <h2>Hesap Oluştur</h2>
                  <p>Yolculuğunuza başlamak için bilgilerinizi girin</p>
                </div>

                {errors.general && (
                  <div className="error-message general-error">
                    <span className="error-icon">⚠️</span>
                    {errors.general}
                  </div>
                )}

                <form onSubmit={handleSubmit} className="register-form">
                  <div className="account-type-selector">
                    <label className="form-label">Hesap Tipi *</label>
                    <div className="type-options">
                      <button
                        type="button"
                        className={`type-option ${formData.roleName === 'Kullanıcı' ? 'active' : ''}`}
                        onClick={() => {
                          setFormData(prev => ({ ...prev, roleName: 'Kullanıcı' }))
                          if (errors.roleName) {
                            setErrors(prev => ({ ...prev, roleName: '' }))
                          }
                        }}
                      >
                        <span className="type-icon">👤</span>
                        <span className="type-label">Kullanıcı</span>
                      </button>
                      <button
                        type="button"
                        className={`type-option ${formData.roleName === 'Şirket' ? 'active' : ''}`}
                        onClick={() => {
                          setFormData(prev => ({ ...prev, roleName: 'Şirket' }))
                          if (errors.roleName) {
                            setErrors(prev => ({ ...prev, roleName: '' }))
                          }
                        }}
                      >
                        <span className="type-icon">🏢</span>
                        <span className="type-label">Şirket</span>
                      </button>
                    </div>
                    {errors.roleName && <span className="error-text">{errors.roleName}</span>}
                  </div>

                  <div className="form-group">
                    <label htmlFor="fullName" className="form-label">
                      <span className="label-icon">👤</span>
                      {formData.roleName === 'Şirket' ? 'Şirket Adı *' : 'Ad Soyad *'}
                    </label>
                    <div className="input-wrapper">
                      <input
                        type="text"
                        id="fullName"
                        name="fullName"
                        value={formData.fullName}
                        onChange={handleChange}
                        placeholder={formData.roleName === 'Şirket' ? 'Şirket adınız' : 'Adınız ve Soyadınız'}
                        required
                        className={errors.fullName ? 'error' : ''}
                      />
                    </div>
                    {errors.fullName && <span className="error-text">{errors.fullName}</span>}
                  </div>

                  <div className="form-group">
                    <label htmlFor="email" className="form-label">
                      <span className="label-icon">📧</span>
                      E-posta *
                    </label>
                    <div className="input-wrapper">
                      <input
                        type="email"
                        id="email"
                        name="email"
                        value={formData.email}
                        onChange={handleChange}
                        placeholder="ornek@email.com"
                        required
                        className={errors.email ? 'error' : ''}
                      />
                    </div>
                    {errors.email && <span className="error-text">{errors.email}</span>}
                  </div>

                  <div className="form-group">
                    <label htmlFor="phone" className="form-label">
                      <span className="label-icon">📱</span>
                      Telefon *
                    </label>
                    <div className="input-wrapper">
                      <input
                        type="tel"
                        id="phone"
                        name="phone"
                        value={formData.phone}
                        onChange={handleChange}
                        placeholder="0555 123 45 67"
                        required
                        className={errors.phone ? 'error' : ''}
                      />
                    </div>
                    {errors.phone && <span className="error-text">{errors.phone}</span>}
                  </div>

                  <div className="form-group">
                    <label htmlFor="password" className="form-label">
                      <span className="label-icon">🔒</span>
                      Şifre *
                    </label>
                    <div className="input-wrapper">
                      <input
                        type="password"
                        id="password"
                        name="password"
                        value={formData.password}
                        onChange={handleChange}
                        placeholder="En az 6 karakter"
                        required
                        className={errors.password ? 'error' : ''}
                      />
                    </div>
                    {errors.password && <span className="error-text">{errors.password}</span>}
                  </div>

                  <div className="form-group">
                    <label htmlFor="confirmPassword" className="form-label">
                      <span className="label-icon">🔐</span>
                      Şifre Tekrar *
                    </label>
                    <div className="input-wrapper">
                      <input
                        type="password"
                        id="confirmPassword"
                        name="confirmPassword"
                        value={formData.confirmPassword}
                        onChange={handleChange}
                        placeholder="Şifrenizi tekrar giriniz"
                        required
                        className={errors.confirmPassword ? 'error' : ''}
                      />
                    </div>
                    {errors.confirmPassword && <span className="error-text">{errors.confirmPassword}</span>}
                  </div>

                  <button 
                    type="submit" 
                    className="btn btn-primary register-submit" 
                    disabled={loading}
                  >
                    {loading ? (
                      <>
                        <span className="spinner"></span>
                        Kayıt yapılıyor...
                      </>
                    ) : (
                      <>
                        <span>✓</span>
                        Kayıt Ol
                      </>
                    )}
                  </button>
                </form>

                <div className="register-footer">
                  <p>
                    Zaten hesabınız var mı?{' '}
                    <Link to="/" className="login-link">
                      Giriş Yap
                    </Link>
                  </p>
                </div>
              </div>
            </div>
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

export default Register

