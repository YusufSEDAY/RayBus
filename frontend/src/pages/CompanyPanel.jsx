import { useState, useEffect, useMemo } from 'react'
import { companyAPI, cityAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './CompanyPanel.css'

const CompanyPanel = () => {
  const [activeTab, setActiveTab] = useState('trips')
  const [trips, setTrips] = useState([])
  const [stats, setStats] = useState(null)
  const [loading, setLoading] = useState(false)
  const [showTripForm, setShowTripForm] = useState(false)
  const [editingTrip, setEditingTrip] = useState(null)
  const [cities, setCities] = useState([])
  const [vehicles, setVehicles] = useState([])
  const [terminals, setTerminals] = useState({})
  const [stations, setStations] = useState({})
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })
  
  // Pagination
  const [tripsDisplayLimit, setTripsDisplayLimit] = useState(100)
  
  // Filtreleme
  const [tripFromCityFilter, setTripFromCityFilter] = useState('')
  const [tripToCityFilter, setTripToCityFilter] = useState('')
  const [tripFromCityIDFilter, setTripFromCityIDFilter] = useState('')
  const [tripToCityIDFilter, setTripToCityIDFilter] = useState('')
  const [tripStatusFilter, setTripStatusFilter] = useState('') // '' = Tümü, 'Aktif', 'İptal'
  const [tripDateFilter, setTripDateFilter] = useState('')
  const [tripDateFromFilter, setTripDateFromFilter] = useState('')
  const [tripDateToFilter, setTripDateToFilter] = useState('')
  
  // İptal modal
  const [showCancelModal, setShowCancelModal] = useState(false)
  const [selectedTripForCancel, setSelectedTripForCancel] = useState(null)
  const [cancelReason, setCancelReason] = useState('')
  
  // Görünüm modu (card veya table)
  const [viewMode, setViewMode] = useState('table') // 'table' veya 'card'
  const [formData, setFormData] = useState({
    VehicleID: '',
    FromCityID: '',
    ToCityID: '',
    DepartureTerminalID: '',
    ArrivalTerminalID: '',
    DepartureStationID: '',
    ArrivalStationID: '',
    DepartureDate: '',
    DepartureTime: '',
    ArrivalDate: '',
    ArrivalTime: '',
    Price: '',
    VehicleType: 'Bus'
  })

  useEffect(() => {
    if (activeTab === 'trips') {
      setTripsDisplayLimit(100) // Tab değiştiğinde limit'i sıfırla
      fetchTrips()
    } else if (activeTab === 'stats') {
      fetchStats()
    }
    fetchCities()
  }, [activeTab])

  useEffect(() => {
    if (formData.VehicleType) {
      fetchVehicles(formData.VehicleType)
    }
  }, [formData.VehicleType])

  useEffect(() => {
    if (formData.FromCityID) {
      fetchTerminalsAndStations(formData.FromCityID, 'from')
    } else {
      setTerminals(prev => ({ ...prev, from: [] }))
      setStations(prev => ({ ...prev, from: [] }))
    }
  }, [formData.FromCityID])

  useEffect(() => {
    if (formData.ToCityID) {
      fetchTerminalsAndStations(formData.ToCityID, 'to')
    } else {
      setTerminals(prev => ({ ...prev, to: [] }))
      setStations(prev => ({ ...prev, to: [] }))
    }
  }, [formData.ToCityID])

  const showSnackbar = (message, type = 'success') => {
    setSnackbar({ isOpen: true, message, type })
  }

  const fetchCities = async () => {
    try {
      const response = await cityAPI.getAll()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      if (success && Array.isArray(data)) {
        const citiesData = data
          .map(city => ({
            CityID: city.CityID ?? city.cityID ?? city.CityId ?? city.cityId,
            CityName: city.CityName ?? city.cityName ?? city.City_Name
          }))
          .filter(city => city.CityID && city.CityName)
          .sort((a, b) => a.CityName.localeCompare(b.CityName, 'tr'))
        setCities(citiesData)
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Şehirler yüklenirken hata:', error)
      }
      showSnackbar('Şehirler yüklenirken bir hata oluştu', 'error')
    }
  }

  const fetchVehicles = async (vehicleType) => {
    try {
      const response = await companyAPI.getVehicles(vehicleType)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et (camelCase -> PascalCase)
        const normalizedVehicles = data.map(vehicle => ({
          VehicleID: vehicle.VehicleID ?? vehicle.vehicleID ?? vehicle.VehicleId ?? vehicle.vehicleId ?? 0,
          VehicleType: vehicle.VehicleType ?? vehicle.vehicleType ?? '',
          PlateOrCode: vehicle.PlateOrCode ?? vehicle.plateOrCode ?? vehicle.Plate_Or_Code ?? '',
          SeatCount: vehicle.SeatCount ?? vehicle.seatCount ?? vehicle.Seat_Count ?? 0,
          Active: vehicle.Active ?? vehicle.active ?? true
        }))
        
        if (process.env.NODE_ENV === 'development' && normalizedVehicles.length > 0) {
          console.log('🚗 İlk araç (normalized):', normalizedVehicles[0])
        }
        
        setVehicles(normalizedVehicles)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Vehicles: Success veya Data yok veya array değil', { success, isArray: Array.isArray(data), data })
        }
        setVehicles([])
      }
    } catch (error) {
      console.error('❌ Araçlar yüklenirken hata:', error)
      console.error('❌ Error response:', error.response?.data)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Araçlar yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setVehicles([])
    }
  }

  const fetchTerminalsAndStations = async (cityId, direction) => {
    try {
      const [terminalsResponse, stationsResponse] = await Promise.all([
        cityAPI.getTerminals(cityId).catch(() => ({ data: { Success: false, Data: [] } })),
        cityAPI.getStations(cityId).catch(() => ({ data: { Success: false, Data: [] } }))
      ])

      const terminalsSuccess = terminalsResponse?.data?.Success ?? terminalsResponse?.data?.success
      const terminalsData = terminalsResponse?.data?.Data ?? terminalsResponse?.data?.data ?? []
      const terminalsList = terminalsSuccess && Array.isArray(terminalsData) ? terminalsData : []

      const stationsSuccess = stationsResponse?.data?.Success ?? stationsResponse?.data?.success
      const stationsData = stationsResponse?.data?.Data ?? stationsResponse?.data?.data ?? []
      const stationsList = stationsSuccess && Array.isArray(stationsData) ? stationsData : []

      setTerminals(prev => ({ ...prev, [direction]: terminalsList }))
      setStations(prev => ({ ...prev, [direction]: stationsList }))
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error(`Terminal/istasyon yüklenirken hata (${direction}):`, error)
      }
    }
  }

  // JWT kaldırıldı - Token kontrolü devre dışı
  // const isTokenValid = () => { return true } // Artık her zaman true döner

  const fetchTrips = async () => {
    setLoading(true)
    try {
      // localStorage'dan kullanıcı bilgisini al
      const savedUser = localStorage.getItem('raybus_user')
      let companyID = null
      
      if (savedUser) {
        try {
          const user = JSON.parse(savedUser)
          // Eğer kullanıcı şirket rolündeyse, UserID'yi şirket ID olarak kullan
          if (user.RoleName === 'Şirket' || user.roleName === 'Şirket') {
            companyID = user.UserID || user.userID || user.UserId || user.userId
          }
        } catch (e) {
          console.warn('⚠️ Kullanıcı bilgisi parse edilemedi:', e)
        }
      }
      
      // Şirket ID'sini query parameter olarak gönder
      const response = companyID 
        ? await companyAPI.getMyTrips(`?sirketID=${companyID}`)
        : await companyAPI.getMyTrips()
      
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 Company Trips Response:', response)
        console.log('🔍 Response Data:', response.data)
      }
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 Success:', success, 'Data:', data, 'Data Type:', typeof data, 'Is Array:', Array.isArray(data))
        if (Array.isArray(data) && data.length > 0) {
          console.log('🔍 İlk sefer (raw):', data[0])
          console.log('🔍 İlk sefer keys:', Object.keys(data[0]))
        }
      }
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et (camelCase -> PascalCase)
        const normalizedTrips = data.map(trip => {
          const normalized = {
            TripID: trip.TripID ?? trip.tripID ?? trip.TripId ?? trip.tripId ?? 0,
            AracPlaka: trip.AracPlaka ?? trip.aracPlaka ?? trip.Arac_Plaka ?? '',
            Guzergah: trip.Guzergah ?? trip.guzergah ?? trip.Guzergah ?? '',
            Tarih: trip.Tarih ?? trip.tarih ?? trip.DepartureDate ?? trip.departureDate ?? null,
            Saat: trip.Saat ?? trip.saat ?? trip.DepartureTime ?? trip.departureTime ?? null,
            Price: trip.Price ?? trip.price ?? trip.Fiyat ?? trip.fiyat ?? 0,
            Durum: trip.Durum ?? trip.durum ?? trip.Status ?? trip.status ?? '',
            DoluKoltukSayisi: trip.DoluKoltukSayisi ?? trip.doluKoltukSayisi ?? trip.Dolu_Koltuk_Sayisi ?? 0,
            ToplamKoltuk: trip.ToplamKoltuk ?? trip.toplamKoltuk ?? trip.Toplam_Koltuk ?? 0,
            // Ekstra alanlar (varsa)
            FromCity: trip.FromCity ?? trip.fromCity ?? '',
            ToCity: trip.ToCity ?? trip.toCity ?? '',
            VehicleType: trip.VehicleType ?? trip.vehicleType ?? '',
            Status: (trip.Durum ?? trip.durum ?? trip.Status ?? trip.status ?? '') === 'Aktif' ? 1 : 0
          }
          
          if (process.env.NODE_ENV === 'development') {
            console.log('🔍 Normalized trip:', normalized)
          }
          
          return normalized
        })
        
        if (process.env.NODE_ENV === 'development') {
          console.log('🚌 Normalized trips count:', normalizedTrips.length)
          if (normalizedTrips.length > 0) {
            console.log('🚌 İlk sefer (normalized):', normalizedTrips[0])
          }
        }
        
        setTrips(normalizedTrips)
        
        // Debug: Normalize edilmiş sefer sayısını logla
        if (process.env.NODE_ENV === 'development') {
          console.log(`✅ Toplam ${normalizedTrips.length} sefer yüklendi ve state'e kaydedildi`)
        }
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Trips: Success veya Data yok veya array değil', { 
            success, 
            isArray: Array.isArray(data), 
            data,
            responseData: response.data
          })
        }
        // Eğer success true ama data boşsa, bu normal olabilir (henüz sefer yok)
        if (success && (!data || (Array.isArray(data) && data.length === 0))) {
          showSnackbar('Henüz seferiniz bulunmuyor. Yeni sefer eklemek için "Yeni Sefer Ekle" butonuna tıklayın.', 'info')
        } else {
          showSnackbar('Seferler yüklenirken bir sorun oluştu. Lütfen sayfayı yenileyin.', 'warning')
        }
        setTrips([])
      }
    } catch (error) {
      console.error('❌ Seferler yüklenirken hata:', error)
      console.error('❌ Error response:', error.response?.data)
      console.error('❌ Error status:', error.response?.status)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Seferler yüklenirken bir hata oluştu'
      if (error.response?.status === 403) {
        showSnackbar('Bu işlem için yetkiniz yok. Şirket rolü gereklidir.', 'error')
      } else if (error.response?.status === 400) {
        showSnackbar(`Hata: ${errorMessage}`, 'error')
      } else {
        showSnackbar('Seferler yüklenirken bir hata oluştu. Lütfen sayfayı yenileyin.', 'error')
      }
      setTrips([])
    } finally {
      setLoading(false)
    }
  }

  const fetchStats = async () => {
    setLoading(true)
    try {
      const response = await companyAPI.getCompanyStats()
      
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 CompanyPanel - fetchStats - Raw response:', response)
        console.log('🔍 CompanyPanel - fetchStats - response.data:', response.data)
      }
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 CompanyPanel - fetchStats - success:', success)
        console.log('🔍 CompanyPanel - fetchStats - data:', data)
        console.log('🔍 CompanyPanel - fetchStats - data keys:', data ? Object.keys(data) : 'data is null')
      }
      
      if (success && data) {
        // Verileri normalize et (PascalCase -> PascalCase, backend zaten PascalCase döndürüyor)
        const normalizedStats = {
          SirketID: data.SirketID ?? data.sirketID,
          SirketAdi: data.SirketAdi ?? data.sirketAdi,
          SirketEmail: data.SirketEmail ?? data.sirketEmail,
          TotalTrips: data.TotalTrips ?? data.totalTrips ?? 0,
          ActiveTrips: data.ActiveTrips ?? data.activeTrips ?? 0,
          IptalSefer: data.IptalSefer ?? data.iptalSefer ?? 0,
          TotalReservations: data.TotalReservations ?? data.totalReservations ?? 0,
          ActiveReservations: data.ActiveReservations ?? data.activeReservations ?? 0,
          IptalRezervasyon: data.IptalRezervasyon ?? data.iptalRezervasyon ?? 0,
          ToplamGelir: data.ToplamGelir ?? data.toplamGelir ?? 0,
          SonBirAyGelir: data.SonBirAyGelir ?? data.sonBirAyGelir ?? 0,
          ToplamArac: data.ToplamArac ?? data.toplamArac ?? 0,
          OtobusSayisi: data.OtobusSayisi ?? data.otobusSayisi ?? 0,
          TrenSayisi: data.TrenSayisi ?? data.trenSayisi ?? 0,
          OrtalamaDoluKoltukOrani: data.OrtalamaDoluKoltukOrani ?? data.ortalamaDoluKoltukOrani ?? 0,
          BuAyEklenenSefer: data.BuAyEklenenSefer ?? data.buAyEklenenSefer ?? 0,
          SonGuncellemeTarihi: data.SonGuncellemeTarihi ?? data.sonGuncellemeTarihi
        }
        
        if (process.env.NODE_ENV === 'development') {
          console.log('🔍 CompanyPanel - fetchStats - normalizedStats:', normalizedStats)
        }
        
        setStats(normalizedStats)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ CompanyPanel - fetchStats - success is false or data is null/undefined')
          console.warn('⚠️ CompanyPanel - fetchStats - success:', success, 'data:', data)
        }
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ CompanyPanel - fetchStats - İstatistikler yüklenirken hata:', error)
        console.error('❌ CompanyPanel - fetchStats - Error response:', error.response)
      }
      showSnackbar('İstatistikler yüklenirken bir hata oluştu', 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      // Tarih ve Saat'i doğru formata çevir
      if (!formData.DepartureDate || !formData.DepartureTime) {
        showSnackbar('Lütfen kalkış tarihi ve saat bilgilerini girin', 'error')
        setLoading(false)
        return
      }
      
      const departureDate = new Date(formData.DepartureDate)
      if (isNaN(departureDate.getTime())) {
        showSnackbar('Geçersiz tarih formatı', 'error')
        setLoading(false)
        return
      }
      
      // Saat formatını kontrol et
      const timeParts = formData.DepartureTime.split(':')
      if (timeParts.length < 2) {
        showSnackbar('Geçersiz saat formatı (HH:mm olmalı)', 'error')
        setLoading(false)
        return
      }
      
      const hours = parseInt(timeParts[0])
      const minutes = parseInt(timeParts[1])
      if (isNaN(hours) || isNaN(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
        showSnackbar('Geçersiz saat değerleri', 'error')
        setLoading(false)
        return
      }
      
      // TimeSpan formatı: "HH:mm:ss"
      const departureTime = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`
      
      // ArrivalTime varsa onu da formatla
      let arrivalTime = null
      if (formData.ArrivalTime) {
        const arrivalTimeParts = formData.ArrivalTime.split(':')
        if (arrivalTimeParts.length >= 2) {
          const arrivalHours = parseInt(arrivalTimeParts[0])
          const arrivalMinutes = parseInt(arrivalTimeParts[1])
          if (!isNaN(arrivalHours) && !isNaN(arrivalMinutes)) {
            arrivalTime = `${arrivalHours.toString().padStart(2, '0')}:${arrivalMinutes.toString().padStart(2, '0')}:00`
          }
        }
      }
      
      const submitData = {
        ...formData,
        VehicleID: parseInt(formData.VehicleID),
        FromCityID: parseInt(formData.FromCityID),
        ToCityID: parseInt(formData.ToCityID),
        DepartureTerminalID: formData.DepartureTerminalID ? parseInt(formData.DepartureTerminalID) : null,
        ArrivalTerminalID: formData.ArrivalTerminalID ? parseInt(formData.ArrivalTerminalID) : null,
        DepartureStationID: formData.DepartureStationID ? parseInt(formData.DepartureStationID) : null,
        ArrivalStationID: formData.ArrivalStationID ? parseInt(formData.ArrivalStationID) : null,
        Price: parseFloat(formData.Price),
        DepartureDate: departureDate.toISOString().split('T')[0], // YYYY-MM-DD formatı
        DepartureTime: departureTime, // HH:mm:ss formatı
        ArrivalDate: formData.ArrivalDate || null,
        ArrivalTime: arrivalTime
      }

      if (editingTrip) {
        await companyAPI.updateTrip(editingTrip.TripID, submitData)
        showSnackbar('Sefer başarıyla güncellendi', 'success')
      } else {
        await companyAPI.createTrip(submitData)
        showSnackbar('Sefer başarıyla oluşturuldu', 'success')
      }
      setShowTripForm(false)
      setEditingTrip(null)
      resetForm()
      fetchTrips()
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Sefer kaydedilirken hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Sefer kaydedilemedi'
      showSnackbar(errorMessage, 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleEdit = async (trip) => {
    setEditingTrip(trip)
    
    // Guzergah'dan şehir isimlerini parse et veya FromCity/ToCity kullan
    let fromCityName = trip.FromCity
    let toCityName = trip.ToCity
    
    if (trip.Guzergah && !fromCityName) {
      const parts = trip.Guzergah.split(' > ')
      if (parts.length >= 2) {
        fromCityName = parts[0].trim()
        toCityName = parts[1].trim()
      }
    }
    
    const fromCityID = trip.FromCityID ?? (cities.find(c => c.CityName === fromCityName)?.CityID)
    const toCityID = trip.ToCityID ?? (cities.find(c => c.CityName === toCityName)?.CityID)
    
    // Tarih ve saat formatlarını düzelt
    const departureDate = trip.Tarih || trip.DepartureDate
    const departureTime = trip.Saat || trip.DepartureTime
    
    setFormData({
      VehicleID: trip.VehicleID || '',
      FromCityID: fromCityID ? fromCityID.toString() : '',
      ToCityID: toCityID ? toCityID.toString() : '',
      DepartureTerminalID: trip.DepartureTerminalID ? trip.DepartureTerminalID.toString() : '',
      ArrivalTerminalID: trip.ArrivalTerminalID ? trip.ArrivalTerminalID.toString() : '',
      DepartureStationID: trip.DepartureStationID ? trip.DepartureStationID.toString() : '',
      ArrivalStationID: trip.ArrivalStationID ? trip.ArrivalStationID.toString() : '',
      DepartureDate: departureDate ? new Date(departureDate).toISOString().split('T')[0] : '',
      DepartureTime: departureTime ? (typeof departureTime === 'string' ? departureTime.substring(0, 5) : departureTime.toString().substring(0, 5)) : '',
      ArrivalDate: trip.ArrivalDate ? new Date(trip.ArrivalDate).toISOString().split('T')[0] : '',
      ArrivalTime: trip.ArrivalTime ? (typeof trip.ArrivalTime === 'string' ? trip.ArrivalTime.substring(0, 5) : trip.ArrivalTime.toString().substring(0, 5)) : '',
      Price: trip.Price || '',
      VehicleType: trip.VehicleType || 'Bus'
    })
    
    setShowTripForm(true)
  }

  const handleCancel = (trip) => {
    setSelectedTripForCancel(trip)
    setCancelReason('')
    setShowCancelModal(true)
  }
  
  const confirmCancel = async () => {
    if (!selectedTripForCancel) return
    
    try {
      await companyAPI.cancelTrip(selectedTripForCancel.TripID, {
        IptalNedeni: cancelReason || null
      })
      showSnackbar('Sefer başarıyla iptal edildi', 'success')
      setShowCancelModal(false)
      setSelectedTripForCancel(null)
      setCancelReason('')
      fetchTrips()
    } catch (error) {
      console.error('❌ Sefer iptal edilirken hata:', error)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Sefer iptal edilemedi'
      showSnackbar(errorMessage, 'error')
    }
  }

  const handleDelete = async (tripId) => {
    if (window.confirm('Bu seferi silmek istediğinize emin misiniz?')) {
      try {
        await companyAPI.deleteTrip(tripId)
        showSnackbar('Sefer başarıyla silindi', 'success')
        fetchTrips()
      } catch (error) {
        if (process.env.NODE_ENV === 'development') {
          console.error('Sefer silinirken hata:', error)
        }
        const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Sefer silinemedi'
        showSnackbar(errorMessage, 'error')
      }
    }
  }

  const resetForm = () => {
    setFormData({
      VehicleID: '',
      FromCityID: '',
      ToCityID: '',
      DepartureTerminalID: '',
      ArrivalTerminalID: '',
      DepartureStationID: '',
      ArrivalStationID: '',
      DepartureDate: '',
      DepartureTime: '',
      ArrivalDate: '',
      ArrivalTime: '',
      Price: '',
      VehicleType: 'Bus'
    })
    setTerminals({})
    setStations({})
  }

  const formatDate = (date) => {
    if (!date) return ''
    return new Date(date).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  const formatTime = (time) => {
    if (!time) return ''
    if (typeof time === 'string') {
      return time.substring(0, 5)
    }
    return time.toString().substring(0, 5)
  }
  
  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('tr-TR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount || 0)
  }
  
  // Filtrelenmiş seferler
  const filteredTrips = useMemo(() => {
    if (process.env.NODE_ENV === 'development') {
      console.log(`🔍 Filtreleme başlıyor. Toplam sefer sayısı: ${trips.length}`)
    }
    
    const filtered = trips.filter(trip => {
      // Şehir ID filtresi (dropdown'dan seçilen)
      if (tripFromCityIDFilter) {
        const fromCity = cities.find(c => c.CityID === parseInt(tripFromCityIDFilter))
        if (fromCity) {
          const guzergah = trip.Guzergah || `${trip.FromCity} > ${trip.ToCity}`
          if (!guzergah.includes(fromCity.CityName)) return false
        }
      }
      if (tripToCityIDFilter) {
        const toCity = cities.find(c => c.CityID === parseInt(tripToCityIDFilter))
        if (toCity) {
          const guzergah = trip.Guzergah || `${trip.FromCity} > ${trip.ToCity}`
          if (!guzergah.includes(toCity.CityName)) return false
        }
      }
      
      // Text arama filtresi
      if (tripFromCityFilter) {
        const guzergah = trip.Guzergah || `${trip.FromCity} > ${trip.ToCity}`
        if (!guzergah.toLowerCase().includes(tripFromCityFilter.toLowerCase())) return false
      }
      if (tripToCityFilter) {
        const guzergah = trip.Guzergah || `${trip.FromCity} > ${trip.ToCity}`
        if (!guzergah.toLowerCase().includes(tripToCityFilter.toLowerCase())) return false
      }
      
      // Durum filtresi
      if (tripStatusFilter && trip.Durum !== tripStatusFilter) return false
      
      // Tarih filtresi
      if (tripDateFilter && trip.Tarih) {
        const tripDate = new Date(trip.Tarih)
        const filterDate = new Date(tripDateFilter)
        const tripDateStr = tripDate.toISOString().split('T')[0]
        const filterDateStr = filterDate.toISOString().split('T')[0]
        if (tripDateStr !== filterDateStr) return false
      }
      
      // Tarih aralığı filtresi
      if (tripDateFromFilter && trip.Tarih) {
        const tripDate = new Date(trip.Tarih)
        const fromDate = new Date(tripDateFromFilter)
        if (tripDate < fromDate) return false
      }
      if (tripDateToFilter && trip.Tarih) {
        const tripDate = new Date(trip.Tarih)
        const toDate = new Date(tripDateToFilter)
        toDate.setHours(23, 59, 59, 999)
        if (tripDate > toDate) return false
      }
      
      return true
    })
    
    if (process.env.NODE_ENV === 'development') {
      console.log(`✅ Filtreleme tamamlandı. Filtrelenmiş sefer sayısı: ${filtered.length}`)
      if (filtered.length < trips.length) {
        console.log(`⚠️ ${trips.length - filtered.length} sefer filtrelendi`)
      }
    }
    
    return filtered
  }, [trips, cities, tripFromCityFilter, tripToCityFilter, tripFromCityIDFilter, tripToCityIDFilter, tripStatusFilter, tripDateFilter, tripDateFromFilter, tripDateToFilter])

  return (
    <div className="company-panel">
      <div className="container">
        <div className="company-header">
          <h1 className="page-title">
            <span className="page-title-emoji">🏢</span>
            Şirket Panel
          </h1>
          <p className="page-subtitle">Seferlerinizi yönetin ve takip edin</p>
        </div>

        <div className="company-tabs">
          <button
            className={`tab-btn ${activeTab === 'trips' ? 'active' : ''}`}
            onClick={() => setActiveTab('trips')}
          >
            🚌 Seferlerim
          </button>
          <button
            className={`tab-btn ${activeTab === 'stats' ? 'active' : ''}`}
            onClick={() => setActiveTab('stats')}
          >
            📊 İstatistikler
          </button>
        </div>

        <div className="company-content">
          {activeTab === 'trips' && (
            <>
              <div className="content-header">
                <h2>Seferlerim</h2>
                <button
                  className="btn btn-primary"
                  onClick={() => {
                    setShowTripForm(true)
                    setEditingTrip(null)
                    resetForm()
                  }}
                >
                  + Yeni Sefer Ekle
                </button>
              </div>

              {showTripForm && (
                <div className="trip-form-card card">
                  <h3>{editingTrip ? 'Sefer Düzenle' : 'Yeni Sefer Ekle'}</h3>
                  <form onSubmit={handleSubmit}>
                    <div className="form-grid">
                      <div className="form-group">
                        <label>Araç Türü</label>
                        <select
                          value={formData.VehicleType}
                          onChange={(e) => {
                            setFormData({ ...formData, VehicleType: e.target.value, VehicleID: '' })
                          }}
                          required
                        >
                          <option value="Bus">Otobüs</option>
                          <option value="Train">Tren</option>
                        </select>
                      </div>
                      <div className="form-group">
                        <label>Araç</label>
                        <select
                          value={formData.VehicleID}
                          onChange={(e) => setFormData({ ...formData, VehicleID: e.target.value })}
                          required
                          disabled={!formData.VehicleType || vehicles.length === 0}
                        >
                          <option value="">Araç Seçin</option>
                          {vehicles.map(vehicle => (
                            <option key={vehicle.VehicleID} value={vehicle.VehicleID}>
                              {vehicle.PlateOrCode || `Araç #${vehicle.VehicleID}`} ({vehicle.SeatCount} koltuk)
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="form-group">
                        <label>Nereden (Şehir)</label>
                        <select
                          value={formData.FromCityID}
                          onChange={(e) => {
                            setFormData({ 
                              ...formData, 
                              FromCityID: e.target.value,
                              DepartureTerminalID: '',
                              DepartureStationID: ''
                            })
                          }}
                          required
                        >
                          <option value="">Şehir Seçin</option>
                          {cities.map(city => (
                            <option key={city.CityID} value={city.CityID}>
                              {city.CityName}
                            </option>
                          ))}
                        </select>
                      </div>
                      {formData.VehicleType === 'Bus' && formData.FromCityID && (
                        <div className="form-group">
                          <label>Kalkış Terminali</label>
                          <select
                            value={formData.DepartureTerminalID}
                            onChange={(e) => setFormData({ ...formData, DepartureTerminalID: e.target.value })}
                          >
                            <option value="">Terminal Seçin (Opsiyonel)</option>
                            {(terminals.from || []).map(terminal => (
                              <option key={terminal.TerminalID ?? terminal.terminalID} value={terminal.TerminalID ?? terminal.terminalID}>
                                {terminal.TerminalName ?? terminal.terminalName}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}
                      {formData.VehicleType === 'Train' && formData.FromCityID && (
                        <div className="form-group">
                          <label>Kalkış İstasyonu</label>
                          <select
                            value={formData.DepartureStationID}
                            onChange={(e) => setFormData({ ...formData, DepartureStationID: e.target.value })}
                          >
                            <option value="">İstasyon Seçin (Opsiyonel)</option>
                            {(stations.from || []).map(station => (
                              <option key={station.StationID ?? station.stationID} value={station.StationID ?? station.stationID}>
                                {station.StationName ?? station.stationName}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}
                      <div className="form-group">
                        <label>Nereye (Şehir)</label>
                        <select
                          value={formData.ToCityID}
                          onChange={(e) => {
                            setFormData({ 
                              ...formData, 
                              ToCityID: e.target.value,
                              ArrivalTerminalID: '',
                              ArrivalStationID: ''
                            })
                          }}
                          required
                        >
                          <option value="">Şehir Seçin</option>
                          {cities.map(city => (
                            <option key={city.CityID} value={city.CityID}>
                              {city.CityName}
                            </option>
                          ))}
                        </select>
                      </div>
                      {formData.VehicleType === 'Bus' && formData.ToCityID && (
                        <div className="form-group">
                          <label>Varış Terminali</label>
                          <select
                            value={formData.ArrivalTerminalID}
                            onChange={(e) => setFormData({ ...formData, ArrivalTerminalID: e.target.value })}
                          >
                            <option value="">Terminal Seçin (Opsiyonel)</option>
                            {(terminals.to || []).map(terminal => (
                              <option key={terminal.TerminalID ?? terminal.terminalID} value={terminal.TerminalID ?? terminal.terminalID}>
                                {terminal.TerminalName ?? terminal.terminalName}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}
                      {formData.VehicleType === 'Train' && formData.ToCityID && (
                        <div className="form-group">
                          <label>Varış İstasyonu</label>
                          <select
                            value={formData.ArrivalStationID}
                            onChange={(e) => setFormData({ ...formData, ArrivalStationID: e.target.value })}
                          >
                            <option value="">İstasyon Seçin (Opsiyonel)</option>
                            {(stations.to || []).map(station => (
                              <option key={station.StationID ?? station.stationID} value={station.StationID ?? station.stationID}>
                                {station.StationName ?? station.stationName}
                              </option>
                            ))}
                          </select>
                        </div>
                      )}
                      <div className="form-group">
                        <label>Kalkış Tarihi</label>
                        <input
                          type="date"
                          value={formData.DepartureDate}
                          onChange={(e) => setFormData({ ...formData, DepartureDate: e.target.value })}
                          required
                          min={new Date().toISOString().split('T')[0]}
                        />
                      </div>
                      <div className="form-group">
                        <label>Kalkış Saati</label>
                        <input
                          type="time"
                          value={formData.DepartureTime}
                          onChange={(e) => setFormData({ ...formData, DepartureTime: e.target.value })}
                          required
                        />
                      </div>
                      <div className="form-group">
                        <label>Varış Tarihi</label>
                        <input
                          type="date"
                          value={formData.ArrivalDate}
                          onChange={(e) => setFormData({ ...formData, ArrivalDate: e.target.value })}
                          min={formData.DepartureDate || new Date().toISOString().split('T')[0]}
                        />
                      </div>
                      <div className="form-group">
                        <label>Varış Saati</label>
                        <input
                          type="time"
                          value={formData.ArrivalTime}
                          onChange={(e) => setFormData({ ...formData, ArrivalTime: e.target.value })}
                        />
                      </div>
                      <div className="form-group">
                        <label>Fiyat (₺)</label>
                        <input
                          type="number"
                          step="0.01"
                          min="0"
                          value={formData.Price}
                          onChange={(e) => setFormData({ ...formData, Price: e.target.value })}
                          required
                        />
                      </div>
                    </div>
                    <div className="form-actions">
                      <button type="submit" className="btn btn-primary" disabled={loading}>
                        {loading ? 'Kaydediliyor...' : editingTrip ? 'Güncelle' : 'Oluştur'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => {
                          setShowTripForm(false)
                          setEditingTrip(null)
                          resetForm()
                        }}
                      >
                        İptal
                      </button>
                    </div>
                  </form>
                </div>
              )}

              {/* Filtreleme */}
              {trips.length > 0 && (
                <div className="table-filters">
                  <div className="filter-group">
                    <div className="select-wrapper">
                      <span className="filter-icon">🚌</span>
                      <select
                        value={tripFromCityIDFilter}
                        onChange={(e) => {
                          setTripFromCityIDFilter(e.target.value)
                          setTripFromCityFilter('')
                        }}
                        className="filter-select"
                      >
                        <option value="">Nereden (Tümü)</option>
                        {cities.map(city => (
                          <option key={city.CityID} value={city.CityID}>
                            {city.CityName}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="select-wrapper">
                      <span className="filter-icon">📍</span>
                      <select
                        value={tripToCityIDFilter}
                        onChange={(e) => {
                          setTripToCityIDFilter(e.target.value)
                          setTripToCityFilter('')
                        }}
                        className="filter-select"
                      >
                        <option value="">Nereye (Tümü)</option>
                        {cities.map(city => (
                          <option key={city.CityID} value={city.CityID}>
                            {city.CityName}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="select-wrapper">
                      <span className="filter-icon">📊</span>
                      <select
                        value={tripStatusFilter}
                        onChange={(e) => setTripStatusFilter(e.target.value)}
                        className="filter-select"
                      >
                        <option value="">Tüm Durumlar</option>
                        <option value="Aktif">Aktif</option>
                        <option value="İptal">İptal</option>
                      </select>
                    </div>
                    <div className="search-wrapper">
                      <span className="filter-icon">📅</span>
                      <input
                        type="date"
                        placeholder="Belirli Tarih"
                        value={tripDateFilter}
                        onChange={(e) => {
                          setTripDateFilter(e.target.value)
                          setTripDateFromFilter('')
                          setTripDateToFilter('')
                        }}
                        className="search-input"
                        title="Belirli bir tarihteki seferleri göster"
                      />
                    </div>
                    <div className="search-wrapper">
                      <span className="filter-icon">📅</span>
                      <input
                        type="date"
                        placeholder="Başlangıç Tarihi"
                        value={tripDateFromFilter}
                        onChange={(e) => {
                          setTripDateFromFilter(e.target.value)
                          setTripDateFilter('')
                        }}
                        className="search-input"
                      />
                    </div>
                    <div className="search-wrapper">
                      <span className="filter-icon">📅</span>
                      <input
                        type="date"
                        placeholder="Bitiş Tarihi"
                        value={tripDateToFilter}
                        onChange={(e) => {
                          setTripDateToFilter(e.target.value)
                          setTripDateFilter('')
                        }}
                        className="search-input"
                      />
                    </div>
                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                      <button
                        className={`btn btn-outline ${viewMode === 'table' ? 'active' : ''}`}
                        onClick={() => setViewMode('table')}
                        style={{ minWidth: 'auto', padding: '8px 12px' }}
                        title="Tablo Görünümü"
                      >
                        📋
                      </button>
                      <button
                        className={`btn btn-outline ${viewMode === 'card' ? 'active' : ''}`}
                        onClick={() => setViewMode('card')}
                        style={{ minWidth: 'auto', padding: '8px 12px' }}
                        title="Kart Görünümü"
                      >
                        🎴
                      </button>
                    </div>
                    {(tripFromCityIDFilter || tripToCityIDFilter || tripStatusFilter || tripDateFilter || tripDateFromFilter || tripDateToFilter) && (
                      <button
                        className="btn btn-outline"
                        onClick={() => {
                          setTripFromCityIDFilter('')
                          setTripToCityIDFilter('')
                          setTripFromCityFilter('')
                          setTripToCityFilter('')
                          setTripStatusFilter('')
                          setTripDateFilter('')
                          setTripDateFromFilter('')
                          setTripDateToFilter('')
                        }}
                        style={{ minWidth: 'auto', padding: '8px 16px' }}
                      >
                        ✕ Temizle
                      </button>
                    )}
                  </div>
                </div>
              )}
              
              {loading && !showTripForm ? (
                <div className="card">
                  <p className="info-text">Yükleniyor...</p>
                </div>
              ) : filteredTrips.length === 0 ? (
                <div className="card empty-state">
                  <div className="empty-icon">🚌</div>
                  <h2>{trips.length === 0 ? 'Henüz seferiniz yok' : 'Filtreye uygun sefer bulunamadı'}</h2>
                  <p className="info-text">
                    {trips.length === 0 
                      ? 'Yeni sefer eklemek için yukarıdaki butona tıklayın.'
                      : 'Filtreleri temizleyip tekrar deneyin.'}
                  </p>
                </div>
              ) : viewMode === 'table' ? (
                <div className="table-container">
                  <table className="admin-table">
                    <thead>
                      <tr>
                        <th>ID</th>
                        <th>Güzergah</th>
                        <th>Araç</th>
                        <th>Tarih</th>
                        <th>Saat</th>
                        <th>Fiyat</th>
                        <th>Koltuk</th>
                        <th>Durum</th>
                        <th>İşlemler</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredTrips.slice(0, tripsDisplayLimit).map((trip) => (
                        <tr key={trip.TripID}>
                          <td>{trip.TripID}</td>
                          <td>{trip.Guzergah || `${trip.FromCity} > ${trip.ToCity}`}</td>
                          <td>{trip.AracPlaka || '-'}</td>
                          <td>{trip.Tarih ? formatDate(trip.Tarih) : '-'}</td>
                          <td>{trip.Saat ? formatTime(trip.Saat) : '-'}</td>
                          <td>{formatCurrency(trip.Price)} ₺</td>
                          <td>{trip.DoluKoltukSayisi} / {trip.ToplamKoltuk}</td>
                          <td>
                            <span className={`status-badge ${trip.Durum === 'Aktif' ? 'active' : 'inactive'}`}>
                              {trip.Durum || 'Bilinmiyor'}
                            </span>
                          </td>
                          <td>
                            <div className="action-buttons">
                              {trip.Durum === 'Aktif' && (
                                <button
                                  className="btn-sm btn-outline"
                                  onClick={() => handleCancel(trip)}
                                  title="İptal Et"
                                >
                                  ⏸️
                                </button>
                              )}
                              <button
                                className="btn-sm btn-outline"
                                onClick={() => handleEdit(trip)}
                                title="Düzenle"
                              >
                                ✏️
                              </button>
                              <button
                                className="btn-sm btn-danger"
                                onClick={() => handleDelete(trip.TripID)}
                                title="Sil"
                              >
                                🗑️
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="trips-list">
                  {filteredTrips.slice(0, tripsDisplayLimit).map((trip) => (
                    <div key={trip.TripID} className="card trip-card">
                      <div className="trip-header">
                        <div>
                          <h3>
                            {trip.VehicleType === 'Train' ? '🚄' : '🚌'} 
                            {trip.Guzergah || `${trip.FromCity || 'Nereden'} → ${trip.ToCity || 'Nereye'}`}
                          </h3>
                          <p className="trip-date">
                            {trip.Tarih ? formatDate(trip.Tarih) : '-'} {trip.Saat ? formatTime(trip.Saat) : ''}
                          </p>
                          {trip.AracPlaka && (
                            <p className="trip-terminal">Araç: {trip.AracPlaka}</p>
                          )}
                        </div>
                        <div className="trip-status">
                          <span className={`status-badge ${trip.Durum === 'Aktif' ? 'active' : 'inactive'}`}>
                            {trip.Durum || 'Bilinmiyor'}
                          </span>
                        </div>
                      </div>
                      <div className="trip-details">
                        <div className="detail-row">
                          <span className="detail-label">Fiyat:</span>
                          <span className="detail-value price">{formatCurrency(trip.Price)} ₺</span>
                        </div>
                        {trip.AracPlaka && (
                          <div className="detail-row">
                            <span className="detail-label">Araç Plaka:</span>
                            <span className="detail-value">{trip.AracPlaka}</span>
                          </div>
                        )}
                        {trip.DoluKoltukSayisi !== undefined && trip.ToplamKoltuk !== undefined && (
                          <div className="detail-row">
                            <span className="detail-label">Koltuk Durumu:</span>
                            <span className="detail-value">
                              {trip.DoluKoltukSayisi} / {trip.ToplamKoltuk} Dolu
                            </span>
                          </div>
                        )}
                      </div>
                      <div className="trip-actions">
                        {trip.Durum === 'Aktif' && (
                          <button
                            className="btn btn-outline btn-warning"
                            onClick={() => handleCancel(trip)}
                          >
                            İptal Et
                          </button>
                        )}
                        <button
                          className="btn btn-outline"
                          onClick={() => handleEdit(trip)}
                        >
                          Düzenle
                        </button>
                        <button
                          className="btn btn-danger"
                          onClick={() => handleDelete(trip.TripID)}
                        >
                          Sil
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}

          {activeTab === 'stats' && (
            <div className="stats-container">
              {loading ? (
                <div className="loading-state">
                  <div className="spinner"></div>
                  <p>İstatistikler yükleniyor...</p>
                </div>
              ) : stats ? (
                <>
                  {/* Şirket Bilgileri */}
                  {stats.SirketAdi && (
                    <div className="company-info-card">
                      <h2>🏢 {stats.SirketAdi}</h2>
                      {stats.SirketEmail && <p className="company-email">{stats.SirketEmail}</p>}
                    </div>
                  )}

                  {/* Sefer İstatistikleri */}
                  <div className="stats-section">
                    <h3 className="stats-section-title">📊 Sefer İstatistikleri</h3>
                    <div className="stats-grid">
                      <div className="stat-card">
                        <div className="stat-icon">🚌</div>
                        <div className="stat-info">
                          <h3>{stats.TotalTrips || 0}</h3>
                          <p>Toplam Sefer</p>
                          <span className="stat-sub">{stats.ActiveTrips || 0} Aktif</span>
                          {stats.IptalSefer > 0 && (
                            <span className="stat-sub warning">{stats.IptalSefer} İptal</span>
                          )}
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">✅</div>
                        <div className="stat-info">
                          <h3>{stats.ActiveTrips || 0}</h3>
                          <p>Aktif Sefer</p>
                          <span className="stat-sub">
                            {stats.TotalTrips > 0 
                              ? Math.round((stats.ActiveTrips / stats.TotalTrips) * 100) 
                              : 0}% Oran
                          </span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">📅</div>
                        <div className="stat-info">
                          <h3>{stats.BuAyEklenenSefer || 0}</h3>
                          <p>Bu Ay Eklenen</p>
                          <span className="stat-sub">Yeni seferler</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Rezervasyon İstatistikleri */}
                  <div className="stats-section">
                    <h3 className="stats-section-title">🎫 Rezervasyon İstatistikleri</h3>
                    <div className="stats-grid">
                      <div className="stat-card">
                        <div className="stat-icon">🎫</div>
                        <div className="stat-info">
                          <h3>{stats.TotalReservations || 0}</h3>
                          <p>Toplam Rezervasyon</p>
                          <span className="stat-sub">{stats.ActiveReservations || 0} Aktif</span>
                          {stats.IptalRezervasyon > 0 && (
                            <span className="stat-sub warning">{stats.IptalRezervasyon} İptal</span>
                          )}
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">✅</div>
                        <div className="stat-info">
                          <h3>{stats.ActiveReservations || 0}</h3>
                          <p>Aktif Rezervasyon</p>
                          <span className="stat-sub">
                            {stats.TotalReservations > 0 
                              ? Math.round((stats.ActiveReservations / stats.TotalReservations) * 100) 
                              : 0}% Oran
                          </span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">📊</div>
                        <div className="stat-info">
                          <h3>{stats.OrtalamaDoluKoltukOrani ? stats.OrtalamaDoluKoltukOrani.toFixed(1) : 0}%</h3>
                          <p>Dolu Koltuk Oranı</p>
                          <span className="stat-sub">Ortalama doluluk</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Gelir İstatistikleri */}
                  <div className="stats-section">
                    <h3 className="stats-section-title">💰 Gelir İstatistikleri</h3>
                    <div className="stats-grid">
                      <div className="stat-card highlight">
                        <div className="stat-icon">💵</div>
                        <div className="stat-info">
                          <h3>{stats.ToplamGelir ? stats.ToplamGelir.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '0,00'} ₺</h3>
                          <p>Toplam Gelir</p>
                          <span className="stat-sub">Tüm zamanlar</span>
                        </div>
                      </div>
                      <div className="stat-card highlight">
                        <div className="stat-icon">📈</div>
                        <div className="stat-info">
                          <h3>{stats.SonBirAyGelir ? stats.SonBirAyGelir.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '0,00'} ₺</h3>
                          <p>Son Bir Ay</p>
                          <span className="stat-sub">Aylık gelir</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* Araç İstatistikleri */}
                  <div className="stats-section">
                    <h3 className="stats-section-title">🚗 Araç İstatistikleri</h3>
                    <div className="stats-grid">
                      <div className="stat-card">
                        <div className="stat-icon">🚗</div>
                        <div className="stat-info">
                          <h3>{stats.ToplamArac || 0}</h3>
                          <p>Toplam Araç</p>
                          <span className="stat-sub">Aktif araçlar</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">🚌</div>
                        <div className="stat-info">
                          <h3>{stats.OtobusSayisi || 0}</h3>
                          <p>Otobüs</p>
                          <span className="stat-sub">Otobüs sayısı</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">🚂</div>
                        <div className="stat-info">
                          <h3>{stats.TrenSayisi || 0}</h3>
                          <p>Tren</p>
                          <span className="stat-sub">Tren sayısı</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </>
              ) : (
                <div className="card empty-state">
                  <div className="empty-icon">📊</div>
                  <h2>İstatistikler yüklenemedi</h2>
                  <p className="info-text">Lütfen sayfayı yenileyin.</p>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
      {/* İptal Modal */}
      {showCancelModal && selectedTripForCancel && (
        <div className="modal-overlay" onClick={() => setShowCancelModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Sefer İptal Et</h3>
            <p>
              <strong>Sefer #{selectedTripForCancel.TripID}</strong> - {selectedTripForCancel.Guzergah || `${selectedTripForCancel.FromCity} > ${selectedTripForCancel.ToCity}`}
            </p>
            <p style={{ marginBottom: '16px', color: 'var(--text-secondary)' }}>
              Bu seferi iptal etmek istediğinize emin misiniz?
            </p>
            <div className="form-group">
              <label>İptal Nedeni (Opsiyonel)</label>
              <textarea
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="İptal nedeni..."
                rows="3"
              />
            </div>
            <div className="modal-actions">
              <button className="btn btn-primary" onClick={confirmCancel}>
                İptal Et
              </button>
              <button 
                className="btn btn-secondary" 
                onClick={() => {
                  setShowCancelModal(false)
                  setSelectedTripForCancel(null)
                  setCancelReason('')
                }}
              >
                Vazgeç
              </button>
            </div>
          </div>
        </div>
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

export default CompanyPanel
