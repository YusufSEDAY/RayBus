import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { tripAPI, reservationAPI } from '../services/api'
import PaymentModal from '../components/PaymentModal'
import Snackbar from '../components/Snackbar'
import './TripDetail.css'

const TripDetail = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const [trip, setTrip] = useState(null)
  const [loading, setLoading] = useState(true)
  const [selectedSeat, setSelectedSeat] = useState(null)
  const [error, setError] = useState(null)
  const [showPaymentModal, setShowPaymentModal] = useState(false)
  const [isProcessing, setIsProcessing] = useState(false)
  const [pendingReservationType, setPendingReservationType] = useState(null) // 0 = Rezervasyon, 1 = Satın Alma
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  // Sayfa yüklendiğinde localStorage'daki kullanıcı bilgisini kontrol et
  useEffect(() => {
    const userStr = localStorage.getItem('raybus_user')
    if (userStr) {
      try {
        const user = JSON.parse(userStr)
        console.log('🔍 Sayfa yüklendiğinde localStorage user:', user)
        console.log('🔍 UserID kontrolü:', {
          UserID: user.UserID,
          userID: user.userID,
          id: user.id,
          tümKeys: Object.keys(user)
        })
      } catch (e) {
        console.error('❌ localStorage user parse hatası:', e)
      }
    } else {
      console.warn('⚠️ localStorage\'da raybus_user bulunamadı')
    }
  }, [])

  useEffect(() => {
    fetchTripDetail()
  }, [id])

  const fetchTripDetail = async () => {
    try {
      setLoading(true)
      
      // ID validasyonu
      if (!id || id === 'undefined' || id === 'null') {
        console.error('❌ Geçersiz sefer ID:', id)
        setError('Geçersiz sefer ID\'si')
        setLoading(false)
        return
      }
      
      const tripId = parseInt(id)
      if (isNaN(tripId) || tripId <= 0) {
        console.error('❌ Geçersiz sefer ID (parse edilemedi):', id)
        setError('Geçersiz sefer ID\'si')
        setLoading(false)
        return
      }
      
      console.log('🔍 TripDetail - Sefer detayı yükleniyor, ID:', tripId)
      const response = await tripAPI.getDetail(tripId)
      console.log('🔍 TripDetail API Response:', response.data)
      
      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      const message = response.data?.Message ?? response.data?.message
      
      if (success && data) {
        console.log('✅ Sefer detayı yüklendi:', data)
        setTrip(data)
      } else {
        console.error('❌ Sefer detayı yüklenemedi:', message)
        setError(message || 'Sefer bulunamadı')
      }
    } catch (error) {
      console.error('Sefer detayı yüklenirken hata:', error)
      setError('Sefer detayı yüklenirken bir hata oluştu')
    } finally {
      setLoading(false)
    }
  }

  const handleSeatSelect = (seat) => {
    const isActive = seat.IsActive ?? seat.isActive ?? true
    const paymentStatus = (seat.PaymentStatus ?? seat.paymentStatus ?? '').toString().trim()
    const paymentStatusLower = paymentStatus.toLowerCase()
    
    // Ödeme yapılmış (Dolu) veya rezerve edilmiş (Rezerve) koltuklar seçilemez
    if (paymentStatusLower === 'paid' || paymentStatusLower === 'pending' || !isActive) {
      console.log('⚠️ Koltuk seçilemez:', { seat, isActive, paymentStatus, paymentStatusLower })
      return
    }
    
    console.log('✅ Koltuk seçildi:', seat)
    setSelectedSeat(seat)
  }

  const handleReservationClick = (islemTipi) => {
    if (!selectedSeat) return
    
    // Eğer satın alma ise (islemTipi === 1), önce kart bilgilerini iste
    if (islemTipi === 1) {
      setPendingReservationType(1)
      setShowPaymentModal(true)
    } else {
      // Rezervasyon için direkt oluştur
      createReservation(0, null)
    }
  }

  const handlePaymentConfirm = (paymentInfo) => {
    // Modal'ı kapat ve rezervasyonu oluştur
    setShowPaymentModal(false)
    createReservation(pendingReservationType, paymentInfo)
  }

  const createReservation = async (islemTipi, paymentInfo = null) => {
    if (!selectedSeat) return

    // localStorage'dan kullanıcı bilgisini al
    const userStr = localStorage.getItem('raybus_user')
    console.log('🔍 localStorage raybus_user (raw):', userStr)
    
    if (!userStr) {
      setSnackbar({
        isOpen: true,
        message: 'Rezervasyon yapmak için giriş yapmalısınız',
        type: 'warning'
      })
      setTimeout(() => navigate('/'), 2000)
      return
    }

    let user
    try {
      user = JSON.parse(userStr)
      console.log('🔍 Parsed user object:', user)
      console.log('🔍 User keys:', Object.keys(user))
      console.log('🔍 User.UserID:', user.UserID)
      console.log('🔍 User.userID:', user.userID)
      console.log('🔍 User.id:', user.id)
    } catch (e) {
      console.error('❌ User parse hatası:', e)
      setSnackbar({
        isOpen: true,
        message: 'Kullanıcı bilgisi okunamadı. Lütfen tekrar giriş yapın.',
        type: 'error'
      })
      setTimeout(() => navigate('/'), 2000)
      return
    }

    try {
      // Kullanıcı ID'sini farklı formatlardan al
      const userId = user.UserID ?? user.userID ?? user.id ?? user.UserId ?? user.userId
      const tripId = trip?.TripID ?? trip?.tripID ?? parseInt(id)
      const seatId = selectedSeat.SeatID ?? selectedSeat.seatID
      const price = trip.Price ?? trip.price ?? 0

      console.log('🔍 ID değerleri:', { 
        userId, 
        userIdType: typeof userId,
        userObject: user,
        tripId, 
        seatId, 
        price 
      })

      // ID validasyonu - userId undefined ise token'dan almayı dene
      let finalUserId = userId
      if (userId === undefined || userId === null) {
        console.error('❌ UserID undefined! User object:', JSON.stringify(user, null, 2))
        console.error('❌ Tüm localStorage içeriği:')
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i)
          console.error(`  ${key}:`, localStorage.getItem(key))
        }
        
        // Token varsa, token'dan kullanıcı bilgisini çıkarmayı dene
        const token = localStorage.getItem('raybus_token')
        if (token) {
          try {
            // JWT token'ı decode et
            const tokenParts = token.split('.')
            if (tokenParts.length === 3) {
              const payload = JSON.parse(atob(tokenParts[1]))
              console.log('🔍 Token payload:', payload)
              
              // Token'dan UserID'yi al
              // JWT'de UserID ClaimTypes.NameIdentifier olarak saklanıyor, bu da "nameid" claim'i olarak görünür
              const tokenUserId = payload.nameid ?? payload.NameIdentifier ?? payload.UserID ?? payload.userID ?? payload.sub
              if (tokenUserId) {
                console.log('✅ Token\'dan UserID bulundu:', tokenUserId)
                finalUserId = parseInt(tokenUserId)
                
                // localStorage'ı güncelle
                const updatedUser = {
                  ...user,
                  UserID: finalUserId,
                  id: finalUserId
                }
                localStorage.setItem('raybus_user', JSON.stringify(updatedUser))
                console.log('✅ localStorage güncellendi')
              }
            }
          } catch (e) {
            console.error('❌ Token decode hatası:', e)
          }
        }
        
        // Hala userId yoksa hata ver
        if (!finalUserId || finalUserId <= 0) {
          setSnackbar({
            isOpen: true,
            message: 'Kullanıcı bilgisi eksik. Lütfen çıkış yapıp tekrar giriş yapın.',
            type: 'error'
          })
          localStorage.removeItem('raybus_user')
          localStorage.removeItem('raybus_token')
          setTimeout(() => navigate('/'), 2000)
          return
        }
      }

      if (finalUserId <= 0 || isNaN(finalUserId)) {
        console.error('❌ Geçersiz userId:', finalUserId, 'User object:', JSON.stringify(user, null, 2))
        setSnackbar({
          isOpen: true,
          message: 'Geçersiz kullanıcı bilgisi. Lütfen tekrar giriş yapın.',
          type: 'error'
        })
        // localStorage'ı temizle ve login sayfasına yönlendir
        localStorage.removeItem('raybus_user')
        localStorage.removeItem('raybus_token')
        setTimeout(() => navigate('/'), 2000)
        return
      }

      if (!tripId || tripId <= 0 || isNaN(tripId)) {
        setSnackbar({
          isOpen: true,
          message: 'Geçersiz sefer bilgisi. Lütfen sayfayı yenileyin.',
          type: 'error'
        })
        console.error('❌ Geçersiz tripId:', tripId, 'trip:', trip, 'id:', id)
        return
      }

      if (!seatId || seatId <= 0) {
        setSnackbar({
          isOpen: true,
          message: 'Geçersiz koltuk bilgisi. Lütfen bir koltuk seçin.',
          type: 'error'
        })
        console.error('❌ Geçersiz seatId:', seatId, 'selectedSeat:', selectedSeat)
        return
      }

      if (!price || price <= 0) {
        setSnackbar({
          isOpen: true,
          message: 'Geçersiz fiyat bilgisi. Lütfen sayfayı yenileyin.',
          type: 'error'
        })
        console.error('❌ Geçersiz price:', price)
        return
      }

      const islemTipiText = islemTipi === 1 ? 'Satın alma' : 'Rezervasyon'
      console.log(`🔍 ${islemTipiText} yapılıyor:`, { userId: finalUserId, tripId, seatId, price, islemTipi, paymentInfo })

      setIsProcessing(true)

      // Backend PascalCase bekliyor
      // IslemTipi: 0 = Sadece Rezervasyon, 1 = Satın Alma (Hemen Öde)
      const reservationData = {
        UserID: parseInt(finalUserId),
        TripID: parseInt(tripId),
        SeatID: parseInt(seatId),
        Price: parseFloat(price),
        PaymentMethod: 'Kredi Kartı',
        IslemTipi: islemTipi // 0 = Rezervasyon, 1 = Satın Alma
      }

      // Eğer kart bilgileri varsa ekle
      if (paymentInfo && islemTipi === 1) {
        reservationData.CardInfo = {
          Last4Digits: paymentInfo.cardNumber,
          CardHolder: paymentInfo.cardHolder,
          ExpiryMonth: paymentInfo.expiryMonth,
          ExpiryYear: paymentInfo.expiryYear,
          MaskedCardNumber: paymentInfo.maskedCardNumber
        }
      }

      const response = await reservationAPI.create(reservationData)

      console.log('🔍 Rezervasyon API Response:', response.data)

      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const message = response.data?.Message ?? response.data?.message
      const data = response.data?.Data ?? response.data?.data
      const paymentStatus = data?.PaymentStatus ?? data?.paymentStatus

      if (success) {
        if (islemTipi === 1) {
          setSnackbar({
            isOpen: true,
            message: 'Biletiniz başarıyla satın alındı!',
            type: 'success'
          })
          setTimeout(() => navigate('/purchased-tickets'), 2000)
        } else {
          setSnackbar({
            isOpen: true,
            message: 'Rezervasyon başarıyla oluşturuldu! Ödeme için "Rezervasyonlarım" sayfasından devam edebilirsiniz.',
            type: 'success'
          })
          setTimeout(() => navigate('/reservations'), 2000)
        }
      } else {
        setSnackbar({
          isOpen: true,
          message: message || 'İşlem başarısız oldu. Lütfen tekrar deneyin.',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Rezervasyon hatası:', error)
      const errorMessage = error.response?.data?.message || error.response?.data?.Message || error.message || 'Rezervasyon oluşturulurken bir hata oluştu'
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setIsProcessing(false)
      setPendingReservationType(null)
    }
  }

  const formatTime = (time) => {
    if (!time) return ''
    // TimeSpan objesi veya string olabilir
    if (typeof time === 'string') {
      return time.substring(0, 5) // "HH:mm" formatı
    }
    if (time.totalSeconds !== undefined) {
      const hours = Math.floor(time.totalSeconds / 3600)
      const minutes = Math.floor((time.totalSeconds % 3600) / 60)
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    // TimeSpan objesi direkt olarak gelirse
    const hours = time.hours || Math.floor((time.ticks || 0) / 36000000000)
    const minutes = time.minutes || Math.floor(((time.ticks || 0) % 36000000000) / 600000000)
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
  }

  // Debug: trip state değişikliklerini izle
  useEffect(() => {
    console.log('🔄 TripDetail - trip state güncellendi:', trip)
    if (trip) {
      console.log('📋 Trip verisi:', {
        TripID: trip.TripID,
        VehicleCode: trip.VehicleCode,
        VehicleModel: trip.VehicleModel,
        VehicleType: trip.VehicleType,
        FromCity: trip.FromCity,
        ToCity: trip.ToCity,
        Seats: trip.Seats?.length || 0
      })
      
      // Koltuk PaymentStatus değerlerini kontrol et
      if (trip.Seats && trip.Seats.length > 0) {
        console.log('🎫 İlk 5 koltuk PaymentStatus değerleri:')
        trip.Seats.slice(0, 5).forEach((seat, index) => {
          const paymentStatus = seat.PaymentStatus ?? seat.paymentStatus
          const isReserved = seat.IsReserved ?? seat.isReserved
          console.log(`  Koltuk ${seat.SeatNo ?? seat.seatNo}: PaymentStatus="${paymentStatus}", IsReserved=${isReserved}`, seat)
        })
        
        // PaymentStatus değerlerini grupla
        const statusCounts = trip.Seats.reduce((acc, seat) => {
          const status = (seat.PaymentStatus ?? seat.paymentStatus ?? 'null').toString()
          acc[status] = (acc[status] || 0) + 1
          return acc
        }, {})
        console.log('📊 PaymentStatus dağılımı:', statusCounts)
      }
    }
  }, [trip])

  if (loading) {
    console.log('⏳ TripDetail - Loading state: true')
    return (
      <div className="trip-detail-page">
        <div className="container">
          <div className="card">
            <p className="info-text">Yükleniyor...</p>
          </div>
        </div>
      </div>
    )
  }

  if (error || !trip) {
    console.log('❌ TripDetail - Error veya trip null:', { error, trip })
    return (
      <div className="trip-detail-page">
        <div className="container">
          <div className="card error-card">
            <p>{error || 'Sefer bulunamadı'}</p>
            <button className="btn btn-primary" onClick={() => navigate(-1)}>
              Geri Dön
            </button>
          </div>
        </div>
      </div>
    )
  }

  console.log('✅ TripDetail - Render ediliyor, trip var:', trip)

  // Veri normalizasyonu - hem PascalCase hem camelCase destekle
  const vehicleType = trip.VehicleType ?? trip.vehicleType ?? 'Bus'
  const vehicleCode = trip.VehicleCode ?? trip.vehicleCode ?? 'N/A'
  const vehicleModel = trip.VehicleModel ?? trip.vehicleModel ?? null
  const fromCity = trip.FromCity ?? trip.fromCity ?? 'N/A'
  const toCity = trip.ToCity ?? trip.toCity ?? 'N/A'
  const departureTerminal = trip.DepartureTerminal ?? trip.departureTerminal
  const arrivalTerminal = trip.ArrivalTerminal ?? trip.arrivalTerminal
  const departureStation = trip.DepartureStation ?? trip.departureStation
  const arrivalStation = trip.ArrivalStation ?? trip.arrivalStation
  const departureDate = trip.DepartureDate ?? trip.departureDate
  const departureTime = trip.DepartureTime ?? trip.departureTime
  const arrivalDate = trip.ArrivalDate ?? trip.arrivalDate
  const arrivalTime = trip.ArrivalTime ?? trip.arrivalTime
  const price = trip.Price ?? trip.price ?? 0
  const totalSeats = trip.TotalSeats ?? trip.totalSeats ?? 0
  const availableSeats = trip.AvailableSeats ?? trip.availableSeats ?? 0
  const layoutType = trip.LayoutType ?? trip.layoutType
  const seats = trip.Seats ?? trip.seats ?? []

  console.log('🔍 TripDetail - Normalize edilmiş veri:', {
    vehicleType,
    vehicleCode,
    vehicleModel,
    fromCity,
    toCity,
    seatsCount: seats.length
  })

  // Koltuk numarasını parse et
  const parseSeatNumber = (seatNo) => {
    if (!seatNo) return 0
    const num = parseInt(seatNo)
    return isNaN(num) ? 0 : num
  }

  // 2+2 düzeni için koltukları tek sırada organize et (1,2,3,4,5,6...)
  const organizeBusSeats2Plus2 = (seats) => {
    if (!seats || seats.length === 0) {
      console.log('⚠️ Koltuk listesi boş')
      return []
    }
    if (vehicleType !== 'Bus') {
      console.log('⚠️ VehicleType Bus değil:', vehicleType)
      return []
    }

    console.log('🔍 2+2 düzeni için koltuklar organize ediliyor:', seats.length, 'koltuk')

    // Koltukları numaraya göre sırala
    const sortedSeats = [...seats].sort((a, b) => {
      const numA = parseSeatNumber(a.SeatNo || a.seatNo || '0')
      const numB = parseSeatNumber(b.SeatNo || b.seatNo || '0')
      if (numA !== numB) return numA - numB
      return (a.SeatNo || a.seatNo || '').localeCompare(b.SeatNo || b.seatNo || '')
    })

    console.log('📋 Sıralanmış koltuklar:', sortedSeats.map(s => s.SeatNo || s.seatNo))

    // 2+2 düzeni: Her sırada 4 koltuk (2 sol, 2 sağ)
    // Tek sırada göster: 1,2,3,4,5,6,7,8...
    const rows = []
    let i = 0

    while (i < sortedSeats.length) {
      // Her sırada 4 koltuk: 1-2 (sol), 3-4 (sağ)
      const leftSeats = []
      const rightSeats = []
      
      // Sol taraf: 2 koltuk
      if (i < sortedSeats.length) {
        leftSeats.push(sortedSeats[i])
        i++
      }
      if (i < sortedSeats.length) {
        leftSeats.push(sortedSeats[i])
        i++
      }
      
      // Sağ taraf: 2 koltuk
      if (i < sortedSeats.length) {
        rightSeats.push(sortedSeats[i])
        i++
      }
      if (i < sortedSeats.length) {
        rightSeats.push(sortedSeats[i])
        i++
      }
      
      if (leftSeats.length > 0 || rightSeats.length > 0) {
        rows.push({
          left: leftSeats,
          right: rightSeats
        })
      }
    }

    console.log('✅ Organize edilmiş sıralar (2+2):', rows.length, 'sıra')
    return rows
  }

  // OBilet tarzı koltuk düzenleme: Görseldeki gibi
  // Düzen: Her sırada üstte çiftli (sağ), altta tekli (sol)
  // Örnek: 1 (sol alt), 2-3 (sağ üst), 4 (sol alt), 5-6 (sağ üst)...
  const organizeBusSeatsOBiletStyle = (seats) => {
    if (!seats || seats.length === 0) {
      console.log('⚠️ Koltuk listesi boş')
      return []
    }
    if (vehicleType !== 'Bus') {
      console.log('⚠️ VehicleType Bus değil:', vehicleType)
      return []
    }

    console.log('🔍 OBilet düzeni için koltuklar organize ediliyor:', seats.length, 'koltuk')

    // Koltukları numaraya göre sırala
    const sortedSeats = [...seats].sort((a, b) => {
      const numA = parseSeatNumber(a.SeatNo || a.seatNo || '0')
      const numB = parseSeatNumber(b.SeatNo || b.seatNo || '0')
      if (numA !== numB) return numA - numB
      return (a.SeatNo || a.seatNo || '').localeCompare(b.SeatNo || b.seatNo || '')
    })

    console.log('📋 Sıralanmış koltuklar:', sortedSeats.map(s => s.SeatNo || s.seatNo))

    // Koltukları sıralara göre düzenle
    // Her 3 koltuk bir sıra: 1 (sol alt), 2-3 (sağ üst)
    const rows = []
    let i = 0

    while (i < sortedSeats.length) {
      const seatNum = parseSeatNumber(sortedSeats[i].SeatNo || sortedSeats[i].seatNo || '0')
      
      // 1, 4, 7, 10, 13... (seatNum % 3 === 1) -> tekli sol alt
      if (seatNum % 3 === 1) {
        const leftSeat = sortedSeats[i]
        i++
        
        // Sağ tarafta 2 koltuk olmalı (2 ve 3, 5 ve 6, 8 ve 9, vb.)
        const rightSeats = []
        if (i < sortedSeats.length) {
          const nextSeatNum = parseSeatNumber(sortedSeats[i].SeatNo || sortedSeats[i].seatNo || '0')
          // Bir sonraki koltuk 2, 5, 8, 11... (seatNum % 3 === 2) olmalı
          if (nextSeatNum % 3 === 2 && nextSeatNum === seatNum + 1) {
            rightSeats.push(sortedSeats[i])
            i++
            
            // Bir sonraki koltuk 3, 6, 9, 12... (seatNum % 3 === 0) olmalı
            if (i < sortedSeats.length) {
              const thirdSeatNum = parseSeatNumber(sortedSeats[i].SeatNo || sortedSeats[i].seatNo || '0')
              if (thirdSeatNum % 3 === 0 && thirdSeatNum === seatNum + 2) {
                rightSeats.push(sortedSeats[i])
                i++
              }
            }
          }
        }
        
        rows.push({
          left: [leftSeat],
          right: rightSeats
        })
      }
      // 2, 5, 8, 11... (seatNum % 3 === 2) -> sağ tarafa başla
      else if (seatNum % 3 === 2) {
        const rightSeats = [sortedSeats[i]]
        i++
        
        // Bir sonraki koltuk 3, 6, 9, 12... (seatNum % 3 === 0) olmalı
        if (i < sortedSeats.length) {
          const nextSeatNum = parseSeatNumber(sortedSeats[i].SeatNo || sortedSeats[i].seatNo || '0')
          if (nextSeatNum % 3 === 0 && nextSeatNum === seatNum + 1) {
            rightSeats.push(sortedSeats[i])
            i++
          }
        }
        
        rows.push({
          left: [],
          right: rightSeats
        })
      }
      // 3, 6, 9, 12... (seatNum % 3 === 0) -> sağ tarafa ekle
      else {
        rows.push({
          left: [],
          right: [sortedSeats[i]]
        })
        i++
      }
    }

    console.log('✅ Organize edilmiş sıralar:', rows.length, 'sıra')
    return rows
  }

  // Koltukları vagonlara göre grupla (tren için) veya layoutType'a göre düzenle (otobüs için)
  const organizedBusSeats = vehicleType === 'Bus' 
    ? (layoutType === '2+2' 
        ? organizeBusSeats2Plus2(seats)
        : organizeBusSeatsOBiletStyle(seats))
    : []

  const seatsByWagon = vehicleType === 'Train' 
    ? seats.reduce((acc, seat) => {
        const wagonNo = seat.WagonNo ?? seat.wagonNo ?? 0
        if (!acc[wagonNo]) acc[wagonNo] = []
        acc[wagonNo].push(seat)
        return acc
      }, {})
    : { 0: seats }

  return (
    <div className="trip-detail-page">
      <div className="container">
        <div className="trip-header card">
          <div className="trip-header-top">
            <button className="btn-back" onClick={() => navigate(-1)}>← Geri Dön</button>
            <h1 className="trip-title">Sefer Detayları</h1>
          </div>
          
          <div className="trip-info">
            {/* Sefer Bilgileri */}
            <div className="trip-route">
              <div className="route-city departure">
                <div className="city-name">{fromCity}</div>
                <div className="location-name">
                  {departureTerminal || departureStation || 'Terminal/İstasyon'}
                </div>
                <div className="datetime">
                  <span className="date">
                    {departureDate ? new Date(departureDate).toLocaleDateString('tr-TR', { 
                      day: '2-digit', 
                      month: 'short', 
                      year: 'numeric' 
                    }) : 'Tarih yok'}
                  </span>
                  <span className="time">{formatTime(departureTime)}</span>
                </div>
              </div>
              
              <div className="route-arrow-container">
                <div className="route-line"></div>
                <div className="route-arrow">→</div>
                <div className="route-line"></div>
              </div>
              
              <div className="route-city arrival">
                <div className="city-name">{toCity}</div>
                <div className="location-name">
                  {arrivalTerminal || arrivalStation || 'Terminal/İstasyon'}
                </div>
                {arrivalDate && (
                  <div className="datetime">
                    <span className="date">
                      {new Date(arrivalDate).toLocaleDateString('tr-TR', { 
                        day: '2-digit', 
                        month: 'short', 
                        year: 'numeric' 
                      })}
                    </span>
                    <span className="time">{formatTime(arrivalTime)}</span>
                  </div>
                )}
              </div>
            </div>

            {/* Sefer Özeti */}
            <div className="trip-summary">
              <div className="summary-item">
                <span className="summary-icon">🚌</span>
                <div className="summary-content">
                  <span className="summary-label">Araç</span>
                  <span className="summary-value">{vehicleCode}</span>
                </div>
              </div>
              <div className="summary-item">
                <span className="summary-icon">📋</span>
                <div className="summary-content">
                  <span className="summary-label">Model</span>
                  <span className="summary-value">{vehicleModel || 'Belirtilmemiş'}</span>
                </div>
              </div>
              {layoutType && (
                <div className="summary-item">
                  <span className="summary-icon">🪑</span>
                  <div className="summary-content">
                    <span className="summary-label">Düzen</span>
                    <span className="summary-value">{layoutType}</span>
                  </div>
                </div>
              )}
              <div className="summary-item">
                <span className="summary-icon">💰</span>
                <div className="summary-content">
                  <span className="summary-label">Bilet Fiyatı</span>
                  <span className="summary-value price">{price ? Number(price).toFixed(2) : '0.00'} ₺</span>
                </div>
              </div>
              <div className="summary-item">
                <span className="summary-icon">✅</span>
                <div className="summary-content">
                  <span className="summary-label">Boş Koltuk</span>
                  <span className="summary-value highlight">{availableSeats} / {totalSeats}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="seat-selection card">
          <h2>Koltuk Seçimi</h2>
          
          {vehicleType === 'Bus' && organizedBusSeats.length > 0 ? (
            // OBilet tarzı otobüs koltuk düzeni
            <div className="obilet-seat-map">
              <div className="bus-layout">
                {/* Ön kısım */}
                <div className="bus-front">
                  <div className="bus-front-icon">🚌</div>
                  <div className="bus-front-label">ÖN</div>
                </div>
                
                {/* Koltuk alanı */}
                <div className="bus-seats-area">
                  <div className="seats-container">
                    {organizedBusSeats.map((row, rowIndex) => (
                      <div key={rowIndex} className="seat-row">
                        {/* Sol taraf - koltuklar (2+2 düzeninde 2 koltuk, OBilet tarzında tek koltuk) */}
                        <div className="seat-group left-group">
                          {(layoutType === '2+2' ? [...row.left].reverse() : row.left).map(seat => {
                            const seatID = seat.SeatID ?? seat.seatID
                            const seatNo = seat.SeatNo ?? seat.seatNo ?? '?'
                            const isActive = seat.IsActive ?? seat.isActive ?? true
                            const paymentStatus = (seat.PaymentStatus ?? seat.paymentStatus ?? '').toString().trim()
                            const isSelected = selectedSeat && (selectedSeat.SeatID === seatID || selectedSeat.seatID === seatID)
                            
                            // Koltuk durumunu belirle: Sadece PaymentStatus'e göre (case-insensitive)
                            const paymentStatusLower = paymentStatus.toLowerCase()
                            let seatStatus = 'available'
                            let seatTitle = `Koltuk ${seatNo}`
                            
                            // Debug: İlk birkaç koltuk için log
                            if ((seatNo === '1' || seatNo === 1 || seatNo === '2' || seatNo === 2) && paymentStatus) {
                              console.log(`🔍 Koltuk ${seatNo} - PaymentStatus: "${paymentStatus}" (lower: "${paymentStatusLower}") -> Status: ${seatStatus}`, seat)
                            }
                            
                            if (paymentStatusLower === 'paid') {
                              seatStatus = 'dolu'
                              seatTitle = 'Dolu (Ödendi)'
                            } else if (paymentStatusLower === 'pending') {
                              seatStatus = 'reserved'
                              seatTitle = 'Rezerve (Ödeme Bekliyor)'
                            }
                            
                            // Debug: Status belirlendikten sonra
                            if ((seatNo === '1' || seatNo === 1 || seatNo === '2' || seatNo === 2) && paymentStatus) {
                              console.log(`✅ Koltuk ${seatNo} - Final Status: ${seatStatus} (PaymentStatus: "${paymentStatusLower}")`)
                            }
                            
                            return (
                              <button
                                key={seatID}
                                className={`seat ${seatStatus} ${!isActive ? 'inactive' : ''} ${isSelected ? 'selected' : ''}`}
                                onClick={() => handleSeatSelect(seat)}
                                disabled={paymentStatusLower === 'paid' || paymentStatusLower === 'pending' || !isActive}
                                title={seatTitle}
                              >
                                <span className="seat-number">{seatNo}</span>
                              </button>
                            )
                          })}
                        </div>
                        
                        {/* Koridor - sadece sol ve sağ grupların ikisi de dolu olduğunda göster */}
                        {row.left.length > 0 && row.right.length > 0 && (
                          <div className="aisle"></div>
                        )}
                        
                        {/* Sağ taraf - çiftli koltuklar (ters sırada: 3, 2) */}
                        <div className="seat-group right-group">
                          {[...row.right].reverse().map(seat => {
                            const seatID = seat.SeatID ?? seat.seatID
                            const seatNo = seat.SeatNo ?? seat.seatNo ?? '?'
                            const isActive = seat.IsActive ?? seat.isActive ?? true
                            const paymentStatus = (seat.PaymentStatus ?? seat.paymentStatus ?? '').toString().trim()
                            const paymentStatusLower = paymentStatus.toLowerCase()
                            const isSelected = selectedSeat && (selectedSeat.SeatID === seatID || selectedSeat.seatID === seatID)
                            
                            // Koltuk durumunu belirle: Sadece PaymentStatus'e göre (case-insensitive)
                            let seatStatus = 'available'
                            let seatTitle = `Koltuk ${seatNo}`
                            
                            if (paymentStatusLower === 'paid') {
                              seatStatus = 'dolu'
                              seatTitle = 'Dolu (Ödendi)'
                            } else if (paymentStatusLower === 'pending') {
                              seatStatus = 'reserved'
                              seatTitle = 'Rezerve (Ödeme Bekliyor)'
                            }
                            
                            return (
                              <button
                                key={seatID}
                                className={`seat ${seatStatus} ${!isActive ? 'inactive' : ''} ${isSelected ? 'selected' : ''}`}
                                onClick={() => handleSeatSelect(seat)}
                                disabled={paymentStatusLower === 'paid' || paymentStatusLower === 'pending' || !isActive}
                                title={seatTitle}
                              >
                                <span className="seat-number">{seatNo}</span>
                              </button>
                            )
                          })}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
                
                {/* Arka kısım */}
                <div className="bus-back">
                  <div className="bus-back-label">ARKA</div>
                </div>
              </div>
            </div>
          ) : vehicleType === 'Bus' && seats.length === 0 ? (
            <div className="info-text" style={{ textAlign: 'center', padding: '40px' }}>
              <p>Bu sefer için koltuk bilgisi bulunamadı.</p>
            </div>
          ) : (
            // Tren veya düzenlenmemiş otobüs için grid görünüm
            Object.keys(seatsByWagon).sort((a, b) => Number(a) - Number(b)).map(wagonNo => {
              const wagonSeats = seatsByWagon[wagonNo] || []
              return (
                <div key={wagonNo} className="wagon-section">
                  {vehicleType === 'Train' && (
                    <h3>Vagon {wagonNo}</h3>
                  )}
                  <div className="seat-map">
                    {wagonSeats.map(seat => {
                      const seatID = seat.SeatID ?? seat.seatID
                      const seatNo = seat.SeatNo ?? seat.seatNo ?? '?'
                      const isActive = seat.IsActive ?? seat.isActive ?? true
                      const paymentStatus = (seat.PaymentStatus ?? seat.paymentStatus ?? '').toString().trim()
                      const paymentStatusLower = paymentStatus.toLowerCase()
                      const isSelected = selectedSeat && (selectedSeat.SeatID === seatID || selectedSeat.seatID === seatID)
                      
                      // Koltuk durumunu belirle: Sadece PaymentStatus'e göre (case-insensitive)
                      let seatStatus = 'available'
                      let seatTitle = `Koltuk ${seatNo}`
                      
                      if (paymentStatusLower === 'paid') {
                        seatStatus = 'dolu'
                        seatTitle = 'Dolu (Ödendi)'
                      } else if (paymentStatusLower === 'pending') {
                        seatStatus = 'reserved'
                        seatTitle = 'Rezerve (Ödeme Bekliyor)'
                      }
                      
                      return (
                        <button
                          key={seatID}
                          className={`seat ${seatStatus} ${!isActive ? 'inactive' : ''} ${isSelected ? 'selected' : ''}`}
                          onClick={() => handleSeatSelect(seat)}
                          disabled={paymentStatusLower === 'paid' || paymentStatusLower === 'pending' || !isActive}
                          title={seatTitle}
                        >
                          {seatNo}
                        </button>
                      )
                    })}
                  </div>
                </div>
              )
            })
          )}
          
          <div className="seat-legend">
            <div className="legend-item">
              <span className="seat-legend-box available"></span>
              <span>Boş</span>
            </div>
            <div className="legend-item">
              <span className="seat-legend-box reserved"></span>
              <span>Rezerve</span>
            </div>
            <div className="legend-item">
              <span className="seat-legend-box dolu"></span>
              <span>Dolu</span>
            </div>
            <div className="legend-item">
              <span className="seat-legend-box selected"></span>
              <span>Seçili</span>
            </div>
          </div>

          {selectedSeat && (
            <div className="selected-seat-info">
              <h3>Seçili Koltuk</h3>
              <p>
                <span className="label">Koltuk No:</span>
                <strong>{selectedSeat.SeatNo ?? selectedSeat.seatNo ?? 'N/A'}</strong>
              </p>
              <p>
                <span className="label">Fiyat:</span>
                <strong className="price">{price ? Number(price).toFixed(2) : '0.00'} ₺</strong>
              </p>
              <div className="reservation-buttons">
                <button 
                  className="btn btn-primary btn-reserve" 
                  onClick={() => handleReservationClick(0)}
                  disabled={isProcessing}
                >
                  🎫 Rezervasyon Yap
                </button>
                <button 
                  className="btn btn-secondary btn-purchase" 
                  onClick={() => handleReservationClick(1)}
                  disabled={isProcessing}
                >
                  💳 Hemen Satın Al
                </button>
              </div>
            </div>
          )}
          
          {!selectedSeat && seats.length > 0 && (
            <div className="info-text" style={{ textAlign: 'center', padding: '20px', color: 'var(--text-secondary)' }}>
              <p>Lütfen bir koltuk seçin</p>
            </div>
          )}
        </div>
      </div>

      {/* Ödeme Modal */}
      {showPaymentModal && selectedSeat && (
        <PaymentModal
          isOpen={showPaymentModal}
          onClose={() => {
            setShowPaymentModal(false)
            setPendingReservationType(null)
          }}
          onConfirm={handlePaymentConfirm}
          amount={price ? parseFloat(price) : 0}
          loading={isProcessing}
        />
      )}
      
      <Snackbar
        isOpen={snackbar.isOpen}
        message={snackbar.message}
        type={snackbar.type}
        onClose={() => setSnackbar({ ...snackbar, isOpen: false })}
      />
    </div>
  )
}

export default TripDetail

