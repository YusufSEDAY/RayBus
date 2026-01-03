import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { userAPI, userStatisticsAPI, notificationAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './Profile.css'

const Profile = () => {
  const navigate = useNavigate()
  const [user, setUser] = useState(null)
  const [formData, setFormData] = useState({
    FullName: '',
    Email: '',
    Phone: '',
    CurrentPassword: '',
    NewPassword: '',
    ConfirmPassword: ''
  })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })
  const [activeTab, setActiveTab] = useState('profile')
  const [userStatistics, setUserStatistics] = useState(null)
  const [notificationPreferences, setNotificationPreferences] = useState(null)
  const [loadingStats, setLoadingStats] = useState(false)
  const [loadingNotifications, setLoadingNotifications] = useState(false)

  useEffect(() => {
    const loadUserData = async () => {
      const savedUser = JSON.parse(localStorage.getItem('raybus_user') || 'null')
      if (!savedUser) {
        navigate('/')
        return
      }

      setUser(savedUser)
      setFormData({
        FullName: savedUser.FullName || savedUser.fullName || '',
        Email: savedUser.Email || savedUser.email || '',
        Phone: savedUser.Phone || savedUser.phone || '',
        CurrentPassword: '',
        NewPassword: '',
        ConfirmPassword: ''
      })

      // CreatedAt yoksa veya geçersizse backend'den çek
      const hasValidCreatedAt = savedUser.CreatedAt || savedUser.createdAt
      const createdAtValue = savedUser.CreatedAt || savedUser.createdAt
      
      if (!hasValidCreatedAt || 
          createdAtValue === '0001-01-01T00:00:00' || 
          createdAtValue === '1970-01-01T00:00:00Z' ||
          createdAtValue === 'Yükleniyor...') {
        await fetchUserData(savedUser.UserID || savedUser.userID || savedUser.id)
      } else {
        setLoading(false)
      }
    }

    loadUserData()
  }, [navigate])

  useEffect(() => {
    if (user && activeTab === 'statistics') {
      fetchUserStatistics()
    } else if (user && activeTab === 'notifications') {
      fetchNotificationPreferences()
    }
  }, [user, activeTab])

  // Debug: userStatistics state değişikliklerini izle
  useEffect(() => {
    if (userStatistics) {
      console.log('🔄 userStatistics state güncellendi:', userStatistics)
      console.log('📊 Değerler:', {
        toplamHarcama: userStatistics.toplamHarcama,
        toplamSeyahatSayisi: userStatistics.toplamSeyahatSayisi,
        gelecekSeyahatSayisi: userStatistics.gelecekSeyahatSayisi,
        gecmisSeyahatSayisi: userStatistics.gecmisSeyahatSayisi,
        toplamRezervasyonSayisi: userStatistics.toplamRezervasyonSayisi
      })
    }
  }, [userStatistics])

  const fetchUserStatistics = async () => {
    if (!user) return
    setLoadingStats(true)
    try {
      const userId = user.UserID || user.userID || user.id
      console.log('📊 İstatistikler getiriliyor. UserID:', userId)
      const response = await userStatisticsAPI.getStatistics(userId)
      console.log('📊 API Response (raw):', JSON.stringify(response.data, null, 2))
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      console.log('📊 Success:', success, 'Data:', data)
      console.log('📊 Data detayları:', {
        toplamHarcama: data?.toplamHarcama,
        toplamSeyahatSayisi: data?.toplamSeyahatSayisi,
        gelecekSeyahatSayisi: data?.gelecekSeyahatSayisi,
        gecmisSeyahatSayisi: data?.gecmisSeyahatSayisi,
        toplamRezervasyonSayisi: data?.toplamRezervasyonSayisi
      })
      
      if (success && data) {
        console.log('📊 İstatistikler set ediliyor:', data)
        setUserStatistics(data)
      } else {
        console.warn('⚠️ İstatistikler başarısız veya veri yok:', { success, data })
      }
    } catch (error) {
      console.error('❌ İstatistikler yüklenirken hata:', error)
      console.error('❌ Error details:', error.response?.data)
    } finally {
      setLoadingStats(false)
    }
  }

  const fetchNotificationPreferences = async () => {
    if (!user) return
    setLoadingNotifications(true)
    try {
      const userId = user.UserID || user.userID || user.id
      const response = await notificationAPI.getUserPreferences(userId)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        // Backend'den dönen değerleri normalize et (camelCase veya PascalCase olabilir)
        const normalizedData = {
          PreferenceID: data.PreferenceID || data.preferenceID,
          UserID: data.UserID || data.userID,
          EmailNotifications: data.EmailNotifications ?? data.emailNotifications ?? true,
          SMSNotifications: data.SMSNotifications ?? data.smsNotifications ?? false,
          ReservationNotifications: data.ReservationNotifications ?? data.reservationNotifications ?? true,
          PaymentNotifications: data.PaymentNotifications ?? data.paymentNotifications ?? true,
          CancellationNotifications: data.CancellationNotifications ?? data.cancellationNotifications ?? true,
          ReminderNotifications: data.ReminderNotifications ?? data.reminderNotifications ?? true,
          UpdatedAt: data.UpdatedAt || data.updatedAt
        }
        console.log('📧 Bildirim tercihleri yüklendi:', normalizedData)
        setNotificationPreferences(normalizedData)
      }
    } catch (error) {
      console.error('Bildirim tercihleri yüklenirken hata:', error)
    } finally {
      setLoadingNotifications(false)
    }
  }

  const handleUpdateNotificationPreferences = async () => {
    if (!user || !notificationPreferences) return
    setSaving(true)
    try {
      const userId = user.UserID || user.userID || user.id
      console.log('📤 Güncellenecek tercihler:', notificationPreferences)
      const response = await notificationAPI.updateUserPreferences(userId, notificationPreferences)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success) {
        // Backend'den dönen güncel değerleri state'e set et
        if (data) {
          // Backend'den dönen değerleri normalize et (camelCase veya PascalCase olabilir)
          const normalizedData = {
            PreferenceID: data.PreferenceID || data.preferenceID,
            UserID: data.UserID || data.userID,
            EmailNotifications: data.EmailNotifications ?? data.emailNotifications ?? true,
            SMSNotifications: data.SMSNotifications ?? data.smsNotifications ?? false,
            ReservationNotifications: data.ReservationNotifications ?? data.reservationNotifications ?? true,
            PaymentNotifications: data.PaymentNotifications ?? data.paymentNotifications ?? true,
            CancellationNotifications: data.CancellationNotifications ?? data.cancellationNotifications ?? true,
            ReminderNotifications: data.ReminderNotifications ?? data.reminderNotifications ?? true,
            UpdatedAt: data.UpdatedAt || data.updatedAt
          }
          console.log('📧 Backend\'den dönen değerler:', data)
          console.log('📧 Normalize edilmiş değerler:', normalizedData)
          setNotificationPreferences(normalizedData)
        }
        setSnackbar({
          isOpen: true,
          message: 'Bildirim tercihleri başarıyla güncellendi',
          type: 'success'
        })
      } else {
        setSnackbar({
          isOpen: true,
          message: 'Bildirim tercihleri güncellenemedi',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Bildirim tercihleri güncellenirken hata:', error)
      setSnackbar({
        isOpen: true,
        message: 'Bildirim tercihleri güncellenirken bir hata oluştu',
        type: 'error'
      })
    } finally {
      setSaving(false)
    }
  }

  const fetchUserData = async (userId) => {
    try {
      setLoading(true)
      const response = await userAPI.getUser(userId)
      const success = response.data?.Success ?? response.data?.success
      const userData = response.data?.Data ?? response.data?.data
      
      if (success && userData) {
        // localStorage'dan mevcut kullanıcı verilerini al
        const savedUser = JSON.parse(localStorage.getItem('raybus_user') || '{}')
        
        // Mevcut kullanıcı verilerini güncelle
        const updatedUser = {
          ...savedUser,
          ...userData,
          CreatedAt: userData.CreatedAt ?? userData.createdAt ?? savedUser.CreatedAt ?? savedUser.createdAt
        }
        
        localStorage.setItem('raybus_user', JSON.stringify(updatedUser))
        setUser(updatedUser)
        
        // FormData'yı da güncelle
        setFormData(prev => ({
          ...prev,
          FullName: updatedUser.FullName || updatedUser.fullName || prev.FullName,
          Email: updatedUser.Email || updatedUser.email || prev.Email,
          Phone: updatedUser.Phone || updatedUser.phone || prev.Phone
        }))
      }
    } catch (error) {
      console.error('Kullanıcı bilgileri yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setSaving(true)

    try {
      const updateData = {
        FullName: formData.FullName || null,
        Email: formData.Email || null,
        Phone: formData.Phone || null,
        CurrentPassword: formData.CurrentPassword || null,
        NewPassword: formData.NewPassword || null
      }

      // Şifre değişikliği validasyonu
      if (updateData.NewPassword) {
        if (!updateData.CurrentPassword) {
          setSnackbar({
            isOpen: true,
            message: 'Şifre değiştirmek için mevcut şifrenizi girmelisiniz',
            type: 'error'
          })
          setSaving(false)
          return
        }

        if (updateData.NewPassword.length < 6) {
          setSnackbar({
            isOpen: true,
            message: 'Yeni şifre en az 6 karakter olmalıdır',
            type: 'error'
          })
          setSaving(false)
          return
        }

        if (updateData.NewPassword === updateData.CurrentPassword) {
          setSnackbar({
            isOpen: true,
            message: 'Yeni şifre mevcut şifre ile aynı olamaz',
            type: 'error'
          })
          setSaving(false)
          return
        }

        if (updateData.NewPassword !== formData.ConfirmPassword) {
          setSnackbar({
            isOpen: true,
            message: 'Yeni şifre ve onay şifresi eşleşmiyor',
            type: 'error'
          })
          setSaving(false)
          return
        }
      }

      const response = await userAPI.updateProfile(user.UserID || user.id, updateData)

      if (response.data?.success) {
        const updatedUser = response.data.data

        // Token varsa güncelle
        if (updatedUser.Token) {
          localStorage.setItem('raybus_token', updatedUser.Token)
        }

        localStorage.setItem('raybus_user', JSON.stringify(updatedUser))
        setUser(updatedUser)

        setSnackbar({
          isOpen: true,
          message: 'Profil bilgileriniz başarıyla güncellendi',
          type: 'success'
        })

        // Şifre alanlarını temizle
        setFormData(prev => ({
          ...prev,
          CurrentPassword: '',
          NewPassword: '',
          ConfirmPassword: ''
        }))
      } else {
        setSnackbar({
          isOpen: true,
          message: response.data?.message || 'Profil güncellenirken bir hata oluştu',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Profil güncelleme hatası:', error)
      const errorMessage = error.response?.data?.message || 
                           'Profil güncellenirken bir hata oluştu'
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="profile-page">
        <div className="container">
          <div className="card">
            <p className="info-text">Yükleniyor...</p>
          </div>
        </div>
      </div>
    )
  }

  if (!user) {
    return null
  }

  return (
    <div className="profile-page">
      <div className="container">
        <div className="profile-header">
          <h1 className="page-title">👤 Profilim</h1>
          <p className="page-subtitle">Hesap bilgilerinizi yönetin</p>
        </div>

        {/* Tab Navigation */}
        <div className="profile-tabs">
          <button
            className={`tab-btn ${activeTab === 'profile' ? 'active' : ''}`}
            onClick={() => setActiveTab('profile')}
          >
            👤 Profil
          </button>
          <button
            className={`tab-btn ${activeTab === 'statistics' ? 'active' : ''}`}
            onClick={() => setActiveTab('statistics')}
          >
            📊 İstatistikler
          </button>
          <button
            className={`tab-btn ${activeTab === 'notifications' ? 'active' : ''}`}
            onClick={() => setActiveTab('notifications')}
          >
            🔔 Bildirimler
          </button>
        </div>

        <div className="profile-content">
          {/* Profil Sekmesi */}
          {activeTab === 'profile' && (
            <>
              {/* Kişisel Bilgiler */}
              <div className="profile-section card">
            <h2 className="section-title">Kişisel Bilgiler</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label htmlFor="FullName">Ad Soyad</label>
                <input
                  type="text"
                  id="FullName"
                  name="FullName"
                  value={formData.FullName}
                  onChange={handleChange}
                  placeholder="Ad Soyad"
                />
              </div>

              <div className="form-group">
                <label htmlFor="Email">E-posta</label>
                <input
                  type="email"
                  id="Email"
                  name="Email"
                  value={formData.Email}
                  onChange={handleChange}
                  placeholder="E-posta"
                />
              </div>

              <div className="form-group">
                <label htmlFor="Phone">Telefon</label>
                <input
                  type="tel"
                  id="Phone"
                  name="Phone"
                  value={formData.Phone}
                  onChange={handleChange}
                  placeholder="Telefon"
                />
              </div>

              {/* Şifre Değiştirme */}
              <div className="password-section">
                <h3 className="section-subtitle">Şifre Değiştir</h3>
                
                <div className="form-group">
                  <label htmlFor="CurrentPassword">Mevcut Şifre</label>
                  <input
                    type="password"
                    id="CurrentPassword"
                    name="CurrentPassword"
                    value={formData.CurrentPassword}
                    onChange={handleChange}
                    placeholder="Mevcut şifrenizi girin"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="NewPassword">Yeni Şifre</label>
                  <input
                    type="password"
                    id="NewPassword"
                    name="NewPassword"
                    value={formData.NewPassword}
                    onChange={handleChange}
                    placeholder="Yeni şifrenizi girin"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="ConfirmPassword">Yeni Şifre (Tekrar)</label>
                  <input
                    type="password"
                    id="ConfirmPassword"
                    name="ConfirmPassword"
                    value={formData.ConfirmPassword}
                    onChange={handleChange}
                    placeholder="Yeni şifrenizi tekrar girin"
                  />
                </div>
              </div>

              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving ? 'Kaydediliyor...' : 'Güncelle'}
              </button>
            </form>
          </div>

          {/* Hesap Bilgileri */}
          <div className="profile-section card">
            <h2 className="section-title">Hesap Bilgileri</h2>
            <div className="info-grid">
              <div className="info-card">
                <div className="info-card-icon">👤</div>
                <div className="info-card-content">
                  <div className="info-card-label">Rol</div>
                  <div className="info-card-value">{user.RoleName || user.roleName || 'Bilinmiyor'}</div>
                </div>
              </div>
              <div className="info-card">
                <div className="info-card-icon">📅</div>
                <div className="info-card-content">
                  <div className="info-card-label">Kayıt Tarihi</div>
                  <div className="info-card-value">
                    {(() => {
                      const createdAt = user.CreatedAt || user.createdAt
                      if (!createdAt || 
                          createdAt === '0001-01-01T00:00:00' || 
                          createdAt === '1970-01-01T00:00:00Z' ||
                          createdAt === 'Yükleniyor...') {
                        return 'Yükleniyor...'
                      }
                      try {
                        const date = new Date(createdAt)
                        if (!isNaN(date.getTime())) {
                          return date.toLocaleDateString('tr-TR', {
                            year: 'numeric',
                            month: 'long',
                            day: 'numeric'
                          })
                        }
                      } catch (e) {
                        console.error('Tarih formatlama hatası:', e)
                      }
                      return 'Bilinmiyor'
                    })()}
                  </div>
                </div>
              </div>
            </div>
          </div>
          </>
          )}

          {/* İstatistikler Sekmesi */}
          {activeTab === 'statistics' && (
            <div className="profile-section card">
              <h2 className="section-title">📊 Kullanıcı İstatistikleri</h2>
              {loadingStats ? (
                <p className="info-text">Yükleniyor...</p>
              ) : userStatistics ? (
                <div className="stats-grid">
                  <div className="stat-card">
                    <div className="stat-icon">💰</div>
                    <div className="stat-info">
                      <h3>{userStatistics.toplamHarcama?.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }) || '0,00 ₺'}</h3>
                      <p>Toplam Harcama</p>
                    </div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-icon">🚌</div>
                    <div className="stat-info">
                      <h3>{userStatistics.toplamSeyahatSayisi || 0}</h3>
                      <p>Toplam Seyahat</p>
                      <span className="stat-sub">{userStatistics.gelecekSeyahatSayisi || 0} Gelecek, {userStatistics.gecmisSeyahatSayisi || 0} Geçmiş</span>
                    </div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-icon">💵</div>
                    <div className="stat-info">
                      <h3>{userStatistics.ortalamaSeyahatFiyati?.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }) || '0,00 ₺'}</h3>
                      <p>Ortalama Seyahat Fiyatı</p>
                    </div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-icon">📍</div>
                    <div className="stat-info">
                      <h3>{userStatistics.enCokGidilenSehir || 'Yok'}</h3>
                      <p>En Çok Gidilen Şehir</p>
                    </div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-icon">🎫</div>
                    <div className="stat-info">
                      <h3>{userStatistics.toplamRezervasyonSayisi || 0}</h3>
                      <p>Toplam Rezervasyon</p>
                      <span className="stat-sub">{userStatistics.iptalEdilenRezervasyonSayisi || 0} İptal</span>
                    </div>
                  </div>
                  <div className="stat-card">
                    <div className="stat-icon">📅</div>
                    <div className="stat-info">
                      <h3 className="stat-date">{userStatistics.sonSeyahatTarihi ? (() => {
                        const date = new Date(userStatistics.sonSeyahatTarihi);
                        const day = date.getDate();
                        const month = date.toLocaleDateString('tr-TR', { month: 'short' });
                        const year = date.getFullYear();
                        return `${day} ${month} ${year}`;
                      })() : 'Yok'}</h3>
                      <p>Son Seyahat Tarihi</p>
                    </div>
                  </div>
                </div>
              ) : (
                <p className="info-text">İstatistikler yüklenemedi.</p>
              )}
            </div>
          )}

          {/* Bildirimler Sekmesi */}
          {activeTab === 'notifications' && (
            <div className="profile-section card">
              <h2 className="section-title">🔔 Bildirim Tercihleri</h2>
              {loadingNotifications ? (
                <p className="info-text">Yükleniyor...</p>
              ) : notificationPreferences ? (
                <form onSubmit={(e) => { e.preventDefault(); handleUpdateNotificationPreferences(); }}>
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={notificationPreferences.EmailNotifications}
                        onChange={(e) => setNotificationPreferences({ ...notificationPreferences, EmailNotifications: e.target.checked })}
                      />
                      <span>E-posta Bildirimleri</span>
                    </label>
                  </div>
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={notificationPreferences.ReservationNotifications}
                        onChange={(e) => setNotificationPreferences({ ...notificationPreferences, ReservationNotifications: e.target.checked })}
                      />
                      <span>Rezervasyon Bildirimleri</span>
                    </label>
                  </div>
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={notificationPreferences.PaymentNotifications}
                        onChange={(e) => setNotificationPreferences({ ...notificationPreferences, PaymentNotifications: e.target.checked })}
                      />
                      <span>Ödeme Bildirimleri</span>
                    </label>
                  </div>
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={notificationPreferences.CancellationNotifications}
                        onChange={(e) => setNotificationPreferences({ ...notificationPreferences, CancellationNotifications: e.target.checked })}
                      />
                      <span>İptal Bildirimleri</span>
                    </label>
                  </div>
                  <div className="form-group">
                    <label className="checkbox-label">
                      <input
                        type="checkbox"
                        checked={notificationPreferences.ReminderNotifications}
                        onChange={(e) => setNotificationPreferences({ ...notificationPreferences, ReminderNotifications: e.target.checked })}
                      />
                      <span>Hatırlatma Bildirimleri</span>
                    </label>
                  </div>
                  <button type="submit" className="btn btn-primary" disabled={saving}>
                    {saving ? 'Kaydediliyor...' : 'Tercihleri Kaydet'}
                  </button>
                </form>
              ) : (
                <p className="info-text">Bildirim tercihleri yüklenemedi.</p>
              )}
            </div>
          )}
        </div>
      </div>

      <Snackbar
        isOpen={snackbar.isOpen}
        message={snackbar.message}
        type={snackbar.type}
        onClose={() => setSnackbar({ ...snackbar, isOpen: false })}
        duration={4000}
      />
    </div>
  )
}

export default Profile
