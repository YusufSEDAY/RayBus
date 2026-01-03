import { useState, useEffect, useMemo } from 'react'
import { adminAPI, cityAPI, autoCancellationAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './AdminPanel.css'

const AdminPanel = () => {
  const [activeTab, setActiveTab] = useState('dashboard')
  const [stats, setStats] = useState(null)
  const [users, setUsers] = useState([])
  const [reservations, setReservations] = useState([])
  const [trips, setTrips] = useState([])
  const [loading, setLoading] = useState(false)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })
  
  // Kullanıcı arama ve filtreleme
  const [userSearchText, setUserSearchText] = useState('')
  const [userRoleFilter, setUserRoleFilter] = useState(null) // null = tümü, 2 = Müşteri, 3 = Şirket
  const [showUserStatusModal, setShowUserStatusModal] = useState(false)
  const [showUserEditModal, setShowUserEditModal] = useState(false)
  const [selectedUser, setSelectedUser] = useState(null)
  const [statusReason, setStatusReason] = useState('')
  const [userEditData, setUserEditData] = useState({ FullName: '', Email: '', Phone: '' })
  
  // Sefer filtreleme
  const [tripFromCityFilter, setTripFromCityFilter] = useState('') // Şehir ID veya text arama
  const [tripToCityFilter, setTripToCityFilter] = useState('') // Şehir ID veya text arama
  const [tripFromCityIDFilter, setTripFromCityIDFilter] = useState('') // Dropdown için şehir ID
  const [tripToCityIDFilter, setTripToCityIDFilter] = useState('') // Dropdown için şehir ID
  const [tripStatusFilter, setTripStatusFilter] = useState('') // '' = Tümü, '1' = Aktif, '0' = İptal
  const [tripVehicleTypeFilter, setTripVehicleTypeFilter] = useState('') // '' = Tümü, 'Bus', 'Train'
  const [tripDateFromFilter, setTripDateFromFilter] = useState('')
  const [tripDateToFilter, setTripDateToFilter] = useState('')
  const [tripDateFilter, setTripDateFilter] = useState('') // Tek tarih filtresi
  
  // Rezervasyon filtreleme
  const [reservationSearchText, setReservationSearchText] = useState('')
  const [reservationStatusFilter, setReservationStatusFilter] = useState('') // '' = Tümü, 'Reserved', 'Cancelled', 'Completed'
  const [reservationPaymentStatusFilter, setReservationPaymentStatusFilter] = useState('') // '' = Tümü, 'Pending', 'Paid', 'Refunded'
  const [reservationDateFromFilter, setReservationDateFromFilter] = useState('')
  const [reservationDateToFilter, setReservationDateToFilter] = useState('')
  
  // Araç filtreleme
  const [vehicleSearchText, setVehicleSearchText] = useState('')
  const [vehicleTypeFilter, setVehicleTypeFilter] = useState('') // '' = Tümü, 'Bus', 'Train'
  const [vehicleStatusFilter, setVehicleStatusFilter] = useState('') // '' = Tümü, 'true' = Aktif, 'false' = Pasif
  const [vehicleCompanyFilter, setVehicleCompanyFilter] = useState('') // Şirket adı ile filtreleme
  
  // Araç ekleme/güncelleme
  const [showVehicleForm, setShowVehicleForm] = useState(false)
  const [editingVehicle, setEditingVehicle] = useState(null)
  const [vehicleFormData, setVehicleFormData] = useState({
    PlakaNo: '',
    AracTipi: 'Bus',
    ToplamKoltuk: '',
    SirketID: null
  })
  
  // Sefer ekleme/güncelleme
  const [showTripForm, setShowTripForm] = useState(false)
  const [editingTrip, setEditingTrip] = useState(null)
  const [tripFormData, setTripFormData] = useState({
    NeredenID: '',
    NereyeID: '',
    AracID: '',
    Tarih: '',
    Saat: '',
    Fiyat: '',
    DepartureTerminalID: '',
    ArrivalTerminalID: '',
    DepartureStationID: '',
    ArrivalStationID: '',
    ArrivalDate: '',
    ArrivalTime: ''
  })
  const [cities, setCities] = useState([])
  const [vehicles, setVehicles] = useState([])
  const [companies, setCompanies] = useState([])
  const [selectedVehicleType, setSelectedVehicleType] = useState('')
  const [selectedCompanyID, setSelectedCompanyID] = useState('')
  const [departureTerminals, setDepartureTerminals] = useState([])
  const [arrivalTerminals, setArrivalTerminals] = useState([])
  const [departureStations, setDepartureStations] = useState([])
  const [arrivalStations, setArrivalStations] = useState([])
  const [showCancelTripModal, setShowCancelTripModal] = useState(false)
  const [selectedTrip, setSelectedTrip] = useState(null)
  const [cancelReason, setCancelReason] = useState('')
  
  // Pagination state'leri (her sekme için ayrı)
  const [usersDisplayLimit, setUsersDisplayLimit] = useState(100)
  const [tripsDisplayLimit, setTripsDisplayLimit] = useState(100)
  const [reservationsDisplayLimit, setReservationsDisplayLimit] = useState(100)
  const [vehiclesDisplayLimit, setVehiclesDisplayLimit] = useState(100)
  const [tripDetailsDisplayLimit, setTripDetailsDisplayLimit] = useState(100)
  
  // Otomatik İptal state'leri
  const [autoCancellationSettings, setAutoCancellationSettings] = useState(null)
  const [autoCancellationLogs, setAutoCancellationLogs] = useState([])
  const [timeoutMinutes, setTimeoutMinutes] = useState(15)
  const [processingCancellation, setProcessingCancellation] = useState(false)
  
  // Finansal Raporlar state'leri
  const [routeRevenueReport, setRouteRevenueReport] = useState([])
  const [loadingRouteRevenue, setLoadingRouteRevenue] = useState(false)
  
  // Sefer Detayları state'leri
  const [tripDetails, setTripDetails] = useState([])
  const [loadingTripDetails, setLoadingTripDetails] = useState(false)
  const [selectedTripIdForDetails, setSelectedTripIdForDetails] = useState('')
  
  // Filtrelenmiş veriler (useMemo ile performans optimizasyonu)
  const filteredTrips = useMemo(() => {
    return trips.filter(trip => {
      // Şehir ID filtresi (dropdown'dan seçilen)
      if (tripFromCityIDFilter) {
        const fromCity = cities.find(c => c.CityID === parseInt(tripFromCityIDFilter))
        if (fromCity && trip.FromCity !== fromCity.CityName) return false
      }
      if (tripToCityIDFilter) {
        const toCity = cities.find(c => c.CityID === parseInt(tripToCityIDFilter))
        if (toCity && trip.ToCity !== toCity.CityName) return false
      }
      
      // Text arama filtresi (manuel yazılan)
      if (tripFromCityFilter && trip.FromCity?.toLowerCase().includes(tripFromCityFilter.toLowerCase()) === false) return false
      if (tripToCityFilter && trip.ToCity?.toLowerCase().includes(tripToCityFilter.toLowerCase()) === false) return false
      
      // Durum filtresi
      if (tripStatusFilter !== '' && trip.Status?.toString() !== tripStatusFilter) return false
      
      // Araç tipi filtresi
      if (tripVehicleTypeFilter && trip.VehicleType !== tripVehicleTypeFilter) return false
      
      // Tarih aralığı filtresi
      if (tripDateFromFilter) {
        const tripDate = new Date(trip.DepartureDate)
        const fromDate = new Date(tripDateFromFilter)
        if (tripDate < fromDate) return false
      }
      if (tripDateToFilter) {
        const tripDate = new Date(trip.DepartureDate)
        const toDate = new Date(tripDateToFilter)
        toDate.setHours(23, 59, 59, 999) // Günün sonuna kadar
        if (tripDate > toDate) return false
      }
      
      // Tek tarih filtresi (ekstra)
      if (tripDateFilter) {
        const tripDate = new Date(trip.DepartureDate)
        const filterDate = new Date(tripDateFilter)
        const tripDateStr = tripDate.toISOString().split('T')[0]
        const filterDateStr = filterDate.toISOString().split('T')[0]
        if (tripDateStr !== filterDateStr) return false
      }
      
      return true
    })
  }, [trips, cities, tripFromCityFilter, tripToCityFilter, tripFromCityIDFilter, tripToCityIDFilter, tripStatusFilter, tripVehicleTypeFilter, tripDateFromFilter, tripDateToFilter, tripDateFilter])
  
  const filteredReservations = useMemo(() => {
    return reservations.filter(reservation => {
      if (reservationSearchText && !reservation.UserName?.toLowerCase().includes(reservationSearchText.toLowerCase())) return false
      if (reservationStatusFilter && reservation.Status !== reservationStatusFilter) return false
      if (reservationPaymentStatusFilter && reservation.PaymentStatus !== reservationPaymentStatusFilter) return false
      if (reservationDateFromFilter) {
        const resDate = new Date(reservation.ReservationDate)
        const fromDate = new Date(reservationDateFromFilter)
        if (resDate < fromDate) return false
      }
      if (reservationDateToFilter) {
        const resDate = new Date(reservation.ReservationDate)
        const toDate = new Date(reservationDateToFilter)
        toDate.setHours(23, 59, 59, 999)
        if (resDate > toDate) return false
      }
      return true
    })
  }, [reservations, reservationSearchText, reservationStatusFilter, reservationPaymentStatusFilter, reservationDateFromFilter, reservationDateToFilter])
  
  const filteredVehicles = useMemo(() => {
    return vehicles.filter(vehicle => {
      if (vehicleSearchText && !vehicle.PlateOrCode?.toLowerCase().includes(vehicleSearchText.toLowerCase())) return false
      if (vehicleTypeFilter && vehicle.VehicleType !== vehicleTypeFilter) return false
      if (vehicleStatusFilter !== '' && vehicle.Active?.toString() !== vehicleStatusFilter) return false
      if (vehicleCompanyFilter && !vehicle.CompanyName?.toLowerCase().includes(vehicleCompanyFilter.toLowerCase())) return false
      return true
    })
  }, [vehicles, vehicleSearchText, vehicleTypeFilter, vehicleStatusFilter, vehicleCompanyFilter])

  // Token'ı kontrol et ve temizle (eğer geçersizse)
  useEffect(() => {
    // JWT kaldırıldı - Token kontrolü devre dışı
    // Artık token kontrolü yapılmıyor
  }, [])

  useEffect(() => {
    // Sadece tab değiştiğinde fetch yap ve pagination limit'lerini sıfırla
    if (activeTab === 'dashboard') {
      fetchStats()
    } else if (activeTab === 'users') {
      setUsersDisplayLimit(100) // Tab değiştiğinde limit'i sıfırla
      fetchUsers()
    } else if (activeTab === 'reservations') {
      setReservationsDisplayLimit(100)
      fetchReservations()
    } else if (activeTab === 'trips') {
      setTripsDisplayLimit(100)
      fetchTrips()
      fetchCities()
      fetchCompanies()
    } else if (activeTab === 'vehicles') {
      setVehiclesDisplayLimit(100)
      fetchVehicles(null, null)
    } else if (activeTab === 'auto-cancellation') {
      fetchAutoCancellationSettings()
      fetchAutoCancellationLogs()
    } else if (activeTab === 'financial-reports') {
      fetchRouteRevenueReport()
    } else if (activeTab === 'trip-details') {
      setTripDetailsDisplayLimit(100) // Tab değiştiğinde limit'i sıfırla
      fetchTripDetails()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeTab]) // userSearchText ve userRoleFilter ayrı useEffect'te yönetiliyor

  useEffect(() => {
    // Sadece users tab'ındayken ve arama/filtre değiştiğinde fetch yap
    if (activeTab === 'users') {
      const timeoutId = setTimeout(() => {
        fetchUsers()
      }, 500) // Debounce süresini artırdık (300ms -> 500ms)
      return () => clearTimeout(timeoutId)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userSearchText, userRoleFilter, activeTab])

  const showSnackbar = (message, type = 'success') => {
    setSnackbar({ isOpen: true, message, type })
  }

  // JWT kaldırıldı - Token kontrolü devre dışı
  // const isTokenValid = () => { return true } // Artık her zaman true döner

  const fetchStats = async () => {
    setLoading(true)
    try {
      const response = await adminAPI.getDashboardStats()
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        // Backend camelCase gönderiyor, frontend PascalCase bekliyor - normalize et
        const normalizedData = {
          TotalUsers: data.TotalUsers ?? data.totalUsers ?? 0,
          ActiveUsers: data.ActiveUsers ?? data.activeUsers ?? 0,
          TotalReservations: data.TotalReservations ?? data.totalReservations ?? 0,
          ActiveReservations: data.ActiveReservations ?? data.activeReservations ?? 0,
          TotalTrips: data.TotalTrips ?? data.totalTrips ?? 0,
          ActiveTrips: data.ActiveTrips ?? data.activeTrips ?? 0,
          TotalRevenue: data.TotalRevenue ?? data.totalRevenue ?? 0,
          ToplamAktifUye: data.ToplamAktifUye ?? data.toplamAktifUye ?? 0,
          GelecekSeferler: data.GelecekSeferler ?? data.gelecekSeferler ?? 0,
          GunlukCiro: data.GunlukCiro ?? data.gunlukCiro ?? 0,
          ToplamSatis: data.ToplamSatis ?? data.toplamSatis ?? 0,
          SonIslemLoglari: data.SonIslemLoglari ?? data.sonIslemLoglari ?? 0
        }
        
        if (process.env.NODE_ENV === 'development') {
          console.log('📊 Dashboard Data (normalized):', normalizedData)
        }
        
        setStats(normalizedData)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Dashboard: Success veya Data yok', { success, data })
        }
        setStats(null)
      }
    } catch (error) {
      // Detaylı hata logu
      console.error('❌ İstatistikler yüklenirken hata:', error)
      console.error('❌ Error response:', error.response?.data)
      console.error('❌ Error status:', error.response?.status)
      
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'İstatistikler yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setStats(null)
    } finally {
      setLoading(false)
    }
  }

  const fetchUsers = async () => {
    setLoading(true)
    try {
      const response = await adminAPI.getAllUsers(
        userSearchText && userSearchText.trim() ? userSearchText.trim() : null,
        userRoleFilter || null
      )
      
      // Debug: Response'u logla
      if (process.env.NODE_ENV === 'development') {
        console.log('👥 Users Response:', response.data)
      }
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et (camelCase -> PascalCase)
        const normalizedUsers = data.map(user => ({
          UserID: user.UserID ?? user.userID ?? 0,
          FullName: user.FullName ?? user.fullName ?? '',
          Email: user.Email ?? user.email ?? '',
          Phone: user.Phone ?? user.phone ?? '',
          RoleName: user.RoleName ?? user.roleName ?? '',
          Status: user.Status ?? user.status ?? 0,
          Durum: user.Durum ?? user.durum ?? user.Status ?? user.status ?? 0,
          CreatedAt: user.CreatedAt ?? user.createdAt ?? new Date(),
          KayitTarihi: user.KayitTarihi ?? user.kayitTarihi ?? user.CreatedAt ?? user.createdAt ?? new Date(),
          ToplamHarcama: user.ToplamHarcama ?? user.toplamHarcama ?? 0
        }))
        
        if (process.env.NODE_ENV === 'development' && normalizedUsers.length > 0) {
          console.log('👥 İlk kullanıcı (normalized):', normalizedUsers[0])
        }
        
        setUsers(normalizedUsers)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Users: Success veya Data yok veya array değil', { success, isArray: Array.isArray(data), data })
        }
        setUsers([])
      }
    } catch (error) {
      // Detaylı hata logu
      console.error('❌ Kullanıcılar yüklenirken hata:', error)
      console.error('❌ Error response:', error.response?.data)
      console.error('❌ Error status:', error.response?.status)
      
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Kullanıcılar yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setUsers([])
    } finally {
      setLoading(false)
    }
  }

  const fetchReservations = async () => {
    setLoading(true)
    try {
      const response = await adminAPI.getAllReservations()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et (camelCase -> PascalCase)
        const normalizedReservations = data.map(reservation => ({
          ReservationID: reservation.ReservationID ?? reservation.reservationID ?? 0,
          UserID: reservation.UserID ?? reservation.userID ?? 0,
          UserName: reservation.UserName ?? reservation.userName ?? '',
          TripID: reservation.TripID ?? reservation.tripID ?? 0,
          TripRoute: reservation.TripRoute ?? reservation.tripRoute ?? '',
          SeatID: reservation.SeatID ?? reservation.seatID ?? 0,
          SeatNo: reservation.SeatNo ?? reservation.seatNo ?? '',
          Status: reservation.Status ?? reservation.status ?? '',
          PaymentStatus: reservation.PaymentStatus ?? reservation.paymentStatus ?? '',
          ReservationDate: reservation.ReservationDate ?? reservation.reservationDate ?? new Date()
        }))
        
        if (process.env.NODE_ENV === 'development' && normalizedReservations.length > 0) {
          console.log('🎫 İlk rezervasyon (normalized):', normalizedReservations[0])
        }
        
        setReservations(normalizedReservations)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Reservations: Success veya Data yok veya array değil', { success, isArray: Array.isArray(data), data })
        }
        setReservations([])
      }
    } catch (error) {
      console.error('❌ Rezervasyonlar yüklenirken hata:', error)
      console.error('❌ Error response:', error.response?.data)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Rezervasyonlar yüklenirken bir hata oluştu'
      if (error.response?.status === 403) {
        showSnackbar('Bu işlem için yetkiniz yok. Admin rolü gereklidir.', 'error')
      } else if (error.response?.status === 401) {
        const errorDetails = error.response?.data?.Errors?.[0] || error.response?.data?.errors?.[0] || ''
        showSnackbar(`Oturum hatası: ${errorDetails || 'Lütfen çıkış yapıp tekrar giriş yapın.'}`, 'error')
      } else {
        showSnackbar(errorMessage, 'error')
      }
      setReservations([])
    } finally {
      setLoading(false)
    }
  }

  const fetchTrips = async () => {
    setLoading(true)
    try {
      const response = await adminAPI.getAllTrips()
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et (camelCase -> PascalCase)
        const normalizedTrips = data.map(trip => ({
          TripID: trip.TripID ?? trip.tripID ?? 0,
          VehicleID: trip.VehicleID ?? trip.vehicleID ?? 0,
          VehicleType: trip.VehicleType ?? trip.vehicleType ?? '',
          VehicleCompanyID: trip.VehicleCompanyID ?? trip.vehicleCompanyID ?? null,
          FromCityID: trip.FromCityID ?? trip.fromCityID ?? 0,
          FromCity: trip.FromCity ?? trip.fromCity ?? '',
          ToCityID: trip.ToCityID ?? trip.toCityID ?? 0,
          ToCity: trip.ToCity ?? trip.toCity ?? '',
          DepartureTerminalID: trip.DepartureTerminalID ?? trip.departureTerminalID ?? null,
          DepartureTerminal: trip.DepartureTerminal ?? trip.departureTerminal ?? null,
          ArrivalTerminalID: trip.ArrivalTerminalID ?? trip.arrivalTerminalID ?? null,
          ArrivalTerminal: trip.ArrivalTerminal ?? trip.arrivalTerminal ?? null,
          DepartureStationID: trip.DepartureStationID ?? trip.departureStationID ?? null,
          DepartureStation: trip.DepartureStation ?? trip.departureStation ?? null,
          ArrivalStationID: trip.ArrivalStationID ?? trip.arrivalStationID ?? null,
          ArrivalStation: trip.ArrivalStation ?? trip.arrivalStation ?? null,
          DepartureDate: trip.DepartureDate ?? trip.departureDate ?? new Date(),
          DepartureTime: trip.DepartureTime ?? trip.departureTime ?? '',
          ArrivalDate: trip.ArrivalDate ?? trip.arrivalDate ?? null,
          ArrivalTime: trip.ArrivalTime ?? trip.arrivalTime ?? null,
          Price: trip.Price ?? trip.price ?? 0,
          Status: trip.Status ?? trip.status ?? 0
        }))
        
        if (process.env.NODE_ENV === 'development' && normalizedTrips.length > 0) {
          console.log('🚌 İlk sefer (normalized):', normalizedTrips[0])
        }
        
        setTrips(normalizedTrips)
      } else {
        if (process.env.NODE_ENV === 'development') {
          console.warn('⚠️ Trips: Success veya Data yok veya array değil', { success, isArray: Array.isArray(data), data })
        }
        setTrips([])
      }
    } catch (error) {
      // Sadece kritik hataları göster (500+), normal yükleme hatalarını sessizce handle et
      if (error.response?.status && error.response.status >= 500) {
        const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Seferler yüklenirken bir hata oluştu'
        showSnackbar(errorMessage, 'error')
      }
      
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ Seferler yüklenirken hata:', error)
        console.error('❌ Error response:', error.response?.data)
        console.error('❌ Error status:', error.response?.status)
      }
      
      setTrips([])
    } finally {
      setLoading(false)
    }
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

      if (direction === 'from') {
        setDepartureTerminals(terminalsList)
        setDepartureStations(stationsList)
      } else {
        setArrivalTerminals(terminalsList)
        setArrivalStations(stationsList)
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error(`Terminal/istasyon yüklenirken hata (${direction}):`, error)
      }
    }
  }

  // Şehir seçildiğinde terminal ve station'ları yükle
  useEffect(() => {
    if (tripFormData.NeredenID) {
      fetchTerminalsAndStations(parseInt(tripFormData.NeredenID), 'from')
    } else {
      setDepartureTerminals([])
      setDepartureStations([])
    }
  }, [tripFormData.NeredenID])

  useEffect(() => {
    if (tripFormData.NereyeID) {
      fetchTerminalsAndStations(parseInt(tripFormData.NereyeID), 'to')
    } else {
      setArrivalTerminals([])
      setArrivalStations([])
    }
  }, [tripFormData.NereyeID])

  const fetchCompanies = async () => {
    try {
      const response = await adminAPI.getAllCompanies()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        const normalizedCompanies = data.map(company => ({
          CompanyID: company.CompanyID ?? company.companyID ?? 0,
          CompanyName: company.CompanyName ?? company.companyName ?? ''
        }))
        setCompanies(normalizedCompanies)
      } else {
        setCompanies([])
      }
    } catch (error) {
      console.error('❌ Şirketler yüklenirken hata:', error)
      showSnackbar('Şirketler yüklenirken bir hata oluştu', 'error')
      setCompanies([])
    }
  }

  const fetchVehicles = async (vehicleType, companyID) => {
    try {
      // Parametreleri logla (debug için)
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 fetchVehicles çağrılıyor:', { vehicleType, companyID, companyIDType: typeof companyID })
      }
      
      const response = await adminAPI.getAllVehicles(vehicleType, companyID)
      
      if (process.env.NODE_ENV === 'development') {
        console.log('🔍 fetchVehicles response:', response.data)
      }
      
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        // Backend'den gelen veriyi normalize et
        const normalizedVehicles = data.map(vehicle => ({
          VehicleID: vehicle.VehicleID ?? vehicle.vehicleID ?? 0,
          VehicleType: vehicle.VehicleType ?? vehicle.vehicleType ?? '',
          PlateOrCode: vehicle.PlateOrCode ?? vehicle.plateOrCode ?? '',
          SeatCount: vehicle.SeatCount ?? vehicle.seatCount ?? 0,
          Active: vehicle.Active ?? vehicle.active ?? true,
          CompanyID: vehicle.CompanyID ?? vehicle.companyID ?? null,
          CompanyName: vehicle.CompanyName ?? vehicle.companyName ?? null
        }))
        
        if (process.env.NODE_ENV === 'development') {
          console.log('🚗 Toplam araç sayısı:', normalizedVehicles.length)
          if (normalizedVehicles.length > 0) {
            console.log('🚗 İlk araç (normalized):', normalizedVehicles[0])
          }
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
      console.error('❌ Error status:', error.response?.status)
      console.error('❌ Error config:', error.config)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Araçlar yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setVehicles([])
    }
  }

  const handleUpdateUserStatus = (user) => {
    setSelectedUser(user)
    setStatusReason('')
    setShowUserStatusModal(true)
  }

  const confirmUpdateUserStatus = async () => {
    if (!selectedUser) return
    
    try {
      const currentStatus = selectedUser.Durum ?? selectedUser.Status ?? 0
      const newStatus = currentStatus === 1 ? 0 : 1
      await adminAPI.updateUserStatus(selectedUser.UserID, {
        Status: newStatus,
        Sebep: statusReason || null
      })
      fetchUsers()
      setShowUserStatusModal(false)
      setSelectedUser(null)
      setStatusReason('')
      showSnackbar('Kullanıcı durumu başarıyla güncellendi', 'success')
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Kullanıcı durumu güncellenirken hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Kullanıcı durumu güncellenemedi'
      showSnackbar(errorMessage, 'error')
    }
  }

  const handleEditUser = (user) => {
    setSelectedUser(user)
    setUserEditData({
      FullName: user.FullName || user.fullName || '',
      Email: user.Email || user.email || '',
      Phone: user.Phone || user.phone || ''
    })
    setShowUserEditModal(true)
  }

  const handleUpdateUser = async () => {
    if (!selectedUser) return
    
    try {
      await adminAPI.updateUser(selectedUser.UserID, userEditData)
      fetchUsers()
      setShowUserEditModal(false)
      setSelectedUser(null)
      setUserEditData({ FullName: '', Email: '', Phone: '' })
      showSnackbar('Kullanıcı bilgileri başarıyla güncellendi', 'success')
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Kullanıcı güncellenirken hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Kullanıcı güncellenemedi'
      showSnackbar(errorMessage, 'error')
    }
  }

  // Otomatik İptal fonksiyonları
  const fetchAutoCancellationSettings = async () => {
    setLoading(true)
    try {
      const response = await autoCancellationAPI.getSettings()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        setAutoCancellationSettings(data)
        setTimeoutMinutes(data.TimeoutMinutes ?? 15)
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ Otomatik iptal ayarları yüklenirken hata:', error)
      }
      showSnackbar('Ayarlar yüklenirken bir hata oluştu', 'error')
    } finally {
      setLoading(false)
    }
  }

  const fetchRouteRevenueReport = async () => {
    setLoadingRouteRevenue(true)
    try {
      const response = await adminAPI.getRouteRevenueReport()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        setRouteRevenueReport(data)
      } else {
        setRouteRevenueReport([])
      }
    } catch (error) {
      console.error('❌ Güzergah ciro raporu yüklenirken hata:', error)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Rapor yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setRouteRevenueReport([])
    } finally {
      setLoadingRouteRevenue(false)
    }
  }

  const fetchTripDetails = async (tripId = null) => {
    setLoadingTripDetails(true)
    try {
      const response = await adminAPI.getTripDetails(tripId)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data ?? []
      
      if (success && Array.isArray(data)) {
        setTripDetails(data)
      } else {
        setTripDetails([])
      }
    } catch (error) {
      console.error('❌ Sefer detayları yüklenirken hata:', error)
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? error.message ?? 'Sefer detayları yüklenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
      setTripDetails([])
    } finally {
      setLoadingTripDetails(false)
    }
  }

  const handleSearchTripDetails = () => {
    const tripId = selectedTripIdForDetails.trim() ? parseInt(selectedTripIdForDetails.trim()) : null
    fetchTripDetails(tripId)
  }

  const fetchAutoCancellationLogs = async () => {
    try {
      const response = await autoCancellationAPI.getLogs()
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        setAutoCancellationLogs(Array.isArray(data) ? data : [])
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ İptal logları yüklenirken hata:', error)
      }
    }
  }

  const handleProcessTimeoutReservations = async () => {
    if (processingCancellation) return
    
    setProcessingCancellation(true)
    try {
      const response = await autoCancellationAPI.process(timeoutMinutes)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        showSnackbar(
          `${data.IptalEdilenSayisi || 0} rezervasyon otomatik olarak iptal edildi`,
          'success'
        )
        fetchAutoCancellationLogs()
      } else {
        showSnackbar('İptal işlemi başarısız oldu', 'error')
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ Otomatik iptal işlemi sırasında hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'İptal işlemi sırasında bir hata oluştu'
      showSnackbar(errorMessage, 'error')
    } finally {
      setProcessingCancellation(false)
    }
  }

  const handleUpdateAutoCancellationSettings = async () => {
    if (timeoutMinutes < 1 || timeoutMinutes > 1440) {
      showSnackbar('Timeout süresi 1-1440 dakika arasında olmalıdır', 'error')
      return
    }
    
    setLoading(true)
    try {
      const response = await autoCancellationAPI.updateSettings(timeoutMinutes)
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      
      if (success && data) {
        setAutoCancellationSettings(data)
        showSnackbar('Ayarlar başarıyla güncellendi', 'success')
      } else {
        showSnackbar('Ayarlar güncellenemedi', 'error')
      }
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('❌ Ayarlar güncellenirken hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Ayarlar güncellenirken bir hata oluştu'
      showSnackbar(errorMessage, 'error')
    } finally {
      setLoading(false)
    }
  }

  const handleDeleteUser = async (userId) => {
    if (window.confirm('Bu kullanıcıyı silmek istediğinize emin misiniz?')) {
      try {
        await adminAPI.deleteUser(userId)
        fetchUsers()
        showSnackbar('Kullanıcı başarıyla silindi', 'success')
      } catch (error) {
        if (process.env.NODE_ENV === 'development') {
          console.error('Kullanıcı silinirken hata:', error)
        }
        const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Kullanıcı silinemedi'
        showSnackbar(errorMessage, 'error')
      }
    }
  }

  const resetVehicleForm = () => {
    setVehicleFormData({
      PlakaNo: '',
      AracTipi: 'Bus',
      ToplamKoltuk: '',
      SirketID: null
    })
    setEditingVehicle(null)
  }

  const handleEditVehicle = (vehicle) => {
    setEditingVehicle(vehicle)
    setVehicleFormData({
      PlakaNo: vehicle.PlateOrCode || vehicle.plateOrCode || '',
      AracTipi: vehicle.VehicleType || vehicle.vehicleType || 'Bus',
      ToplamKoltuk: vehicle.SeatCount || vehicle.seatCount || '',
      SirketID: vehicle.CompanyID || vehicle.companyID || null
    })
    setShowVehicleForm(true)
  }

  const handleAddVehicle = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      if (editingVehicle) {
        // Güncelleme
        await adminAPI.updateVehicle(editingVehicle.VehicleID, {
          PlateOrCode: vehicleFormData.PlakaNo,
          VehicleType: vehicleFormData.AracTipi,
          Active: true, // Varsayılan olarak aktif
          CompanyID: vehicleFormData.SirketID || null
        })
        showSnackbar('Araç başarıyla güncellendi', 'success')
      } else {
        // Ekleme
        await adminAPI.addVehicle({
          PlakaNo: vehicleFormData.PlakaNo,
          AracTipi: vehicleFormData.AracTipi,
          ToplamKoltuk: parseInt(vehicleFormData.ToplamKoltuk),
          SirketID: vehicleFormData.SirketID || null
        })
        showSnackbar('Araç başarıyla eklendi', 'success')
      }
      setShowVehicleForm(false)
      resetVehicleForm()
      fetchVehicles(null, null)
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Araç işlemi sırasında hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? (editingVehicle ? 'Araç güncellenemedi' : 'Araç eklenemedi')
      showSnackbar(errorMessage, 'error')
    } finally {
      setLoading(false)
    }
  }

  const resetTripForm = () => {
    setTripFormData({
      NeredenID: '',
      NereyeID: '',
      AracID: '',
      Tarih: '',
      Saat: '',
      Fiyat: '',
      DepartureTerminalID: '',
      ArrivalTerminalID: '',
      DepartureStationID: '',
      ArrivalStationID: '',
      ArrivalDate: '',
      ArrivalTime: ''
    })
    setDepartureTerminals([])
    setArrivalTerminals([])
    setDepartureStations([])
    setArrivalStations([])
    setEditingTrip(null)
  }

  const handleEditTrip = (trip) => {
    setEditingTrip(trip)
    // Tarih formatını düzelt (YYYY-MM-DD)
    const departureDate = trip.DepartureDate || trip.departureDate
    const formattedDate = departureDate ? (typeof departureDate === 'string' ? departureDate.split('T')[0] : new Date(departureDate).toISOString().split('T')[0]) : ''
    
    // Saat formatını düzelt (HH:mm)
    const departureTime = trip.DepartureTime || trip.departureTime
    let formattedTime = ''
    if (departureTime) {
      if (typeof departureTime === 'string') {
        formattedTime = departureTime.substring(0, 5) // "HH:mm" formatı
      } else if (departureTime.hours !== undefined) {
        // TimeSpan objesi
        formattedTime = `${String(departureTime.hours || 0).padStart(2, '0')}:${String(departureTime.minutes || 0).padStart(2, '0')}`
      } else {
        // TimeSpan string formatından parse et
        const timeStr = departureTime.toString()
        const parts = timeStr.split(':')
        if (parts.length >= 2) {
          formattedTime = `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
        }
      }
    }
    
    // Arrival date ve time
    const arrivalDate = trip.ArrivalDate || trip.arrivalDate
    const formattedArrivalDate = arrivalDate ? (typeof arrivalDate === 'string' ? arrivalDate.split('T')[0] : new Date(arrivalDate).toISOString().split('T')[0]) : ''
    
    const arrivalTime = trip.ArrivalTime || trip.arrivalTime
    let formattedArrivalTime = ''
    if (arrivalTime) {
      if (typeof arrivalTime === 'string') {
        formattedArrivalTime = arrivalTime.substring(0, 5)
      } else if (arrivalTime.hours !== undefined) {
        formattedArrivalTime = `${String(arrivalTime.hours || 0).padStart(2, '0')}:${String(arrivalTime.minutes || 0).padStart(2, '0')}`
      } else {
        const timeStr = arrivalTime.toString()
        const parts = timeStr.split(':')
        if (parts.length >= 2) {
          formattedArrivalTime = `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
        }
      }
    }

    const vehicleID = trip.VehicleID || trip.vehicleID || 0
    const vehicleType = trip.VehicleType || trip.vehicleType || ''
    const vehicleCompanyID = trip.VehicleCompanyID || trip.vehicleCompanyID || null
    
    setTripFormData({
      NeredenID: (trip.FromCityID || trip.fromCityID || 0).toString(),
      NereyeID: (trip.ToCityID || trip.toCityID || 0).toString(),
      AracID: vehicleID.toString(),
      Tarih: formattedDate,
      Saat: formattedTime,
      Fiyat: (trip.Price || trip.price || 0).toString(),
      DepartureTerminalID: trip.DepartureTerminalID || trip.departureTerminalID ? (trip.DepartureTerminalID || trip.departureTerminalID).toString() : '',
      ArrivalTerminalID: trip.ArrivalTerminalID || trip.arrivalTerminalID ? (trip.ArrivalTerminalID || trip.arrivalTerminalID).toString() : '',
      DepartureStationID: trip.DepartureStationID || trip.departureStationID ? (trip.DepartureStationID || trip.departureStationID).toString() : '',
      ArrivalStationID: trip.ArrivalStationID || trip.arrivalStationID ? (trip.ArrivalStationID || trip.arrivalStationID).toString() : '',
      ArrivalDate: formattedArrivalDate,
      ArrivalTime: formattedArrivalTime
    })
    
    // Vehicle type ve company seçimlerini ayarla
    if (vehicleType) {
      setSelectedVehicleType(vehicleType)
      const companyIDValue = vehicleCompanyID !== null && vehicleCompanyID !== undefined ? vehicleCompanyID : 0
      setSelectedCompanyID(companyIDValue.toString())
      // Araçları yükle (0 ise null olarak gönder)
      fetchVehicles(vehicleType, companyIDValue === 0 ? null : companyIDValue)
      
      // Düzenleme modunda: Eğer tren ise terminal alanlarını temizle, otobüs ise istasyon alanlarını temizle
      if (vehicleType === 'Train') {
        setTripFormData(prev => ({
          ...prev,
          DepartureTerminalID: '',
          ArrivalTerminalID: ''
        }))
      } else if (vehicleType === 'Bus') {
        setTripFormData(prev => ({
          ...prev,
          DepartureStationID: '',
          ArrivalStationID: ''
        }))
      }
    }
    
    // Terminal ve station'ları yükle
    if (trip.FromCityID || trip.fromCityID) {
      fetchTerminalsAndStations(trip.FromCityID || trip.fromCityID, 'from')
    }
    if (trip.ToCityID || trip.toCityID) {
      fetchTerminalsAndStations(trip.ToCityID || trip.toCityID, 'to')
    }
    
    setShowTripForm(true)
  }

  const handleAddTrip = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      // Tarih ve Saat'i doğru formata çevir
      if (!tripFormData.Tarih || !tripFormData.Saat) {
        showSnackbar('Lütfen tarih ve saat bilgilerini girin', 'error')
        setLoading(false)
        return
      }
      
      const departureDate = new Date(tripFormData.Tarih)
      if (isNaN(departureDate.getTime())) {
        showSnackbar('Geçersiz tarih formatı', 'error')
        setLoading(false)
        return
      }
      
      // Saat formatını kontrol et (HH:mm veya HH:mm:ss)
      const timeParts = tripFormData.Saat.split(':')
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
      
      // TimeSpan formatı: "HH:mm:ss" veya "HH:mm"
      const departureTime = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`
      
      // Arrival time formatını kontrol et (varsa)
      let arrivalTime = null
      if (tripFormData.ArrivalTime) {
        const arrivalTimeParts = tripFormData.ArrivalTime.split(':')
        if (arrivalTimeParts.length >= 2) {
          const arrivalHours = parseInt(arrivalTimeParts[0])
          const arrivalMinutes = parseInt(arrivalTimeParts[1])
          if (!isNaN(arrivalHours) && !isNaN(arrivalMinutes)) {
            arrivalTime = `${arrivalHours.toString().padStart(2, '0')}:${arrivalMinutes.toString().padStart(2, '0')}:00`
          }
        }
      }

      // Arrival date formatını kontrol et (varsa)
      let arrivalDate = null
      if (tripFormData.ArrivalDate) {
        const arrivalDateObj = new Date(tripFormData.ArrivalDate)
        if (!isNaN(arrivalDateObj.getTime())) {
          arrivalDate = arrivalDateObj.toISOString().split('T')[0]
        }
      }

      const tripData = {
        FromCityID: parseInt(tripFormData.NeredenID),
        ToCityID: parseInt(tripFormData.NereyeID),
        VehicleID: parseInt(tripFormData.AracID),
        DepartureDate: departureDate.toISOString().split('T')[0], // YYYY-MM-DD formatı
        DepartureTime: departureTime, // HH:mm:ss formatı
        Price: parseFloat(tripFormData.Fiyat),
        DepartureTerminalID: tripFormData.DepartureTerminalID ? parseInt(tripFormData.DepartureTerminalID) : null,
        ArrivalTerminalID: tripFormData.ArrivalTerminalID ? parseInt(tripFormData.ArrivalTerminalID) : null,
        DepartureStationID: tripFormData.DepartureStationID ? parseInt(tripFormData.DepartureStationID) : null,
        ArrivalStationID: tripFormData.ArrivalStationID ? parseInt(tripFormData.ArrivalStationID) : null,
        ArrivalDate: arrivalDate,
        ArrivalTime: arrivalTime
      }

      let response
      if (editingTrip) {
        // Güncelleme
        response = await adminAPI.updateTrip(editingTrip.TripID, tripData)
        const success = response?.data?.Success ?? response?.data?.success
        const message = response?.data?.Message ?? response?.data?.message
        
        if (success) {
          showSnackbar(message || '✅ Sefer başarıyla güncellendi', 'success')
          setShowTripForm(false)
          resetTripForm()
          // fetchTrips'ı sessizce çağır (hata olsa bile snackbar gösterme)
          fetchTrips().catch(() => {}) // Sessizce handle et
        } else {
          showSnackbar(message || '❌ Sefer güncellenirken bir hata oluştu', 'error')
        }
      } else {
        // Ekleme
        response = await adminAPI.addTrip(tripData)
        const success = response?.data?.Success ?? response?.data?.success
        const message = response?.data?.Message ?? response?.data?.message
        
        if (success) {
          showSnackbar(message || '✅ Sefer başarıyla eklendi', 'success')
          setShowTripForm(false)
          resetTripForm()
          // fetchTrips'ı sessizce çağır (hata olsa bile snackbar gösterme)
          fetchTrips().catch(() => {}) // Sessizce handle et
        } else {
          showSnackbar(message || '❌ Sefer eklenirken bir hata oluştu', 'error')
        }
      }
    } catch (error) {
      // Sadece gerçek hataları göster
      if (error.response?.status && error.response.status >= 400) {
        const errorMessage = error.response?.data?.Message ?? error.response?.data?.message
        if (errorMessage) {
          // Backend'den gelen hata mesajını kullanıcı dostu hale getir
          const userFriendlyMessage = errorMessage
            .replace(/Cannot insert the value NULL into column/i, 'Eksik bilgi:')
            .replace(/column does not allow nulls/i, 'Zorunlu alan boş bırakılamaz')
            .replace(/INSERT fails/i, 'Kayıt eklenemedi')
            .replace(/UPDATE fails/i, 'Güncelleme yapılamadı')
            .replace(/Seçilen araç belirtilen saat aralığında başka bir seferde görünüyor/i, '⚠️ Seçilen araç bu saatte başka bir seferde kullanılıyor')
            .replace(/Geçmiş tarihli bir sefer güncellenemez/i, '⚠️ Geçmiş tarihli seferler güncellenemez')
            .replace(/Sefer bulunamadı/i, '❌ Sefer bulunamadı')
          
          showSnackbar(userFriendlyMessage, 'error')
        } else {
          showSnackbar(editingTrip ? '❌ Sefer güncellenirken bir hata oluştu' : '❌ Sefer eklenirken bir hata oluştu', 'error')
        }
      } else if (!error.response) {
        // Network hatası
        showSnackbar('⚠️ Bağlantı hatası. Lütfen tekrar deneyin.', 'error')
      }
      
      if (process.env.NODE_ENV === 'development') {
        console.error('Sefer işlemi sırasında hata:', error)
      }
    } finally {
      setLoading(false)
    }
  }

  const handleCancelTrip = (trip) => {
    setSelectedTrip(trip)
    setCancelReason('')
    setShowCancelTripModal(true)
  }

  const confirmCancelTrip = async () => {
    if (!selectedTrip) return
    
    try {
      await adminAPI.cancelTrip(selectedTrip.TripID, {
        IptalNedeni: cancelReason || null
      })
      fetchTrips()
      setShowCancelTripModal(false)
      setSelectedTrip(null)
      setCancelReason('')
      showSnackbar('Sefer başarıyla iptal edildi', 'success')
    } catch (error) {
      if (process.env.NODE_ENV === 'development') {
        console.error('Sefer iptal edilirken hata:', error)
      }
      const errorMessage = error.response?.data?.Message ?? error.response?.data?.message ?? 'Sefer iptal edilemedi'
      showSnackbar(errorMessage, 'error')
    }
  }

  const formatDate = (date) => {
    if (!date) return ''
    return new Date(date).toLocaleDateString('tr-TR', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  const formatCurrency = (amount) => {
    if (!amount) return '0.00'
    return parseFloat(amount).toFixed(2)
  }

  return (
    <div className="admin-panel">
      <div className="container">
        <div className="admin-header">
          <h1 className="page-title">
            <span className="page-title-emoji">⚙️</span>
            Admin Panel
          </h1>
          <p className="page-subtitle">Sistem yönetimi ve kontrol paneli</p>
        </div>

        <div className="admin-tabs">
          <button
            className={`tab-btn ${activeTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => setActiveTab('dashboard')}
          >
            📊 Dashboard
          </button>
          <button
            className={`tab-btn ${activeTab === 'users' ? 'active' : ''}`}
            onClick={() => setActiveTab('users')}
          >
            👥 Kullanıcılar
          </button>
          <button
            className={`tab-btn ${activeTab === 'vehicles' ? 'active' : ''}`}
            onClick={() => setActiveTab('vehicles')}
          >
            🚌 Araçlar
          </button>
          <button
            className={`tab-btn ${activeTab === 'trips' ? 'active' : ''}`}
            onClick={() => setActiveTab('trips')}
          >
            🚌 Seferler
          </button>
          <button
            className={`tab-btn ${activeTab === 'reservations' ? 'active' : ''}`}
            onClick={() => setActiveTab('reservations')}
          >
            🎫 Rezervasyonlar
          </button>
          <button
            className={`tab-btn ${activeTab === 'auto-cancellation' ? 'active' : ''}`}
            onClick={() => setActiveTab('auto-cancellation')}
          >
            ⏰ Otomatik İptal
          </button>
          <button
            className={`tab-btn ${activeTab === 'financial-reports' ? 'active' : ''}`}
            onClick={() => setActiveTab('financial-reports')}
          >
            💰 Finansal Raporlar
          </button>
          <button
            className={`tab-btn ${activeTab === 'trip-details' ? 'active' : ''}`}
            onClick={() => setActiveTab('trip-details')}
          >
            📋 Sefer Detayları
          </button>
        </div>

        <div className="admin-content">
          {loading ? (
            <div className="card">
              <p className="info-text">Yükleniyor...</p>
            </div>
          ) : (
            <>
              {activeTab === 'dashboard' && (
                <div className="dashboard-grid">
                  {stats && (
                    <>
                      <div className="stat-card">
                        <div className="stat-icon">👥</div>
                        <div className="stat-info">
                          <h3>{stats.TotalUsers || 0}</h3>
                          <p>Toplam Kullanıcı</p>
                          <span className="stat-sub">{stats.ActiveUsers || 0} Aktif</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">✅</div>
                        <div className="stat-info">
                          <h3>{stats.ToplamAktifUye || 0}</h3>
                          <p>Aktif Üyeler</p>
                          <span className="stat-sub">View'dan</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">🎫</div>
                        <div className="stat-info">
                          <h3>{stats.TotalReservations || 0}</h3>
                          <p>Toplam Rezervasyon</p>
                          <span className="stat-sub">{stats.ActiveReservations || 0} Aktif</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">📊</div>
                        <div className="stat-info">
                          <h3>{stats.ToplamSatis || 0}</h3>
                          <p>Toplam Satış</p>
                          <span className="stat-sub">View'dan</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">🚌</div>
                        <div className="stat-info">
                          <h3>{stats.TotalTrips || 0}</h3>
                          <p>Toplam Sefer</p>
                          <span className="stat-sub">{stats.ActiveTrips || 0} Aktif</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">🔮</div>
                        <div className="stat-info">
                          <h3>{stats.GelecekSeferler || 0}</h3>
                          <p>Gelecek Seferler</p>
                          <span className="stat-sub">View'dan</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">💰</div>
                        <div className="stat-info">
                          <h3>{formatCurrency(stats.TotalRevenue)} ₺</h3>
                          <p>Toplam Gelir</p>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">📈</div>
                        <div className="stat-info">
                          <h3>{formatCurrency(stats.GunlukCiro)} ₺</h3>
                          <p>Günlük Ciro</p>
                          <span className="stat-sub">View'dan</span>
                        </div>
                      </div>
                      <div className="stat-card">
                        <div className="stat-icon">📝</div>
                        <div className="stat-info">
                          <h3>{stats.SonIslemLoglari || 0}</h3>
                          <p>Son İşlem Logları</p>
                          <span className="stat-sub">Son 24 saat</span>
                        </div>
                      </div>
                    </>
                  )}
                </div>
              )}

              {activeTab === 'users' && (
                <div className="card">
                  <div className="table-filters">
                    <div className="filter-group">
                      <div className="search-wrapper">
                        <span className="search-icon">🔍</span>
                        <input
                          type="text"
                          placeholder="İsim veya Email ile ara..."
                          value={userSearchText}
                          onChange={(e) => setUserSearchText(e.target.value)}
                          className="search-input"
                        />
                      </div>
                      <div className="select-wrapper">
                        <span className="filter-icon">🎭</span>
                        <select
                          value={userRoleFilter || ''}
                          onChange={(e) => setUserRoleFilter(e.target.value ? parseInt(e.target.value) : null)}
                          className="filter-select"
                        >
                          <option value="">Tüm Roller</option>
                          <option value="2">Müşteri</option>
                          <option value="3">Şirket</option>
                        </select>
                      </div>
                    </div>
                  </div>
                  <div className="table-container">
                    <table className="admin-table users-table">
                      <thead>
                        <tr>
                          <th className="col-id">ID</th>
                          <th className="col-name">Ad Soyad</th>
                          <th className="col-email">Email</th>
                          <th className="col-phone">Telefon</th>
                          <th className="col-role">Rol</th>
                          <th className="col-status">Durum</th>
                          <th className="col-spending">Harcama</th>
                          <th className="col-date">Kayıt</th>
                          <th className="col-actions">İşlemler</th>
                        </tr>
                      </thead>
                      <tbody>
                        {users.length === 0 ? (
                          <tr>
                            <td colSpan="9" style={{ textAlign: 'center', padding: '20px' }}>
                              Kullanıcı bulunamadı
                            </td>
                          </tr>
                        ) : (
                          <>
                            {users.slice(0, usersDisplayLimit).map((user) => (
                              <tr key={user.UserID}>
                                <td className="col-id">{user.UserID}</td>
                                <td className="col-name">{user.FullName}</td>
                                <td className="col-email">{user.Email}</td>
                                <td className="col-phone">{user.Phone || '-'}</td>
                                <td className="col-role">{user.RoleName}</td>
                                <td className="col-status">
                                  <span className={`status-badge ${(user.Durum === 1 || user.Status === 1) ? 'active' : 'inactive'}`}>
                                    {(user.Durum === 1 || user.Status === 1) ? 'Aktif' : 'Pasif'}
                                  </span>
                                </td>
                                <td className="col-spending">{formatCurrency(user.ToplamHarcama || 0)} ₺</td>
                                <td className="col-date">{formatDate(user.KayitTarihi || user.CreatedAt || new Date())}</td>
                                <td className="col-actions">
                                  <div className="action-buttons">
                                    <button
                                      className="btn-sm btn-primary"
                                      onClick={() => handleEditUser(user)}
                                      title="Düzenle"
                                    >
                                      ✏️
                                    </button>
                                    <button
                                      className="btn-sm btn-outline"
                                      onClick={() => handleUpdateUserStatus(user)}
                                      title={(user.Durum === 1 || user.Status === 1) ? 'Pasif Yap' : 'Aktif Yap'}
                                    >
                                      {(user.Durum === 1 || user.Status === 1) ? '⏸️' : '▶️'}
                                    </button>
                                    <button
                                      className="btn-sm btn-danger"
                                      onClick={() => handleDeleteUser(user.UserID)}
                                      title="Sil"
                                    >
                                      🗑️
                                    </button>
                                  </div>
                                </td>
                              </tr>
                            ))}
                          </>
                        )}
                      </tbody>
                    </table>
                  </div>
                  {users.length > usersDisplayLimit && (
                    <div style={{ textAlign: 'center', marginTop: '20px' }}>
                      <button
                        className="btn btn-outline"
                        onClick={() => setUsersDisplayLimit(usersDisplayLimit + 100)}
                      >
                        + {Math.min(100, users.length - usersDisplayLimit)} Daha Fazla Göster
                      </button>
                      <p style={{ marginTop: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        {usersDisplayLimit} / {users.length} kullanıcı gösteriliyor
                      </p>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'vehicles' && (
                <div className="card">
                  <div className="content-header">
                    <h2>Araçlar</h2>
                    <button
                      className="btn btn-primary"
                      onClick={() => {
                        setShowVehicleForm(true)
                        setEditingVehicle(null)
                        resetVehicleForm()
                      }}
                    >
                      + Yeni Araç Ekle
                    </button>
                  </div>
                  {showVehicleForm && (
                    <div className="form-card">
                      <h3>{editingVehicle ? 'Araç Düzenle' : 'Yeni Araç Ekle'}</h3>
                      <form onSubmit={handleAddVehicle}>
                        <div className="form-group">
                          <label>Plaka No</label>
                          <input
                            type="text"
                            value={vehicleFormData.PlakaNo}
                            onChange={(e) => setVehicleFormData({ ...vehicleFormData, PlakaNo: e.target.value })}
                            required
                            placeholder="34 TB 1234"
                          />
                        </div>
                        <div className="form-group">
                          <label>Araç Tipi</label>
                          <select
                            value={vehicleFormData.AracTipi}
                            onChange={(e) => setVehicleFormData({ ...vehicleFormData, AracTipi: e.target.value })}
                            required
                          >
                            <option value="Bus">Otobüs</option>
                            <option value="Train">Tren</option>
                          </select>
                        </div>
                        <div className="form-group">
                          <label>Toplam Koltuk Sayısı</label>
                          <input
                            type="number"
                            min="1"
                            value={vehicleFormData.ToplamKoltuk}
                            onChange={(e) => setVehicleFormData({ ...vehicleFormData, ToplamKoltuk: e.target.value })}
                            required
                          />
                        </div>
                        <div className="form-group">
                          <label>Şirket ID (Opsiyonel)</label>
                          <input
                            type="number"
                            value={vehicleFormData.SirketID || ''}
                            onChange={(e) => setVehicleFormData({ ...vehicleFormData, SirketID: e.target.value ? parseInt(e.target.value) : null })}
                            placeholder="Boş bırakılırsa admin aracı olur"
                          />
                        </div>
                        <div className="form-actions">
                          <button type="submit" className="btn btn-primary" disabled={loading}>
                            {loading ? (editingVehicle ? 'Güncelleniyor...' : 'Ekleniyor...') : (editingVehicle ? 'Güncelle' : 'Ekle')}
                          </button>
                          <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => {
                              setShowVehicleForm(false)
                              resetVehicleForm()
                            }}
                          >
                            İptal
                          </button>
                        </div>
                      </form>
                    </div>
                  )}
                  
                  {/* Araçlar Filtreleme */}
                  {vehicles.length > 0 && (
                    <div className="table-filters" style={{ marginTop: '20px' }}>
                      <div className="filter-group">
                        <div className="search-wrapper">
                          <span className="search-icon">🔍</span>
                          <input
                            type="text"
                            placeholder="Plaka/Kod ile ara..."
                            value={vehicleSearchText}
                            onChange={(e) => setVehicleSearchText(e.target.value)}
                            className="search-input"
                          />
                        </div>
                        <div className="select-wrapper">
                          <span className="filter-icon">🚌</span>
                          <select
                            value={vehicleTypeFilter}
                            onChange={(e) => setVehicleTypeFilter(e.target.value)}
                            className="filter-select"
                          >
                            <option value="">Tüm Araç Tipleri</option>
                            <option value="Bus">Otobüs</option>
                            <option value="Train">Tren</option>
                          </select>
                        </div>
                        <div className="select-wrapper">
                          <span className="filter-icon">📊</span>
                          <select
                            value={vehicleStatusFilter}
                            onChange={(e) => setVehicleStatusFilter(e.target.value)}
                            className="filter-select"
                          >
                            <option value="">Tüm Durumlar</option>
                            <option value="true">Aktif</option>
                            <option value="false">Pasif</option>
                          </select>
                        </div>
                        <div className="search-wrapper">
                          <span className="search-icon">🏢</span>
                          <input
                            type="text"
                            placeholder="Şirket adı ile ara..."
                            value={vehicleCompanyFilter}
                            onChange={(e) => setVehicleCompanyFilter(e.target.value)}
                            className="search-input"
                          />
                        </div>
                        {(vehicleSearchText || vehicleTypeFilter || vehicleStatusFilter || vehicleCompanyFilter) && (
                          <button
                            className="btn btn-outline"
                            onClick={() => {
                              setVehicleSearchText('')
                              setVehicleTypeFilter('')
                              setVehicleStatusFilter('')
                              setVehicleCompanyFilter('')
                            }}
                            style={{ minWidth: 'auto', padding: '8px 16px' }}
                          >
                            ✕ Temizle
                          </button>
                        )}
                      </div>
                    </div>
                  )}
                  
                  {/* Araçlar Listesi */}
                  {filteredVehicles.length > 0 && (
                    <div className="table-container" style={{ marginTop: '20px' }}>
                      <table className="admin-table">
                        <thead>
                          <tr>
                            <th>ID</th>
                            <th>Plaka/Kod</th>
                            <th>Tip</th>
                            <th>Koltuk Sayısı</th>
                            <th>Şirket</th>
                            <th>Durum</th>
                            <th>İşlemler</th>
                          </tr>
                        </thead>
                        <tbody>
                          {filteredVehicles.slice(0, vehiclesDisplayLimit).map((vehicle) => (
                            <tr key={vehicle.VehicleID}>
                              <td>{vehicle.VehicleID}</td>
                              <td>{vehicle.PlateOrCode}</td>
                              <td>
                                <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                                  {vehicle.VehicleType === 'Train' ? '🚄' : '🚌'} {vehicle.VehicleType}
                                </span>
                              </td>
                              <td>{vehicle.SeatCount || '-'}</td>
                              <td>{vehicle.CompanyName || 'Admin'}</td>
                              <td>
                                <span className={`status-badge ${vehicle.Active ? 'active' : 'inactive'}`}>
                                  {vehicle.Active ? 'Aktif' : 'Pasif'}
                                </span>
                              </td>
                              <td>
                                <div className="action-buttons">
                                  <button
                                    className="btn-sm btn-primary"
                                    onClick={() => handleEditVehicle(vehicle)}
                                    title="Düzenle"
                                  >
                                    ✏️
                                  </button>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                  {filteredVehicles.length > vehiclesDisplayLimit && (
                    <div style={{ textAlign: 'center', marginTop: '20px' }}>
                      <button
                        className="btn btn-outline"
                        onClick={() => setVehiclesDisplayLimit(vehiclesDisplayLimit + 100)}
                      >
                        + {Math.min(100, filteredVehicles.length - vehiclesDisplayLimit)} Daha Fazla Göster
                      </button>
                      <p style={{ marginTop: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        {vehiclesDisplayLimit} / {filteredVehicles.length} araç gösteriliyor {filteredVehicles.length !== vehicles.length && `(Toplam: ${vehicles.length})`}
                      </p>
                    </div>
                  )}
                  
                  {filteredVehicles.length === 0 && vehicles.length > 0 && (
                    <div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
                      <p style={{ fontSize: '18px', marginBottom: '8px' }}>🔍 Filtreye uygun araç bulunamadı</p>
                      <p style={{ fontSize: '14px' }}>Filtreleri temizleyip tekrar deneyin.</p>
                    </div>
                  )}
                  
                  {vehicles.length === 0 && !showVehicleForm && (
                    <div style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
                      <p style={{ fontSize: '18px', marginBottom: '8px' }}>📭 Henüz araç eklenmemiş</p>
                      <p style={{ fontSize: '14px' }}>Yukarıdaki "Yeni Araç Ekle" butonuna tıklayarak ilk aracınızı ekleyebilirsiniz.</p>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'trips' && (
                <div className="card">
                  <div className="content-header">
                    <h2>Seferler</h2>
                    <button
                      className="btn btn-primary"
                      onClick={() => {
                        if (companies.length === 0) {
                          fetchCompanies()
                        }
                        setShowTripForm(true)
                      }}
                    >
                      + Yeni Sefer Ekle
                    </button>
                  </div>
                  {showTripForm && (
                    <div className={`form-card ${editingTrip ? 'form-card-edit' : 'form-card-add'}`}>
                      <div className="form-header">
                        <div className="form-header-icon">
                          {editingTrip ? '✏️' : '➕'}
                        </div>
                        <div className="form-header-content">
                          <h3>{editingTrip ? 'Sefer Düzenle' : 'Yeni Sefer Ekle'}</h3>
                          <p className="form-subtitle">
                            {editingTrip 
                              ? 'Mevcut sefer bilgilerini güncelleyin' 
                              : 'Yeni bir sefer oluşturmak için bilgileri doldurun'}
                          </p>
                        </div>
                        {editingTrip && (
                          <div className="form-badge edit-badge">
                            Düzenleme Modu
                          </div>
                        )}
                      </div>
                      <form onSubmit={handleAddTrip}>
                        <div className="form-grid">
                          <div className="form-group">
                            <label>Nereden</label>
                            <select
                              value={tripFormData.NeredenID}
                              onChange={(e) => setTripFormData({ ...tripFormData, NeredenID: e.target.value })}
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
                          <div className="form-group">
                            <label>Nereye</label>
                            <select
                              value={tripFormData.NereyeID}
                              onChange={(e) => setTripFormData({ ...tripFormData, NereyeID: e.target.value })}
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
                          <div className="form-group">
                            <label>Yolculuk Türü</label>
                            <select
                              value={selectedVehicleType}
                              onChange={(e) => {
                                const newVehicleType = e.target.value
                                setSelectedVehicleType(newVehicleType)
                                setSelectedCompanyID('')
                                setTripFormData(prev => ({
                                  ...prev,
                                  AracID: '',
                                  // Yolculuk türü değiştiğinde ilgili alanları temizle
                                  DepartureTerminalID: newVehicleType === 'Train' ? '' : prev.DepartureTerminalID,
                                  ArrivalTerminalID: newVehicleType === 'Train' ? '' : prev.ArrivalTerminalID,
                                  DepartureStationID: newVehicleType === 'Bus' ? '' : prev.DepartureStationID,
                                  ArrivalStationID: newVehicleType === 'Bus' ? '' : prev.ArrivalStationID
                                }))
                                setVehicles([])
                                // Yolculuk türü seçildiğinde şirket seçilmeden araçlar yüklenmemeli
                              }}
                              required
                            >
                              <option value="">Yolculuk Türü Seçin</option>
                              <option value="Bus">Otobüs</option>
                              <option value="Train">Tren</option>
                            </select>
                          </div>
                          <div className="form-group">
                            <label>Şirket</label>
                            <select
                              value={selectedCompanyID}
                              onChange={(e) => {
                                const newCompanyID = e.target.value
                                setSelectedCompanyID(newCompanyID)
                                setTripFormData({ ...tripFormData, AracID: '' })
                                
                                // Hem yolculuk türü hem şirket seçilmişse araçları yükle
                                if (selectedVehicleType && newCompanyID) {
                                  try {
                                    // "0" değeri Admin (şirketsiz) için null olarak gönderilmeli
                                    const companyID = newCompanyID === '0' ? null : parseInt(newCompanyID)
                                    if (isNaN(companyID) && companyID !== null) {
                                      console.error('❌ Geçersiz companyID:', newCompanyID)
                                      setVehicles([])
                                      showSnackbar('Geçersiz şirket seçimi', 'error')
                                      return
                                    }
                                    fetchVehicles(selectedVehicleType, companyID)
                                  } catch (error) {
                                    console.error('❌ CompanyID parse hatası:', error)
                                    setVehicles([])
                                    showSnackbar('Şirket seçimi işlenirken hata oluştu', 'error')
                                  }
                                } else {
                                  setVehicles([])
                                }
                              }}
                              required
                              disabled={!selectedVehicleType}
                            >
                              <option value="">Şirket Seçin</option>
                              <option value="0">Admin (Şirketsiz)</option>
                              {companies.map(company => (
                                <option key={company.CompanyID} value={company.CompanyID}>
                                  {company.CompanyName}
                                </option>
                              ))}
                            </select>
                          </div>
                          <div className="form-group">
                            <label>Araç Plakası</label>
                            <select
                              value={tripFormData.AracID}
                              onChange={(e) => setTripFormData({ ...tripFormData, AracID: e.target.value })}
                              required
                              disabled={!selectedVehicleType || !selectedCompanyID}
                            >
                              <option value="">Araç Seçin</option>
                              {vehicles.map(vehicle => (
                                <option key={vehicle.VehicleID} value={vehicle.VehicleID}>
                                  {vehicle.PlateOrCode} - {vehicle.SeatCount} koltuk
                                </option>
                              ))}
                            </select>
                            {selectedVehicleType && selectedCompanyID && vehicles.length === 0 && (
                              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', marginTop: '4px' }}>
                                Bu şirket için {selectedVehicleType === 'Bus' ? 'otobüs' : 'tren'} bulunamadı.
                              </p>
                            )}
                          </div>
                          <div className="form-group">
                            <label>Tarih</label>
                            <input
                              type="date"
                              value={tripFormData.Tarih}
                              onChange={(e) => setTripFormData({ ...tripFormData, Tarih: e.target.value })}
                              required
                              min={new Date().toISOString().split('T')[0]}
                            />
                          </div>
                          <div className="form-group">
                            <label>Saat</label>
                            <input
                              type="time"
                              value={tripFormData.Saat}
                              onChange={(e) => setTripFormData({ ...tripFormData, Saat: e.target.value })}
                              required
                            />
                          </div>
                          <div className="form-group">
                            <label>Fiyat (₺)</label>
                            <input
                              type="number"
                              step="0.01"
                              min="0"
                              value={tripFormData.Fiyat}
                              onChange={(e) => setTripFormData({ ...tripFormData, Fiyat: e.target.value })}
                              required
                            />
                          </div>
                        </div>
                        <div className="form-grid">
                          {/* Terminal alanları - Sadece Otobüs için */}
                          {selectedVehicleType === 'Bus' && (
                            <>
                              <div className="form-group">
                                <label>Kalkış Terminali (Opsiyonel)</label>
                                <select
                                  value={tripFormData.DepartureTerminalID}
                                  onChange={(e) => setTripFormData({ ...tripFormData, DepartureTerminalID: e.target.value })}
                                >
                                  <option value="">Terminal Seçin</option>
                                  {departureTerminals.map(terminal => (
                                    <option key={terminal.TerminalID || terminal.terminalID} value={terminal.TerminalID || terminal.terminalID}>
                                      {terminal.TerminalName || terminal.terminalName}
                                    </option>
                                  ))}
                                </select>
                              </div>
                              <div className="form-group">
                                <label>Varış Terminali (Opsiyonel)</label>
                                <select
                                  value={tripFormData.ArrivalTerminalID}
                                  onChange={(e) => setTripFormData({ ...tripFormData, ArrivalTerminalID: e.target.value })}
                                >
                                  <option value="">Terminal Seçin</option>
                                  {arrivalTerminals.map(terminal => (
                                    <option key={terminal.TerminalID || terminal.terminalID} value={terminal.TerminalID || terminal.terminalID}>
                                      {terminal.TerminalName || terminal.terminalName}
                                    </option>
                                  ))}
                                </select>
                              </div>
                            </>
                          )}
                          {/* İstasyon alanları - Sadece Tren için */}
                          {selectedVehicleType === 'Train' && (
                            <>
                              <div className="form-group">
                                <label>Kalkış İstasyonu (Opsiyonel)</label>
                                <select
                                  value={tripFormData.DepartureStationID}
                                  onChange={(e) => setTripFormData({ ...tripFormData, DepartureStationID: e.target.value })}
                                >
                                  <option value="">İstasyon Seçin</option>
                                  {departureStations.map(station => (
                                    <option key={station.StationID || station.stationID} value={station.StationID || station.stationID}>
                                      {station.StationName || station.stationName}
                                    </option>
                                  ))}
                                </select>
                              </div>
                              <div className="form-group">
                                <label>Varış İstasyonu (Opsiyonel)</label>
                                <select
                                  value={tripFormData.ArrivalStationID}
                                  onChange={(e) => setTripFormData({ ...tripFormData, ArrivalStationID: e.target.value })}
                                >
                                  <option value="">İstasyon Seçin</option>
                                  {arrivalStations.map(station => (
                                    <option key={station.StationID || station.stationID} value={station.StationID || station.stationID}>
                                      {station.StationName || station.stationName}
                                    </option>
                                  ))}
                                </select>
                              </div>
                            </>
                          )}
                          <div className="form-group">
                            <label>Varış Tarihi (Opsiyonel)</label>
                            <input
                              type="date"
                              value={tripFormData.ArrivalDate}
                              onChange={(e) => setTripFormData({ ...tripFormData, ArrivalDate: e.target.value })}
                              min={tripFormData.Tarih || new Date().toISOString().split('T')[0]}
                            />
                          </div>
                          <div className="form-group">
                            <label>Varış Saati (Opsiyonel)</label>
                            <input
                              type="time"
                              value={tripFormData.ArrivalTime}
                              onChange={(e) => setTripFormData({ ...tripFormData, ArrivalTime: e.target.value })}
                            />
                          </div>
                        </div>
                        <div className="form-actions">
                          <button type="submit" className="btn btn-primary" disabled={loading}>
                            {loading ? (editingTrip ? 'Güncelleniyor...' : 'Ekleniyor...') : (editingTrip ? 'Güncelle' : 'Ekle')}
                          </button>
                          <button
                            type="button"
                            className="btn btn-secondary"
                            onClick={() => {
                              setShowTripForm(false)
                              resetTripForm()
                            }}
                          >
                            İptal
                          </button>
                        </div>
                      </form>
                    </div>
                  )}
                  
                  {/* Sefer Filtreleme */}
                  <div className="table-filters">
                    <div className="filter-group">
                      <div className="select-wrapper">
                        <span className="filter-icon">🚌</span>
                        <select
                          value={tripFromCityIDFilter}
                          onChange={(e) => {
                            setTripFromCityIDFilter(e.target.value)
                            setTripFromCityFilter('') // Text aramayı temizle
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
                            setTripToCityFilter('') // Text aramayı temizle
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
                      <div className="search-wrapper">
                        <span className="search-icon">🔍</span>
                        <input
                          type="text"
                          placeholder="Nereden şehir ara (manuel)..."
                          value={tripFromCityFilter}
                          onChange={(e) => {
                            setTripFromCityFilter(e.target.value)
                            setTripFromCityIDFilter('') // Dropdown'ı temizle
                          }}
                          className="search-input"
                        />
                      </div>
                      <div className="search-wrapper">
                        <span className="search-icon">🔍</span>
                        <input
                          type="text"
                          placeholder="Nereye şehir ara (manuel)..."
                          value={tripToCityFilter}
                          onChange={(e) => {
                            setTripToCityFilter(e.target.value)
                            setTripToCityIDFilter('') // Dropdown'ı temizle
                          }}
                          className="search-input"
                        />
                      </div>
                      <div className="select-wrapper">
                        <span className="filter-icon">🚌</span>
                        <select
                          value={tripVehicleTypeFilter}
                          onChange={(e) => setTripVehicleTypeFilter(e.target.value)}
                          className="filter-select"
                        >
                          <option value="">Tüm Araç Tipleri</option>
                          <option value="Bus">Otobüs</option>
                          <option value="Train">Tren</option>
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
                          <option value="1">Aktif</option>
                          <option value="0">İptal</option>
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
                            setTripDateFromFilter('') // Tarih aralığını temizle
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
                            setTripDateFilter('') // Tek tarih filtresini temizle
                          }}
                          className="search-input"
                          title="Tarih aralığı başlangıcı"
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
                            setTripDateFilter('') // Tek tarih filtresini temizle
                          }}
                          className="search-input"
                          title="Tarih aralığı bitişi"
                        />
                      </div>
                      {(tripFromCityFilter || tripToCityFilter || tripFromCityIDFilter || tripToCityIDFilter || tripStatusFilter || tripVehicleTypeFilter || tripDateFromFilter || tripDateToFilter || tripDateFilter) && (
                        <button
                          className="btn btn-outline"
                          onClick={() => {
                            setTripFromCityFilter('')
                            setTripToCityFilter('')
                            setTripFromCityIDFilter('')
                            setTripToCityIDFilter('')
                            setTripStatusFilter('')
                            setTripVehicleTypeFilter('')
                            setTripDateFromFilter('')
                            setTripDateToFilter('')
                            setTripDateFilter('')
                          }}
                          style={{ minWidth: 'auto', padding: '8px 16px' }}
                        >
                          ✕ Temizle
                        </button>
                      )}
                    </div>
                  </div>
                  
                  <div className="table-container">
                    <table className="admin-table">
                      <thead>
                        <tr>
                          <th>ID</th>
                          <th>Tür</th>
                          <th>Nereden</th>
                          <th>Nereye</th>
                          <th>Tarih</th>
                          <th>Saat</th>
                          <th>Fiyat</th>
                          <th>Durum</th>
                          <th>İşlemler</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredTrips.length === 0 ? (
                          <tr>
                            <td colSpan="9" style={{ textAlign: 'center', padding: '20px' }}>
                              {trips.length === 0 ? 'Henüz sefer yok' : 'Filtreye uygun sefer bulunamadı'}
                            </td>
                          </tr>
                        ) : (
                          <>
                            {filteredTrips.slice(0, tripsDisplayLimit).map((trip) => (
                              <tr key={trip.TripID}>
                                <td>{trip.TripID}</td>
                                <td>
                                  <span style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                                    {trip.VehicleType === 'Train' ? '🚄' : '🚌'} {trip.VehicleType}
                                  </span>
                                </td>
                                <td>{trip.FromCity}</td>
                                <td>{trip.ToCity}</td>
                                <td>{formatDate(trip.DepartureDate)}</td>
                                <td>{typeof trip.DepartureTime === 'string' ? trip.DepartureTime : trip.DepartureTime?.toString().substring(0, 5) || '-'}</td>
                                <td>{formatCurrency(trip.Price)} ₺</td>
                                <td>
                                  <span className={`status-badge ${trip.Status === 1 ? 'active' : 'inactive'}`}>
                                    {trip.Status === 1 ? 'Aktif' : 'İptal'}
                                  </span>
                                </td>
                                <td>
                                  <div className="action-buttons">
                                    <button
                                      className="btn-sm btn-primary"
                                      onClick={() => handleEditTrip(trip)}
                                      title="Düzenle"
                                    >
                                      ✏️
                                    </button>
                                    {trip.Status === 1 && (
                                      <button
                                        className="btn-sm btn-danger"
                                        onClick={() => handleCancelTrip(trip)}
                                        title="İptal Et"
                                      >
                                        🚫
                                      </button>
                                    )}
                                  </div>
                                </td>
                              </tr>
                            ))}
                          </>
                        )}
                      </tbody>
                    </table>
                  </div>
                  {filteredTrips.length > tripsDisplayLimit && (
                    <div style={{ textAlign: 'center', marginTop: '20px' }}>
                      <button
                        className="btn btn-outline"
                        onClick={() => setTripsDisplayLimit(tripsDisplayLimit + 100)}
                      >
                        + {Math.min(100, filteredTrips.length - tripsDisplayLimit)} Daha Fazla Göster
                      </button>
                      <p style={{ marginTop: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        {tripsDisplayLimit} / {filteredTrips.length} sefer gösteriliyor {filteredTrips.length !== trips.length && `(Toplam: ${trips.length})`}
                      </p>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'reservations' && (
                <div className="card">
                  {/* Rezervasyon Filtreleme */}
                  <div className="table-filters">
                    <div className="filter-group">
                      <div className="search-wrapper">
                        <span className="search-icon">🔍</span>
                        <input
                          type="text"
                          placeholder="Kullanıcı adı ile ara..."
                          value={reservationSearchText}
                          onChange={(e) => setReservationSearchText(e.target.value)}
                          className="search-input"
                        />
                      </div>
                      <div className="select-wrapper">
                        <span className="filter-icon">📊</span>
                        <select
                          value={reservationStatusFilter}
                          onChange={(e) => setReservationStatusFilter(e.target.value)}
                          className="filter-select"
                        >
                          <option value="">Tüm Durumlar</option>
                          <option value="Reserved">Rezerve</option>
                          <option value="Cancelled">İptal</option>
                          <option value="Completed">Tamamlandı</option>
                        </select>
                      </div>
                      <div className="select-wrapper">
                        <span className="filter-icon">💳</span>
                        <select
                          value={reservationPaymentStatusFilter}
                          onChange={(e) => setReservationPaymentStatusFilter(e.target.value)}
                          className="filter-select"
                        >
                          <option value="">Tüm Ödeme Durumları</option>
                          <option value="Pending">Beklemede</option>
                          <option value="Paid">Ödendi</option>
                          <option value="Refunded">İade Edildi</option>
                        </select>
                      </div>
                      <div className="search-wrapper">
                        <span className="filter-icon">📅</span>
                        <input
                          type="date"
                          placeholder="Başlangıç Tarihi"
                          value={reservationDateFromFilter}
                          onChange={(e) => setReservationDateFromFilter(e.target.value)}
                          className="search-input"
                        />
                      </div>
                      <div className="search-wrapper">
                        <span className="filter-icon">📅</span>
                        <input
                          type="date"
                          placeholder="Bitiş Tarihi"
                          value={reservationDateToFilter}
                          onChange={(e) => setReservationDateToFilter(e.target.value)}
                          className="search-input"
                        />
                      </div>
                      {(reservationSearchText || reservationStatusFilter || reservationPaymentStatusFilter || reservationDateFromFilter || reservationDateToFilter) && (
                        <button
                          className="btn btn-outline"
                          onClick={() => {
                            setReservationSearchText('')
                            setReservationStatusFilter('')
                            setReservationPaymentStatusFilter('')
                            setReservationDateFromFilter('')
                            setReservationDateToFilter('')
                          }}
                          style={{ minWidth: 'auto', padding: '8px 16px' }}
                        >
                          ✕ Temizle
                        </button>
                      )}
                    </div>
                  </div>
                  
                  <div className="table-container">
                    <table className="admin-table">
                      <thead>
                        <tr>
                          <th>ID</th>
                          <th>Kullanıcı</th>
                          <th>Güzergah</th>
                          <th>Koltuk</th>
                          <th>Durum</th>
                          <th>Ödeme Durumu</th>
                          <th>Tarih</th>
                        </tr>
                      </thead>
                      <tbody>
                        {filteredReservations.length === 0 ? (
                          <tr>
                            <td colSpan="7" style={{ textAlign: 'center', padding: '20px' }}>
                              {reservations.length === 0 ? 'Henüz rezervasyon yok' : 'Filtreye uygun rezervasyon bulunamadı'}
                            </td>
                          </tr>
                        ) : (
                          <>
                            {filteredReservations.slice(0, reservationsDisplayLimit).map((reservation) => (
                            <tr key={reservation.ReservationID}>
                              <td>{reservation.ReservationID}</td>
                              <td>
                                <div>
                                  <div style={{ fontWeight: 600 }}>{reservation.UserName || `Kullanıcı #${reservation.UserID}`}</div>
                                  <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>ID: {reservation.UserID}</div>
                                </div>
                              </td>
                              <td>
                                <div>
                                  <div>{reservation.TripRoute || `Sefer #${reservation.TripID}`}</div>
                                  <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Sefer ID: {reservation.TripID}</div>
                                </div>
                              </td>
                              <td>{reservation.SeatNo || `Koltuk #${reservation.SeatID}`}</td>
                              <td>
                                <span className={`status-badge ${reservation.Status?.toLowerCase()}`}>
                                  {reservation.Status}
                                </span>
                              </td>
                              <td>
                                <span className={`status-badge ${reservation.PaymentStatus?.toLowerCase()}`}>
                                  {reservation.PaymentStatus}
                                </span>
                              </td>
                              <td>{formatDate(reservation.ReservationDate)}</td>
                            </tr>
                          ))}
                        </>
                      )}
                    </tbody>
                  </table>
                  </div>
                  {filteredReservations.length > reservationsDisplayLimit && (
                    <div style={{ textAlign: 'center', marginTop: '20px' }}>
                      <button
                        className="btn btn-outline"
                        onClick={() => setReservationsDisplayLimit(reservationsDisplayLimit + 100)}
                      >
                        + {Math.min(100, filteredReservations.length - reservationsDisplayLimit)} Daha Fazla Göster
                      </button>
                      <p style={{ marginTop: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        {reservationsDisplayLimit} / {filteredReservations.length} rezervasyon gösteriliyor {filteredReservations.length !== reservations.length && `(Toplam: ${reservations.length})`}
                      </p>
                    </div>
                  )}
                </div>
              )}

              {/* Finansal Raporlar Sekmesi */}
              {activeTab === 'financial-reports' && (
                <div className="card">
                  <div className="content-header">
                    <h2>💰 Güzergah Ciro Raporu</h2>
                    <button
                      className="btn btn-primary"
                      onClick={fetchRouteRevenueReport}
                      disabled={loadingRouteRevenue}
                    >
                      {loadingRouteRevenue ? 'Yükleniyor...' : '🔄 Yenile'}
                    </button>
                  </div>
                  
                  {loadingRouteRevenue ? (
                    <p className="info-text">Rapor yükleniyor...</p>
                  ) : routeRevenueReport.length === 0 ? (
                    <p className="info-text">Henüz rapor verisi bulunmuyor.</p>
                  ) : (
                    <div className="table-wrapper" style={{ marginTop: '20px' }}>
                      <table className="admin-table">
                        <thead>
                          <tr>
                            <th>Güzergah</th>
                            <th>Araç Tipi</th>
                            <th>Toplam Satış Adedi</th>
                            <th>Toplam Ciro</th>
                            <th>Ortalama Bilet Fiyatı</th>
                          </tr>
                        </thead>
                        <tbody>
                          {routeRevenueReport.map((report, index) => {
                            const guzergah = report.Guzergah ?? report.guzergah ?? ''
                            const aracTipi = report.AracTipi ?? report.aracTipi ?? ''
                            const toplamSatis = report.ToplamSatisAdedi ?? report.toplamSatisAdedi ?? 0
                            const toplamCiro = report.ToplamCiro ?? report.toplamCiro ?? 0
                            const ortalamaFiyat = report.OrtalamaBiletFiyati ?? report.ortalamaBiletFiyati ?? 0
                            
                            return (
                              <tr key={index}>
                                <td><strong>{guzergah}</strong></td>
                                <td>{aracTipi === 'Bus' ? '🚌 Otobüs' : aracTipi === 'Train' ? '🚄 Tren' : aracTipi}</td>
                                <td>{toplamSatis}</td>
                                <td><strong>{toplamCiro.toFixed(2)} ₺</strong></td>
                                <td>{ortalamaFiyat ? ortalamaFiyat.toFixed(2) + ' ₺' : '-'}</td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}

              {/* Sefer Detayları Sekmesi */}
              {activeTab === 'trip-details' && (
                <div className="card">
                  <div className="content-header">
                    <h2>📋 Sefer Detayları</h2>
                  </div>
                  
                  <div className="form-card" style={{ marginBottom: '24px' }}>
                    <div className="form-group">
                      <label>Sefer ID ile Ara (Opsiyonel)</label>
                      <div style={{ display: 'flex', gap: '10px' }}>
                        <input
                          type="number"
                          value={selectedTripIdForDetails}
                          onChange={(e) => setSelectedTripIdForDetails(e.target.value)}
                          placeholder="Sefer ID girin (boş bırakırsanız tüm seferler listelenir)"
                          style={{ flex: 1 }}
                        />
                        <button
                          className="btn btn-primary"
                          onClick={handleSearchTripDetails}
                          disabled={loadingTripDetails}
                        >
                          {loadingTripDetails ? 'Yükleniyor...' : '🔍 Ara'}
                        </button>
                        <button
                          className="btn btn-secondary"
                          onClick={() => {
                            setSelectedTripIdForDetails('')
                            fetchTripDetails()
                          }}
                          disabled={loadingTripDetails}
                        >
                          Tümünü Göster
                        </button>
                      </div>
                    </div>
                  </div>
                  
                  {loadingTripDetails ? (
                    <p className="info-text">Sefer detayları yükleniyor...</p>
                  ) : tripDetails.length === 0 ? (
                    <p className="info-text">Sefer detayı bulunamadı.</p>
                  ) : (
                    <div className="table-wrapper" style={{ marginTop: '20px' }}>
                      <table className="admin-table">
                        <thead>
                          <tr>
                            <th>Sefer ID</th>
                            <th>Güzergah</th>
                            <th>Araç</th>
                            <th>Tarih & Saat</th>
                            <th>Fiyat</th>
                            <th>Koltuk Durumu</th>
                            <th>Doluluk</th>
                            <th>Durum</th>
                          </tr>
                        </thead>
                        <tbody>
                          {tripDetails.slice(0, tripDetailsDisplayLimit).map((trip) => {
                            const tripId = trip.TripID ?? trip.tripID ?? 0
                            const nereden = trip.Nereden ?? trip.nereden ?? ''
                            const nereye = trip.Nereye ?? trip.nereye ?? ''
                            const aracTipi = trip.AracTipi ?? trip.aracTipi ?? ''
                            const plakaNo = trip.PlakaNo ?? trip.plakaNo ?? ''
                            const departureDate = trip.DepartureDate ?? trip.departureDate
                            const departureTime = trip.DepartureTime ?? trip.departureTime
                            const biletFiyati = trip.BiletFiyati ?? trip.biletFiyati ?? 0
                            const toplamKoltuk = trip.ToplamKoltuk ?? trip.toplamKoltuk ?? 0
                            const satilanKoltuk = trip.SatilanKoltuk ?? trip.satilanKoltuk ?? 0
                            const bosKoltuk = trip.BosKoltuk ?? trip.bosKoltuk ?? (toplamKoltuk - satilanKoltuk)
                            const dolulukOrani = trip.DolulukOrani ?? trip.dolulukOrani ?? (toplamKoltuk > 0 ? (satilanKoltuk / toplamKoltuk * 100) : 0)
                            const seferDurumu = trip.SeferDurumu ?? trip.seferDurumu ?? ''
                            
                            // Tarih formatlama
                            let formattedDate = '-'
                            if (departureDate) {
                              try {
                                const date = new Date(departureDate)
                                if (!isNaN(date.getTime())) {
                                  formattedDate = date.toLocaleDateString('tr-TR', {
                                    year: 'numeric',
                                    month: '2-digit',
                                    day: '2-digit'
                                  })
                                }
                              } catch (e) {
                                console.error('Tarih formatlama hatası:', e)
                              }
                            }
                            
                            // Saat formatlama
                            let formattedTime = '-'
                            if (departureTime) {
                              try {
                                if (typeof departureTime === 'string') {
                                  formattedTime = departureTime.substring(0, 5) // "HH:mm" formatı
                                } else if (departureTime.hours !== undefined) {
                                  formattedTime = `${String(departureTime.hours).padStart(2, '0')}:${String(departureTime.minutes).padStart(2, '0')}`
                                }
                              } catch (e) {
                                console.error('Saat formatlama hatası:', e)
                              }
                            }
                            
                            return (
                              <tr key={tripId}>
                                <td><strong>{tripId}</strong></td>
                                <td>
                                  <strong>{nereden}</strong> → <strong>{nereye}</strong>
                                </td>
                                <td>
                                  {aracTipi === 'Bus' ? '🚌' : aracTipi === 'Train' ? '🚄' : ''} {plakaNo}
                                </td>
                                <td>
                                  {formattedDate} {formattedTime}
                                </td>
                                <td><strong>{biletFiyati.toFixed(2)} ₺</strong></td>
                                <td>
                                  <span style={{ color: '#4caf50' }}>{satilanKoltuk}</span> / <span style={{ color: '#666' }}>{toplamKoltuk}</span>
                                  <br />
                                  <small style={{ color: '#999' }}>{bosKoltuk} boş</small>
                                </td>
                                <td>
                                  <div style={{ 
                                    background: dolulukOrani > 80 ? '#ff9800' : dolulukOrani > 50 ? '#4caf50' : '#2196f3',
                                    color: 'white',
                                    padding: '4px 8px',
                                    borderRadius: '4px',
                                    fontSize: '12px',
                                    fontWeight: 'bold'
                                  }}>
                                    {dolulukOrani.toFixed(1)}%
                                  </div>
                                </td>
                                <td>
                                  <span style={{ 
                                    color: seferDurumu === 'Aktif' ? '#4caf50' : '#f44336',
                                    fontWeight: 'bold'
                                  }}>
                                    {seferDurumu}
                                  </span>
                                </td>
                              </tr>
                            )
                          })} 
                        </tbody>
                      </table>
                    </div>
                  )}
                  
                  {tripDetails.length > tripDetailsDisplayLimit && (
                    <div style={{ textAlign: 'center', marginTop: '20px' }}>
                      <button
                        className="btn btn-outline"
                        onClick={() => setTripDetailsDisplayLimit(tripDetailsDisplayLimit + 100)}
                      >
                        + {Math.min(100, tripDetails.length - tripDetailsDisplayLimit)} Daha Fazla Göster
                      </button>
                      <p style={{ marginTop: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                        {tripDetailsDisplayLimit} / {tripDetails.length} sefer gösteriliyor
                      </p>
                    </div>
                  )}
                </div>
              )}

              {/* Otomatik İptal Sekmesi */}
              {activeTab === 'auto-cancellation' && (
                <div className="card">
                  <div className="content-header">
                    <h2>⏰ Otomatik İptal Sistemi</h2>
                  </div>
                  
                  {/* Ayarlar */}
                  <div className="form-card" style={{ marginBottom: '24px' }}>
                    <h3>⚙️ Ayarlar</h3>
                    <div className="form-group">
                      <label>Zaman Aşımı Süresi (Dakika)</label>
                      <input
                        type="number"
                        min="1"
                        max="1440"
                        value={timeoutMinutes}
                        onChange={(e) => setTimeoutMinutes(parseInt(e.target.value) || 15)}
                        placeholder="15"
                      />
                      <small className="form-hint">
                        Rezervasyon oluşturulduktan sonra bu süre içinde ödeme yapılmazsa otomatik olarak iptal edilir.
                        (1-1440 dakika arası)
                      </small>
                    </div>
                    <button
                      className="btn btn-primary"
                      onClick={handleUpdateAutoCancellationSettings}
                      disabled={loading}
                    >
                      {loading ? 'Kaydediliyor...' : 'Ayarları Kaydet'}
                    </button>
                    {autoCancellationSettings && (
                      <div style={{ marginTop: '16px', padding: '12px', background: '#f0f0f0', borderRadius: '8px' }}>
                        <p><strong>Mevcut Durum:</strong> {autoCancellationSettings.Durum || 'Aktif'}</p>
                        <p><strong>Açıklama:</strong> {autoCancellationSettings.Aciklama || 'Ayarlar kaydedildi'}</p>
                      </div>
                    )}
                  </div>

                  {/* İptal İşlemi */}
                  <div className="form-card" style={{ marginBottom: '24px' }}>
                    <h3>🔄 Zaman Aşımı Rezervasyonlarını İptal Et</h3>
                    <p className="info-text">
                      Belirtilen süre içinde ödeme yapılmamış rezervasyonları otomatik olarak iptal eder.
                    </p>
                    <button
                      className="btn btn-warning"
                      onClick={handleProcessTimeoutReservations}
                      disabled={processingCancellation}
                    >
                      {processingCancellation ? 'İşleniyor...' : 'İptal İşlemini Çalıştır'}
                    </button>
                  </div>

                  {/* İptal Logları */}
                  <div className="form-card">
                    <h3>📋 İptal Logları</h3>
                    {autoCancellationLogs.length === 0 ? (
                      <p className="info-text">Henüz iptal logu bulunmuyor.</p>
                    ) : (
                      <div className="table-wrapper">
                        <table className="data-table">
                          <thead>
                            <tr>
                              <th>Log ID</th>
                              <th>Rezervasyon ID</th>
                              <th>Kullanıcı</th>
                              <th>İptal Tarihi</th>
                              <th>Neden</th>
                              <th>Timeout (dk)</th>
                            </tr>
                          </thead>
                          <tbody>
                            {autoCancellationLogs.map((log) => {
                              // Backend hem PascalCase hem camelCase döndürebilir
                              const logId = log.LogID ?? log.logID ?? 0
                              const reservationId = log.ReservationID ?? log.reservationID ?? 0
                              const userId = log.UserID ?? log.userID ?? 0
                              const userName = log.UserName ?? log.userName ?? (userId ? `User ${userId}` : 'Bilinmiyor')
                              const cancelledAt = log.CancelledAt ?? log.cancelledAt
                              const reason = log.Reason ?? log.reason ?? 'Zaman aşımı'
                              const timeoutMinutes = log.TimeoutMinutes ?? log.timeoutMinutes ?? 0
                              
                              // Tarih formatlama
                              let formattedDate = 'Geçersiz Tarih'
                              if (cancelledAt) {
                                try {
                                  const date = new Date(cancelledAt)
                                  if (!isNaN(date.getTime())) {
                                    formattedDate = date.toLocaleString('tr-TR', {
                                      year: 'numeric',
                                      month: '2-digit',
                                      day: '2-digit',
                                      hour: '2-digit',
                                      minute: '2-digit'
                                    })
                                  }
                                } catch (e) {
                                  console.error('Tarih formatlama hatası:', e)
                                }
                              }
                              
                              return (
                                <tr key={logId}>
                                  <td>{logId}</td>
                                  <td>{reservationId}</td>
                                  <td>{userName}</td>
                                  <td>{formattedDate}</td>
                                  <td>{reason}</td>
                                  <td>{timeoutMinutes}</td>
                                </tr>
                              )
                            })}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {/* Kullanıcı Düzenleme Modal */}
      {showUserEditModal && selectedUser && (
        <div className="modal-overlay" onClick={() => setShowUserEditModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Kullanıcı Bilgilerini Düzenle</h3>
            <form onSubmit={(e) => { e.preventDefault(); handleUpdateUser(); }}>
              <div className="form-group">
                <label>Ad Soyad</label>
                <input
                  type="text"
                  value={userEditData.FullName}
                  onChange={(e) => setUserEditData({ ...userEditData, FullName: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>E-posta</label>
                <input
                  type="email"
                  value={userEditData.Email}
                  onChange={(e) => setUserEditData({ ...userEditData, Email: e.target.value })}
                  required
                />
              </div>
              <div className="form-group">
                <label>Telefon</label>
                <input
                  type="tel"
                  value={userEditData.Phone}
                  onChange={(e) => setUserEditData({ ...userEditData, Phone: e.target.value })}
                />
              </div>
              <div className="form-actions">
                <button type="submit" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Güncelleniyor...' : 'Güncelle'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowUserEditModal(false)
                    setSelectedUser(null)
                    setUserEditData({ FullName: '', Email: '', Phone: '' })
                  }}
                >
                  İptal
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Kullanıcı Durum Değiştirme Modal */}
      {showUserStatusModal && selectedUser && (
        <div className="modal-overlay" onClick={() => setShowUserStatusModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Kullanıcı Durumu Değiştir</h3>
            <p>
              <strong>{selectedUser.FullName}</strong> kullanıcısının durumunu değiştirmek istediğinize emin misiniz?
            </p>
            <div className="form-group">
              <label>Sebep (Opsiyonel)</label>
              <textarea
                value={statusReason}
                onChange={(e) => setStatusReason(e.target.value)}
                placeholder="Durum değişikliği sebebi..."
                rows="3"
              />
            </div>
            <div className="modal-actions">
              <button className="btn btn-primary" onClick={confirmUpdateUserStatus}>
                Onayla
              </button>
              <button className="btn btn-secondary" onClick={() => setShowUserStatusModal(false)}>
                İptal
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Sefer İptal Modal */}
      {showCancelTripModal && selectedTrip && (
        <div className="modal-overlay" onClick={() => setShowCancelTripModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h3>Sefer İptal Et</h3>
            <p>
              <strong>Sefer #{selectedTrip.TripID}</strong> seferini iptal etmek istediğinize emin misiniz?
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
              <button className="btn btn-danger" onClick={confirmCancelTrip}>
                İptal Et
              </button>
              <button className="btn btn-secondary" onClick={() => setShowCancelTripModal(false)}>
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

export default AdminPanel
