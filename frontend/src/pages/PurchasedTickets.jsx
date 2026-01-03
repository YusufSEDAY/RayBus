import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { reservationAPI, ticketAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './PurchasedTickets.css'

const PurchasedTickets = () => {
  const navigate = useNavigate()
  const [tickets, setTickets] = useState([])
  const [filteredTickets, setFilteredTickets] = useState([])
  const [activeFilter, setActiveFilter] = useState('all') // 'all', 'cancelled', 'paid'
  const [loading, setLoading] = useState(true)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  useEffect(() => {
    fetchPurchasedTickets()
  }, [])

  const fetchPurchasedTickets = async () => {
    try {
      setLoading(true)
      
      const userStr = localStorage.getItem('raybus_user')
      if (!userStr) {
        console.warn('⚠️ Kullanıcı bilgisi bulunamadı')
        setLoading(false)
        navigate('/')
        return
      }

      let user
      try {
        user = JSON.parse(userStr)
      } catch (e) {
        console.error('❌ User parse hatası:', e)
        setLoading(false)
        navigate('/')
        return
      }

      let userId = user.UserID ?? user.userID ?? user.id ?? user.UserId ?? user.userId

      if (!userId || userId <= 0) {
        console.warn('⚠️ UserID localStorage\'da yok, token\'dan çıkarılıyor...')
        const token = localStorage.getItem('raybus_token')
        if (token) {
          try {
            const tokenParts = token.split('.')
            if (tokenParts.length === 3) {
              const payload = JSON.parse(atob(tokenParts[1]))
              userId = payload.nameid ?? payload.NameIdentifier ?? payload.UserID ?? payload.userID ?? payload.sub
              if (userId) {
                console.log('✅ Token\'dan UserID bulundu:', userId)
                const updatedUser = {
                  ...user,
                  UserID: parseInt(userId),
                  id: parseInt(userId)
                }
                localStorage.setItem('raybus_user', JSON.stringify(updatedUser))
              }
            }
          } catch (e) {
            console.error('❌ Token decode hatası:', e)
          }
        }
      }

      if (!userId || userId <= 0) {
        console.error('❌ Geçersiz UserID:', userId)
        setSnackbar({
          isOpen: true,
          message: 'Kullanıcı bilgisi eksik. Lütfen tekrar giriş yapın.',
          type: 'error'
        })
        localStorage.removeItem('raybus_user')
        localStorage.removeItem('raybus_token')
        setTimeout(() => navigate('/'), 2000)
        setLoading(false)
        return
      }

      console.log('🔍 Satın alınan biletler yükleniyor, UserID:', userId)
      const response = await reservationAPI.getByUserId(userId)
      console.log('🔍 Biletler API Response:', response.data)
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data

      if (success) {
        const allReservations = Array.isArray(data) ? data : []
        
        const purchasedTickets = allReservations.filter(res => {
          const paymentStatus = res.PaymentStatus ?? res.paymentStatus ?? ''
          return paymentStatus === 'Paid'
        })
        
        console.log('✅ Satın alınan biletler yüklendi:', purchasedTickets.length, 'adet')
        setTickets(purchasedTickets)
        applyFilter(purchasedTickets, activeFilter)
      } else {
        console.error('❌ Biletler yüklenemedi')
        setTickets([])
        setFilteredTickets([])
      }
    } catch (error) {
      console.error('❌ Biletler yüklenirken hata:', error)
      setTickets([])
      setFilteredTickets([])
      setSnackbar({
        isOpen: true,
        message: 'Biletler yüklenirken bir hata oluştu',
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  const applyFilter = (ticketsList, filter) => {
    let filtered = []
    
    switch (filter) {
      case 'cancelled':
        filtered = ticketsList.filter(ticket => {
          const status = (ticket.Status ?? ticket.status ?? '').toString().toLowerCase()
          return status === 'cancelled' || status === 'iptal edildi'
        })
        break
      case 'paid':
        filtered = ticketsList.filter(ticket => {
          const status = (ticket.Status ?? ticket.status ?? '').toString().toLowerCase()
          return status !== 'cancelled' && status !== 'iptal edildi'
        })
        break
      case 'all':
      default:
        filtered = ticketsList
        break
    }
    
    setFilteredTickets(filtered)
  }

  useEffect(() => {
    if (tickets.length > 0) {
      applyFilter(tickets, activeFilter)
    }
  }, [activeFilter, tickets])

  const formatTime = (time) => {
    if (!time) return ''
    if (typeof time === 'string') {
      return time.substring(0, 5)
    }
    if (time.totalSeconds !== undefined) {
      const hours = Math.floor(time.totalSeconds / 3600)
      const minutes = Math.floor((time.totalSeconds % 3600) / 60)
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    return ''
  }

  const formatDate = (date) => {
    if (!date) return ''
    return new Date(date).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  const handleDownloadPDF = async (reservationId) => {
    try {
      const response = await ticketAPI.generatePDF(reservationId)
      
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `Bilet_${reservationId}.pdf`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      
      setSnackbar({
        isOpen: true,
        message: 'Bilet PDF\'i başarıyla indirildi',
        type: 'success'
      })
    } catch (error) {
      console.error('PDF indirme hatası:', error)
      setSnackbar({
        isOpen: true,
        message: 'PDF indirilirken bir hata oluştu',
        type: 'error'
      })
    }
  }

  return (
    <div className="purchased-tickets-page">
      <div className="container">
        <div className="page-header">
          <h1 className="page-title">
            <span className="page-title-emoji">🎟️</span>
            Satın Aldıklarım
          </h1>
          <p className="page-subtitle">
            Ödemenizi tamamladığınız tüm biletleriniz burada görüntülenir.
          </p>
        </div>

        {/* Filtre Butonları */}
        {!loading && tickets.length > 0 && (
          <div className="filter-buttons">
            <button
              className={`filter-btn ${activeFilter === 'all' ? 'active' : ''}`}
              onClick={() => setActiveFilter('all')}
            >
              Tümü ({tickets.length})
            </button>
            <button
              className={`filter-btn ${activeFilter === 'cancelled' ? 'active' : ''}`}
              onClick={() => setActiveFilter('cancelled')}
            >
              İptal Edilenler ({tickets.filter(t => {
                const status = (t.Status ?? t.status ?? '').toString().toLowerCase()
                return status === 'cancelled' || status === 'iptal edildi'
              }).length})
            </button>
            <button
              className={`filter-btn ${activeFilter === 'paid' ? 'active' : ''}`}
              onClick={() => setActiveFilter('paid')}
            >
              Ödendiler ({tickets.filter(t => {
                const status = (t.Status ?? t.status ?? '').toString().toLowerCase()
                return status !== 'cancelled' && status !== 'iptal edildi'
              }).length})
            </button>
          </div>
        )}

        {loading ? (
          <div className="card">
            <p className="info-text">Yükleniyor...</p>
          </div>
        ) : filteredTickets.length === 0 ? (
          <div className="card empty-state">
            <div className="empty-icon">🎫</div>
            <h2>
              {activeFilter === 'all' 
                ? 'Henüz satın aldığınız bilet yok'
                : 'Bu filtre için sonuç bulunamadı'}
            </h2>
            <p className="info-text">
              {activeFilter === 'all' 
                ? 'Ödeme yaptığınız biletler burada görüntülenecektir.'
                : 'Seçtiğiniz filtreye uygun bilet bulunmuyor. Lütfen başka bir filtre seçin.'}
            </p>
            {activeFilter === 'all' && (
              <div className="empty-actions">
                <button className="btn btn-primary" onClick={() => navigate('/trains')}>
                  Tren Bileti Ara
                </button>
                <button className="btn btn-secondary" onClick={() => navigate('/buses')}>
                  Otobüs Bileti Ara
                </button>
              </div>
            )}
            {activeFilter !== 'all' && (
              <div className="empty-actions">
                <button className="btn btn-primary" onClick={() => setActiveFilter('all')}>
                  Tümünü Göster
                </button>
              </div>
            )}
          </div>
        ) : (
          <div className="tickets-list">
            {filteredTickets.map((ticket) => {
              const reservationId = ticket.ReservationID ?? ticket.reservationID
              const vehicleType = ticket.VehicleType ?? ticket.vehicleType ?? ''
              const fromCity = ticket.FromCity ?? ticket.fromCity ?? ''
              const toCity = ticket.ToCity ?? ticket.toCity ?? ''
              const departureDate = ticket.DepartureDate ?? ticket.departureDate
              const departureTime = ticket.DepartureTime ?? ticket.departureTime
              const status = ticket.Status ?? ticket.status ?? ''
              const seatNumber = ticket.SeatNumber ?? ticket.seatNumber ?? ''
              const price = ticket.Price ?? ticket.price ?? 0
              const reservationDate = ticket.ReservationDate ?? ticket.reservationDate
              const tripId = ticket.TripID ?? ticket.tripID

              const isCancelled = status === 'Cancelled' || status === 'İptal Edildi'
              const paymentStatus = ticket.PaymentStatus ?? ticket.paymentStatus ?? ''

              return (
                <div key={reservationId} className={`card ticket-card ${isCancelled ? 'cancelled' : ''}`}>
                  <div className="ticket-header">
                    <div className="ticket-badge">
                      {isCancelled ? (
                        <>
                          <span className="badge-icon">❌</span>
                          <span className="badge-text badge-cancelled">İptal Edildi</span>
                        </>
                      ) : (
                        <>
                          <span className="badge-icon">✅</span>
                          <span className="badge-text badge-paid">Ödendi</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="ticket-content">
                    <div className="ticket-route">
                      <h3>
                        {vehicleType === 'Train' ? '🚄' : '🚌'} 
                        {fromCity} → {toCity}
                      </h3>
                      <p className="ticket-date">
                        {formatDate(departureDate)} {formatTime(departureTime)}
                      </p>
                    </div>
                    <div className="ticket-details">
                      <div className="detail-row">
                        <span className="detail-label">Koltuk:</span>
                        <span className="detail-value">{seatNumber}</span>
                      </div>
                      <div className="detail-row">
                        <span className="detail-label">Fiyat:</span>
                        <span className="detail-value price">{parseFloat(price).toFixed(2)} ₺</span>
                      </div>
                      <div className="detail-row">
                        <span className="detail-label">Satın Alma Tarihi:</span>
                        <span className="detail-value">{formatDate(reservationDate)}</span>
                      </div>
                    </div>
                  </div>
                  <div className="ticket-actions">
                    {!isCancelled && (
                      <button 
                        className="btn btn-primary"
                        onClick={() => handleDownloadPDF(reservationId)}
                        style={{ marginRight: '8px' }}
                      >
                        📄 PDF İndir
                      </button>
                    )}
                    <button 
                      className="btn btn-outline"
                      onClick={() => {
                        if (!tripId || tripId === 0) {
                          setSnackbar({
                            isOpen: true,
                            message: 'Sefer bilgisi bulunamadı. Lütfen sayfayı yenileyin.',
                            type: 'error'
                          })
                          return
                        }
                        
                        const parsedTripId = parseInt(tripId)
                        if (isNaN(parsedTripId) || parsedTripId <= 0) {
                          setSnackbar({
                            isOpen: true,
                            message: 'Geçersiz sefer bilgisi. Lütfen sayfayı yenileyin.',
                            type: 'error'
                          })
                          return
                        }
                        
                        navigate(`/trip/${parsedTripId}`)
                      }}
                    >
                      Detayları Gör
                    </button>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>
      
      {/* Snackbar Notification */}
      <Snackbar
        isOpen={snackbar.isOpen}
        message={snackbar.message}
        type={snackbar.type}
        onClose={() => setSnackbar({ ...snackbar, isOpen: false })}
      />
    </div>
  )
}

export default PurchasedTickets

