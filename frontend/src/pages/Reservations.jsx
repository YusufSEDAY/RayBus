import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { reservationAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import PaymentModal from '../components/PaymentModal'
import './Reservations.css'

const Reservations = () => {
  const navigate = useNavigate()
  const [reservations, setReservations] = useState([])
  const [filteredReservations, setFilteredReservations] = useState([])
  const [activeFilter, setActiveFilter] = useState('all') // 'all', 'pending', 'cancelled', 'paid'
  const [loading, setLoading] = useState(true)
  const [cancellingIds, setCancellingIds] = useState(new Set()) // İptal edilen rezervasyon ID'leri
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })
  
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [selectedReservationId, setSelectedReservationId] = useState(null)
  const [cancellationReasons, setCancellationReasons] = useState([])
  const [selectedReasonId, setSelectedReasonId] = useState(null)
  const [customReason, setCustomReason] = useState('')
  const [loadingReasons, setLoadingReasons] = useState(false)
  const [payingIds, setPayingIds] = useState(new Set())
  
  const [showPaymentModal, setShowPaymentModal] = useState(false)
  const [selectedPaymentReservation, setSelectedPaymentReservation] = useState(null)

  useEffect(() => {
    fetchReservations()
    fetchCancellationReasons()
  }, [])

  const fetchCancellationReasons = async () => {
    try {
      setLoadingReasons(true)
      const response = await reservationAPI.getCancellationReasons()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        setCancellationReasons(data)
      }
    } catch (error) {
      console.error('❌ İptal nedenleri yüklenirken hata:', error)
    } finally {
      setLoadingReasons(false)
    }
  }

  const fetchReservations = async () => {
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

      console.log('🔍 Rezervasyonlar yükleniyor, UserID:', userId)
      const response = await reservationAPI.getByUserId(userId)
      console.log('🔍 Rezervasyonlar API Response:', response.data)
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      const message = response.data?.Message ?? response.data?.message

      if (success) {
        const reservationsList = Array.isArray(data) ? data : []
        console.log('✅ Rezervasyonlar yüklendi:', reservationsList.length, 'adet')
        
        reservationsList.forEach((res, index) => {
          const tripId = res.TripID ?? res.tripID
          if (!tripId || tripId === 0) {
            console.warn(`⚠️ Rezervasyon ${index + 1}'de TripID eksik veya 0:`, res)
          } else {
            console.log(`✅ Rezervasyon ${index + 1} - TripID:`, tripId)
          }
        })
        
        setReservations(reservationsList)
        applyFilter(reservationsList, activeFilter)
      } else {
        console.error('❌ Rezervasyonlar yüklenemedi:', message)
        setReservations([])
        setFilteredReservations([])
      }
    } catch (error) {
      console.error('❌ Rezervasyonlar yüklenirken hata:', error)
      setReservations([])
      setFilteredReservations([])
    } finally {
      setLoading(false)
    }
  }

  const applyFilter = (reservationsList, filter) => {
    let filtered = []
    
    switch (filter) {
      case 'pending':
        filtered = reservationsList.filter(res => {
          const status = (res.Status ?? res.status ?? '').toString().toLowerCase()
          const paymentStatus = (res.PaymentStatus ?? res.paymentStatus ?? '').toString().toLowerCase()
          return status === 'reserved' && paymentStatus === 'pending'
        })
        break
      case 'cancelled':
        filtered = reservationsList.filter(res => {
          const status = (res.Status ?? res.status ?? '').toString().toLowerCase()
          return status === 'cancelled' || status === 'iptal edildi'
        })
        break
      case 'paid':
        filtered = reservationsList.filter(res => {
          const status = (res.Status ?? res.status ?? '').toString().toLowerCase()
          const paymentStatus = (res.PaymentStatus ?? res.paymentStatus ?? '').toString().toLowerCase()
          return paymentStatus === 'paid' && status !== 'cancelled' && status !== 'iptal edildi'
        })
        break
      case 'all':
      default:
        filtered = reservationsList
        break
    }
    
    setFilteredReservations(filtered)
  }

  useEffect(() => {
    if (reservations.length > 0) {
      applyFilter(reservations, activeFilter)
    }
  }, [activeFilter, reservations])

  const handleCancelClick = (id) => {
    if (!id || id <= 0) {
      console.error('❌ Geçersiz rezervasyon ID:', id)
      setSnackbar({
        isOpen: true,
        message: 'Geçersiz rezervasyon bilgisi',
        type: 'error'
      })
      return
    }

    const reservation = reservations.find(r => 
      (r.ReservationID ?? r.reservationID) === id
    )

    if (!reservation) {
      console.error('❌ Rezervasyon bulunamadı:', id)
      setSnackbar({
        isOpen: true,
        message: 'Rezervasyon bulunamadı',
        type: 'error'
      })
      return
    }

    const status = reservation.Status ?? reservation.status ?? ''
    if (status === 'Cancelled' || status === 'İptal Edildi') {
      setSnackbar({
        isOpen: true,
        message: 'Bu rezervasyon zaten iptal edilmiş',
        type: 'warning'
      })
      return
    }

    setSelectedReservationId(id)
    setSelectedReasonId(null)
    setCustomReason('')
    setShowCancelModal(true)
  }

  const handleCancelConfirm = async () => {
    if (!selectedReservationId) return

    let finalCancelReasonID = selectedReasonId

    if (selectedReasonId === 6 && customReason.trim()) {
      try {
        const response = await reservationAPI.createCancellationReason(customReason.trim())
        const success = response.data?.Success ?? response.data?.success
        const data = response.data?.Data ?? response.data?.data
        
        if (success && data) {
          finalCancelReasonID = data.ReasonID ?? data.reasonID
          setCancellationReasons(prev => [...prev, data])
        } else {
          setSnackbar({
            isOpen: true,
            message: 'İptal nedeni kaydedilemedi',
            type: 'error'
          })
          return
        }
      } catch (error) {
        console.error('❌ Özel iptal nedeni kaydedilirken hata:', error)
        setSnackbar({
          isOpen: true,
          message: 'İptal nedeni kaydedilemedi',
          type: 'error'
        })
        return
      }
    } else if (!selectedReasonId || (selectedReasonId === 6 && !customReason.trim())) {
      setSnackbar({
        isOpen: true,
        message: selectedReasonId === 6 
          ? 'Lütfen iptal nedeninizi yazın' 
          : 'Lütfen bir iptal nedeni seçin',
        type: 'warning'
      })
      return
    }

    // Modal'ı kapat
    setShowCancelModal(false)

    // Loading state ekle
    setCancellingIds(prev => new Set(prev).add(selectedReservationId))

    try {
      console.log('🔍 Rezervasyon iptal ediliyor, ID:', selectedReservationId, 'CancelReasonID:', finalCancelReasonID)
      
      // Optimistic UI update - hemen listeden kaldır
      setReservations(prev => prev.filter(r => 
        (r.ReservationID ?? r.reservationID) !== selectedReservationId
      ))

      const cancelDto = finalCancelReasonID ? { CancelReasonID: finalCancelReasonID } : null
      const response = await reservationAPI.cancel(selectedReservationId, cancelDto)
      console.log('🔍 İptal API Response:', response.data)
      
      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const message = response.data?.Message ?? response.data?.message
      const errors = response.data?.Errors ?? response.data?.errors ?? []
      
      if (success) {
        setSnackbar({
          isOpen: true,
          message: message || 'Rezervasyon başarıyla iptal edildi',
          type: 'success'
        })
        
        // Listeyi yenile (güncel durumu almak için)
        setTimeout(() => {
          fetchReservations()
        }, 500)
      } else {
        // Hata durumunda listeyi geri yükle
        fetchReservations()
        
        const errorMsg = errors.length > 0 
          ? errors.join(', ') 
          : message || 'Rezervasyon iptal edilemedi'
        
        setSnackbar({
          isOpen: true,
          message: errorMsg,
          type: 'error'
        })
      }
    } catch (error) {
      console.error('❌ İptal hatası:', error)
      
      // Hata durumunda listeyi geri yükle
      fetchReservations()
      
      const errorMessage = error.response?.data?.Message ?? 
                          error.response?.data?.message ?? 
                          error.response?.data?.Errors?.join(', ') ??
                          error.message ??
                          'Rezervasyon iptal edilirken bir hata oluştu'
      
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      // Loading state'i kaldır
      setCancellingIds(prev => {
        const newSet = new Set(prev)
        newSet.delete(selectedReservationId)
        return newSet
      })
      
      // State'i temizle
      setSelectedReservationId(null)
      setSelectedReasonId(null)
      setCustomReason('')
    }
  }

  const handleCompletePaymentClick = (reservationId, price) => {
    if (!reservationId || reservationId <= 0) {
      setSnackbar({
        isOpen: true,
        message: 'Geçersiz rezervasyon bilgisi',
        type: 'error'
      })
      return
    }

    // Price değerini parse et ve kontrol et
    const parsedPrice = parseFloat(price) || 0
    console.log('🔍 Ödeme modal açılıyor:', { 
      reservationId, 
      price, 
      priceType: typeof price,
      parsedPrice,
      reservation: reservations.find(r => (r.ReservationID ?? r.reservationID) === reservationId)
    })
    
    if (parsedPrice <= 0 || isNaN(parsedPrice)) {
      console.error('❌ Geçersiz fiyat:', { 
        price, 
        parsedPrice, 
        reservationId,
        reservation: reservations.find(r => (r.ReservationID ?? r.reservationID) === reservationId)
      })
      setSnackbar({
        isOpen: true,
        message: `Geçersiz fiyat bilgisi: ${price}. Lütfen sayfayı yenileyin.`,
        type: 'error'
      })
      return
    }

    // Modal'ı aç
    setSelectedPaymentReservation({ reservationId, price: parsedPrice })
    setShowPaymentModal(true)
  }

  const handleCompletePayment = async (paymentInfo) => {
    if (!selectedPaymentReservation) return

    const { reservationId, price } = selectedPaymentReservation

    // Modal'ı kapat
    setShowPaymentModal(false)

    // Loading state ekle
    setPayingIds(prev => new Set(prev).add(reservationId))

    try {
      console.log('🔍 Ödeme tamamlanıyor, ReservationID:', reservationId, 'Price:', price)

      // Ödeme bilgilerini hazırla
      const paymentData = {
        ReservationID: reservationId,
        Price: parseFloat(price),
        PaymentMethod: 'Kredi Kartı',
        CardInfo: {
          Last4Digits: paymentInfo.cardNumber,
          CardHolder: paymentInfo.cardHolder,
          ExpiryMonth: paymentInfo.expiryMonth,
          ExpiryYear: paymentInfo.expiryYear,
          MaskedCardNumber: paymentInfo.maskedCardNumber
        }
      }

      const response = await reservationAPI.completePayment(paymentData)

      console.log('🔍 Ödeme API Response:', response.data)

      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const message = response.data?.Message ?? response.data?.message
      const errors = response.data?.Errors ?? response.data?.errors ?? []

      if (success) {
        setSnackbar({
          isOpen: true,
          message: message || 'Ödeme başarıyla tamamlandı!',
          type: 'success'
        })

        // Listeyi yenile
        setTimeout(() => {
          fetchReservations()
        }, 500)
      } else {
        const errorMsg = errors.length > 0 
          ? errors.join(', ') 
          : message || 'Ödeme tamamlanamadı'

        setSnackbar({
          isOpen: true,
          message: errorMsg,
          type: 'error'
        })
      }
    } catch (error) {
      console.error('❌ Ödeme hatası:', error)

      const errorMessage = error.response?.data?.Message ?? 
                          error.response?.data?.message ?? 
                          error.response?.data?.Errors?.join(', ') ??
                          error.message ??
                          'Ödeme tamamlanırken bir hata oluştu'

      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      // Loading state'i kaldır
      setPayingIds(prev => {
        const newSet = new Set(prev)
        newSet.delete(reservationId)
        return newSet
      })
      
      // State'i temizle
      setSelectedPaymentReservation(null)
    }
  }

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

  console.log('🔄 Reservations component render ediliyor:', { loading, reservationsCount: reservations.length })

  return (
    <div className="reservations-page">
      <div className="container">
        <div className="page-header">
          <h1 className="page-title">
            <span className="page-title-emoji">🎫</span>
            Rezervasyonlarım
          </h1>
          <p className="page-subtitle">
            Tüm rezervasyonlarınızı buradan görüntüleyebilir ve yönetebilirsiniz
          </p>
        </div>

        {/* Filtre Butonları */}
        {!loading && reservations.length > 0 && (
          <div className="filter-buttons">
            <button
              className={`filter-btn ${activeFilter === 'all' ? 'active' : ''}`}
              onClick={() => setActiveFilter('all')}
            >
              Tümü ({reservations.length})
            </button>
            <button
              className={`filter-btn ${activeFilter === 'pending' ? 'active' : ''}`}
              onClick={() => setActiveFilter('pending')}
            >
              Rezerveler ({reservations.filter(r => {
                const status = (r.Status ?? r.status ?? '').toString().toLowerCase()
                const paymentStatus = (r.PaymentStatus ?? r.paymentStatus ?? '').toString().toLowerCase()
                return status === 'reserved' && paymentStatus === 'pending'
              }).length})
            </button>
            <button
              className={`filter-btn ${activeFilter === 'cancelled' ? 'active' : ''}`}
              onClick={() => setActiveFilter('cancelled')}
            >
              İptal Edilenler ({reservations.filter(r => {
                const status = (r.Status ?? r.status ?? '').toString().toLowerCase()
                return status === 'cancelled' || status === 'iptal edildi'
              }).length})
            </button>
            <button
              className={`filter-btn ${activeFilter === 'paid' ? 'active' : ''}`}
              onClick={() => setActiveFilter('paid')}
            >
              Ödeme Yaptıklarım ({reservations.filter(r => {
                const status = (r.Status ?? r.status ?? '').toString().toLowerCase()
                const paymentStatus = (r.PaymentStatus ?? r.paymentStatus ?? '').toString().toLowerCase()
                // İptal edilmemiş ve ödeme yapılmış olanlar
                return paymentStatus === 'paid' && status !== 'cancelled' && status !== 'iptal edildi'
              }).length})
            </button>
          </div>
        )}

        {loading ? (
          <div className="card">
            <p className="info-text">Yükleniyor...</p>
          </div>
        ) : filteredReservations.length === 0 ? (
          <div className="card empty-state">
            <div className="empty-icon">📋</div>
            <h2>
              {activeFilter === 'all' 
                ? 'Henüz rezervasyonunuz yok'
                : 'Bu filtre için sonuç bulunamadı'}
            </h2>
            <p className="info-text">
              {activeFilter === 'all' 
                ? 'Tren veya otobüs bileti arayarak ilk rezervasyonunuzu oluşturabilirsiniz.'
                : 'Seçtiğiniz filtreye uygun rezervasyon bulunmuyor. Lütfen başka bir filtre seçin.'}
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
          <div className="reservations-list">
            {filteredReservations.map((reservation) => {
              // Backend hem PascalCase hem camelCase döndürebilir
              const reservationId = reservation.ReservationID ?? reservation.reservationID
              const vehicleType = reservation.VehicleType ?? reservation.vehicleType ?? ''
              const fromCity = reservation.FromCity ?? reservation.fromCity ?? ''
              const toCity = reservation.ToCity ?? reservation.toCity ?? ''
              const departureDate = reservation.DepartureDate ?? reservation.departureDate
              const departureTime = reservation.DepartureTime ?? reservation.departureTime
              const status = reservation.Status ?? reservation.status ?? ''
              const paymentStatus = reservation.PaymentStatus ?? reservation.paymentStatus ?? ''
              const seatNumber = reservation.SeatNumber ?? reservation.seatNumber ?? ''
              const price = parseFloat(reservation.Price ?? reservation.price ?? 0) || 0
              const reservationDate = reservation.ReservationDate ?? reservation.reservationDate
              const tripId = reservation.TripID ?? reservation.tripID
              
              // TripID validasyonu ve log
              console.log('🔍 Rezervasyon render ediliyor:', {
                reservationId,
                tripId,
                tripIdType: typeof tripId,
                reservation: reservation
              })
              
              if (!tripId || tripId <= 0 || isNaN(parseInt(tripId))) {
                console.error('❌ Rezervasyonda geçersiz TripID:', tripId, 'Reservation:', reservation)
              }

              return (
                <div key={reservationId} className="card reservation-card">
                  <div className="reservation-header">
                    <div>
                      <h3>
                        {vehicleType === 'Train' ? '🚄' : '🚌'} 
                        {fromCity} → {toCity}
                      </h3>
                      <p className="reservation-date">
                        {formatDate(departureDate)} {formatTime(departureTime)}
                      </p>
                    </div>
                    <div className="reservation-status-badge">
                      <span className={`status status-${status.toLowerCase()}`}>
                        {status === 'Reserved' ? 'Rezerve' : 
                         status === 'Cancelled' ? 'İptal Edildi' : 
                         status === 'Completed' ? 'Tamamlandı' : status}
                      </span>
                      {/* İptal edilenler için ödeme durumu gösterilmez */}
                      {status !== 'Cancelled' && status !== 'İptal Edildi' && (
                        <span className={`payment-status payment-${paymentStatus.toLowerCase()}`}>
                          {paymentStatus === 'Pending' ? 'Ödeme Bekliyor' :
                           paymentStatus === 'Paid' ? 'Ödendi' :
                           paymentStatus === 'Refunded' ? 'İade Edildi' : paymentStatus}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className="reservation-details">
                    <div className="detail-row">
                      <span className="detail-label">Koltuk:</span>
                      <span className="detail-value">{seatNumber}</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Fiyat:</span>
                      <span className="detail-value price">{parseFloat(price).toFixed(2)} ₺</span>
                    </div>
                    <div className="detail-row">
                      <span className="detail-label">Rezervasyon Tarihi:</span>
                      <span className="detail-value">{formatDate(reservationDate)}</span>
                    </div>
                  </div>
                  {status === 'Reserved' && (
                    <div className="reservation-actions">
                      <button 
                        className="btn btn-outline"
                        onClick={() => {
                          console.log('🔍 Detayları Gör butonuna tıklandı:', {
                            tripId,
                            tripIdType: typeof tripId,
                            reservationId,
                            reservation
                          })
                          
                          // TripID validasyonu
                          if (!tripId || tripId === 0) {
                            console.error('❌ TripID eksik veya 0:', tripId, 'Reservation:', reservation)
                            setSnackbar({
                              isOpen: true,
                              message: 'Sefer bilgisi bulunamadı. Lütfen sayfayı yenileyin.',
                              type: 'error'
                            })
                            return
                          }
                          
                          const parsedTripId = parseInt(tripId)
                          if (isNaN(parsedTripId) || parsedTripId <= 0) {
                            console.error('❌ Geçersiz TripID (parse edilemedi):', tripId, 'Reservation:', reservation)
                            setSnackbar({
                              isOpen: true,
                              message: 'Geçersiz sefer bilgisi. Lütfen sayfayı yenileyin.',
                              type: 'error'
                            })
                            return
                          }
                          
                          console.log('✅ TripID geçerli, yönlendiriliyor:', parsedTripId)
                          navigate(`/trip/${parsedTripId}`)
                        }}
                      >
                        Detayları Gör
                      </button>
                      {paymentStatus === 'Pending' && (
                        <button 
                          className="btn btn-primary"
                          onClick={() => handleCompletePaymentClick(reservationId, price)}
                          disabled={payingIds.has(reservationId)}
                          title={payingIds.has(reservationId) ? 'Ödeme yapılıyor...' : 'Ödemeyi tamamla'}
                        >
                          {payingIds.has(reservationId) ? (
                            <>
                              <span className="spinner" style={{ 
                                display: 'inline-block', 
                                width: '14px', 
                                height: '14px', 
                                border: '2px solid currentColor',
                                borderTopColor: 'transparent',
                                borderRadius: '50%',
                                animation: 'spin 0.6s linear infinite',
                                marginRight: '6px',
                                verticalAlign: 'middle'
                              }}></span>
                              Ödeme Yapılıyor...
                            </>
                          ) : (
                            '💳 Ödeme Yap'
                          )}
                        </button>
                      )}
                      <button 
                        className="btn btn-outline btn-danger"
                        onClick={() => handleCancelClick(reservationId)}
                        disabled={cancellingIds.has(reservationId)}
                        title={cancellingIds.has(reservationId) ? 'İptal ediliyor...' : 'Rezervasyonu iptal et'}
                      >
                        {cancellingIds.has(reservationId) ? (
                          <>
                            <span className="spinner" style={{ 
                              display: 'inline-block', 
                              width: '14px', 
                              height: '14px', 
                              border: '2px solid currentColor',
                              borderTopColor: 'transparent',
                              borderRadius: '50%',
                              animation: 'spin 0.6s linear infinite',
                              marginRight: '6px',
                              verticalAlign: 'middle'
                            }}></span>
                            İptal Ediliyor...
                          </>
                        ) : (
                          'İptal Et'
                        )}
                      </button>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </div>
      
      {/* İptal Modal */}
      {showCancelModal && (
        <div className="modal-overlay" onClick={() => setShowCancelModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Rezervasyon İptal</h2>
              <button 
                className="modal-close" 
                onClick={() => {
                  setShowCancelModal(false)
                  setSelectedReasonId(null)
                  setCustomReason('')
                }}
              >
                ×
              </button>
            </div>
            <div className="modal-body">
              <p style={{ marginBottom: '20px', color: 'var(--text-secondary)' }}>
                Lütfen iptal nedeninizi seçin:
              </p>
              
              <div className="cancel-reasons-list">
                {loadingReasons ? (
                  <p>Yükleniyor...</p>
                ) : (
                  <>
                    {cancellationReasons.map((reason) => (
                      <label key={reason.ReasonID ?? reason.reasonID} className="reason-option">
                        <input
                          type="radio"
                          name="cancelReason"
                          value={reason.ReasonID ?? reason.reasonID}
                          checked={selectedReasonId === (reason.ReasonID ?? reason.reasonID)}
                          onChange={(e) => {
                            const reasonId = parseInt(e.target.value)
                            setSelectedReasonId(reasonId)
                            // Eğer "Diğer" değilse custom reason'ı temizle
                            if (reasonId !== 6) {
                              setCustomReason('')
                            }
                          }}
                        />
                        <span>{reason.ReasonText ?? reason.reasonText}</span>
                      </label>
                    ))}
                    
                    {/* "Diğer" seçeneği - ID 6 */}
                    <label className="reason-option">
                      <input
                        type="radio"
                        name="cancelReason"
                        value="6"
                        checked={selectedReasonId === 6}
                        onChange={() => {
                          setSelectedReasonId(6)
                        }}
                      />
                      <span>Diğer</span>
                    </label>
                  </>
                )}
              </div>

              {/* "Diğer" seçildiğinde textbox göster */}
              {selectedReasonId === 6 && (
                <div className="custom-reason-input" style={{ marginTop: '16px' }}>
                  <label style={{ display: 'block', marginBottom: '8px', fontWeight: 500 }}>
                    İptal nedeninizi yazın:
                  </label>
                  <textarea
                    value={customReason}
                    onChange={(e) => setCustomReason(e.target.value)}
                    placeholder="İptal nedeninizi detaylı olarak açıklayın..."
                    rows={4}
                    style={{
                      width: '100%',
                      padding: '12px',
                      borderRadius: '8px',
                      border: '1px solid var(--border-color)',
                      backgroundColor: 'var(--card-bg)',
                      color: 'var(--text-primary)',
                      fontSize: '14px',
                      fontFamily: 'inherit',
                      resize: 'vertical'
                    }}
                  />
                </div>
              )}

              <div className="modal-actions" style={{ marginTop: '24px', display: 'flex', gap: '12px', justifyContent: 'flex-end' }}>
                <button
                  className="btn btn-outline"
                  onClick={() => {
                    setShowCancelModal(false)
                    setSelectedReasonId(null)
                    setCustomReason('')
                  }}
                >
                  İptal
                </button>
                <button
                  className="btn btn-danger"
                  onClick={handleCancelConfirm}
                  disabled={!selectedReasonId || (selectedReasonId === 6 && !customReason.trim()) || cancellingIds.has(selectedReservationId)}
                >
                  {cancellingIds.has(selectedReservationId) ? 'İptal Ediliyor...' : 'Rezervasyonu İptal Et'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Ödeme Modal */}
      {showPaymentModal && selectedPaymentReservation && (
        <PaymentModal
          isOpen={showPaymentModal}
          onClose={() => {
            setShowPaymentModal(false)
            setSelectedPaymentReservation(null)
          }}
          onConfirm={handleCompletePayment}
          amount={selectedPaymentReservation.price}
          loading={payingIds.has(selectedPaymentReservation.reservationId)}
        />
      )}

      {/* Snackbar Notification */}
      <Snackbar
        isOpen={snackbar.isOpen}
        message={snackbar.message}
        type={snackbar.type}
        onClose={() => setSnackbar({ ...snackbar, isOpen: false })}
      />
      
      {/* Spinner Animation */}
      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  )
}

export default Reservations
