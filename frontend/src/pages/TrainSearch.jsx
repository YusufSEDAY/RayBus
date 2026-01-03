import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { trainAPI, cityAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './Search.css'

const TrainSearch = () => {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [formData, setFormData] = useState({
    fromCityId: '',
    toCityId: '',
    date: searchParams.get('date') || new Date().toISOString().split('T')[0],
  })
  const [cities, setCities] = useState([])
  const [citiesWithStations, setCitiesWithStations] = useState([])
  const [results, setResults] = useState(null)
  const [loading, setLoading] = useState(false)
  const [loadingCities, setLoadingCities] = useState(true)
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  // Debug: results state değişikliklerini izle
  useEffect(() => {
    console.log('🔄 TrainSearch - results state güncellendi:', results)
    if (results) {
      console.log('📊 Results tipi:', typeof results, 'Array mi?', Array.isArray(results))
      if (Array.isArray(results)) {
        console.log('📊 Results uzunluğu:', results.length)
        if (results.length > 0) {
          console.log('📊 İlk trip:', results[0])
        }
      } else if (typeof results === 'object' && 'error' in results) {
        console.log('❌ Results error:', results.error)
      }
    }
  }, [results])

  // Şehirleri yükle - sadece component mount olduğunda
  useEffect(() => {
    fetchCities()
  }, [])

  // Debug: cities state değişikliklerini izle
  useEffect(() => {
    console.log('🔄 TrainSearch - cities state güncellendi:', cities.length, 'şehir')
    if (cities.length > 0) {
      console.log('📋 İlk 5 şehir:', cities.slice(0, 5))
    }
  }, [cities])
  
  // URL parametrelerinden arama yap - cities yüklendikten sonra
  useEffect(() => {
    if (cities.length === 0) return // Şehirler henüz yüklenmedi
    
    const from = searchParams.get('from')
    const to = searchParams.get('to')
    const date = searchParams.get('date')
    
    if (from && to && date) {
      // Şehir isimlerinden ID bul
      // Backend PascalCase döndürüyor
      const fromCity = cities.find(c => c.CityName === from)
      const toCity = cities.find(c => c.CityName === to)
      
      if (fromCity && toCity) {
        setFormData(prev => ({
          ...prev,
          fromCityId: fromCity.CityID.toString(),
          toCityId: toCity.CityID.toString(),
          date: date
        }))
        
        // Otomatik arama yap
        setTimeout(() => {
          handleAutoSearch(fromCity.CityName, toCity.CityName, date)
        }, 500)
      }
    }
  }, [cities, searchParams])
  
  const handleAutoSearch = async (from, to, date) => {
    // Zaten arama yapıldıysa tekrar yapma
    if (loading) return
    
    setLoading(true)
    try {
      const response = await trainAPI.search(from, to, date)
      console.log('🔍 TrainSearch API Response:', response.data)
      
      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      const message = response.data?.Message ?? response.data?.message
      
      if (success && Array.isArray(data)) {
        console.log('✅ Seferler bulundu:', data.length)
        setResults(data)
        if (data.length === 0) {
          setSnackbar({
            isOpen: true,
            message: 'Bu rota için sefer bulunamadı. Lütfen farklı bir tarih veya rota deneyin.',
            type: 'info'
          })
        }
      } else {
        console.error('❌ Arama hatası:', message)
        setResults({ error: message || 'Arama sırasında bir hata oluştu' })
        setSnackbar({
          isOpen: true,
          message: message || 'Arama yapılırken bir hata oluştu. Lütfen tekrar deneyin.',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Arama hatası:', error)
      const errorMessage = error.response?.data?.Message || error.response?.data?.message || 'Arama yapılırken bir hata oluştu'
      setResults({ error: errorMessage })
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }


  const fetchCities = async () => {
    try {
      setLoadingCities(true)
      console.log('Şehirler yükleniyor...')
      console.log('API URL:', '/api/city (Vite proxy üzerinden)')
      
      const response = await cityAPI.getAll()
      console.log('🔍 API Yanıtı (tam):', response)
      console.log('🔍 API Yanıtı (data):', response?.data)
      
      // Backend PascalCase döndürüyor: {Success: true, Message: "...", Data: [...], Errors: []}
      if (response?.data) {
        // Backend formatı: {Success, Message, Data, Errors}
        const success = response.data.Success || response.data.success
        const data = response.data.Data || response.data.data
        
        console.log('🔍 Success:', success, 'Data:', data, 'Data tipi:', typeof data, 'Array mi?', Array.isArray(data))
        
        if (success && Array.isArray(data) && data.length > 0) {
          console.log('🔍 Ham şehir verileri:', data)
          console.log('🔍 İlk şehir örneği:', data[0])
          console.log('🔍 İlk şehir keys:', data[0] ? Object.keys(data[0]) : 'null')
          
          // Backend hem PascalCase hem camelCase döndürebilir, her ikisini de kontrol et
          const citiesList = data
            .filter(city => {
              if (!city) return false
              const cityID = city.CityID ?? city.cityID ?? city.CityId ?? city.cityId
              const cityName = city.CityName ?? city.cityName ?? city.City_Name
              return cityID != null && cityName
            })
            .map(city => {
              const cityID = city.CityID ?? city.cityID ?? city.CityId ?? city.cityId
              const cityName = city.CityName ?? city.cityName ?? city.City_Name
              return {
                CityID: cityID,
                CityName: cityName
              }
            })
            .sort((a, b) => a.CityName.localeCompare(b.CityName, 'tr', { sensitivity: 'base' }))
          
          console.log('✅ Şehirler yüklendi (alfabetik sıralı):', citiesList.length, citiesList.slice(0, 3))
          setCities(citiesList)
          console.log('✅ setCities çağrıldı, cities state güncellendi:', citiesList.length, 'şehir')
          
          // Her şehir için istasyon bilgilerini yükle
          // Önce cities'i set et, sonra stations'ı yükle (async)
          fetchCitiesWithStations(citiesList).catch(err => {
            console.error('İstasyon bilgileri yüklenirken hata:', err)
            // Hata olsa bile cities state'i zaten set edildi, dropdown'da gösterilebilir
          })
          
          return
        }
        
        // Fallback: Direkt array
        if (Array.isArray(response.data)) {
          const citiesList = response.data
            .filter(city => {
              if (!city) return false
              const cityID = city.CityID ?? city.cityID ?? city.CityId ?? city.cityId
              const cityName = city.CityName ?? city.cityName ?? city.City_Name
              return cityID != null && cityName
            })
            .map(city => {
              const cityID = city.CityID ?? city.cityID ?? city.CityId ?? city.cityId
              const cityName = city.CityName ?? city.cityName ?? city.City_Name
              return {
                CityID: cityID,
                CityName: cityName
              }
            })
            .sort((a, b) => a.CityName.localeCompare(b.CityName, 'tr', { sensitivity: 'base' }))
          setCities(citiesList)
          console.log('✅ Şehirler yüklendi (Format 2, alfabetik sıralı):', citiesList.length)
          await fetchCitiesWithStations(citiesList)
          return
        }
      }
      
      console.error('❌ Geçersiz yanıt formatı:', {
        response: response,
        data: response?.data,
        success: response?.data?.success,
        dataData: response?.data?.data,
        isArray: Array.isArray(response?.data?.data)
      })
      setCities([])
    } catch (error) {
      console.error('❌ Şehirler yüklenirken hata:', error)
      console.error('Hata detayları:', {
        message: error.message,
        response: error.response,
        request: error.request,
        config: error.config
      })
      
      let errorMessage = 'Şehirler yüklenirken bir hata oluştu'
      if (error.response) {
        console.error('HTTP Hatası:', error.response.status, error.response.data)
        errorMessage = error.response.data?.Message || error.response.data?.message || `Sunucu hatası (${error.response.status})`
      } else if (error.request) {
        console.error('Sunucuya ulaşılamadı. Backend çalışıyor mu?')
        errorMessage = 'Sunucuya ulaşılamadı. Lütfen internet bağlantınızı kontrol edin.'
      } else {
        console.error('İstek hatası:', error.message)
        errorMessage = `İstek hatası: ${error.message}`
      }
      
      setCities([])
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setLoadingCities(false)
    }
  }

  const fetchCitiesWithStations = async (citiesList) => {
    try {
      const citiesWithData = await Promise.all(
        citiesList.map(async (city) => {
          try {
            const stationsResponse = await cityAPI.getStations(city.CityID).catch(() => ({ data: { Success: false, Data: [] } }))
            
            // Backend hem PascalCase hem camelCase döndürebilir
            const success = stationsResponse?.data?.Success ?? stationsResponse?.data?.success
            const stationsData = stationsResponse?.data?.Data ?? stationsResponse?.data?.data ?? []
            const stations = success && Array.isArray(stationsData) ? stationsData : []

            // İstasyon bilgilerini normalize et
            const normalizedStations = stations.map(stat => ({
              StationID: stat.StationID ?? stat.stationID ?? stat.StationId ?? stat.stationId,
              StationName: stat.StationName ?? stat.stationName ?? stat.Station_Name,
              CityID: stat.CityID ?? stat.cityID ?? stat.CityId ?? stat.cityId,
              CityName: stat.CityName ?? stat.cityName ?? stat.City_Name
            })).filter(stat => stat.StationName && stat.StationID)

            return {
              ...city,
              stations: normalizedStations
            }
          } catch (error) {
            console.error(`Şehir ${city.CityID} için istasyon bilgileri yüklenirken hata:`, error)
            return {
              ...city,
              stations: []
            }
          }
        })
      )

      setCitiesWithStations(citiesWithData)
      console.log('✅ Şehirler istasyon bilgileriyle yüklendi:', citiesWithData.length, 'şehir')
      const totalStations = citiesWithData.reduce((sum, city) => sum + (city.stations?.length || 0), 0)
      console.log('📊 Toplam istasyon sayısı:', totalStations)
    } catch (error) {
      console.error('Şehir istasyon bilgileri yüklenirken hata:', error)
      setCitiesWithStations(citiesList.map(city => ({ ...city, stations: [] })))
    }
  }


  const handleChange = (e) => {
    const { name, value } = e.target
    setFormData({
      ...formData,
      [name]: value
    })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      const fromCity = cities.find(c => c.CityID === parseInt(formData.fromCityId))
      const toCity = cities.find(c => c.CityID === parseInt(formData.toCityId))
      
      if (!fromCity || !toCity) {
        setResults({ error: 'Lütfen geçerli şehirler seçin' })
        setSnackbar({
          isOpen: true,
          message: 'Lütfen kalkış ve varış şehirlerini seçin',
          type: 'warning'
        })
        return
      }

      const response = await trainAPI.search(fromCity.CityName, toCity.CityName, formData.date)
      console.log('🔍 TrainSearch API Response:', response.data)
      
      // Backend hem PascalCase hem camelCase döndürebilir
      const success = response.data?.Success ?? response.data?.success
      const data = response.data?.Data ?? response.data?.data
      const message = response.data?.Message ?? response.data?.message
      
      if (success && Array.isArray(data)) {
        console.log('✅ Seferler bulundu:', data.length)
        setResults(data)
        if (data.length === 0) {
          setSnackbar({
            isOpen: true,
            message: 'Bu rota için sefer bulunamadı. Lütfen farklı bir tarih veya rota deneyin.',
            type: 'info'
          })
        }
      } else {
        console.error('❌ Arama hatası:', message)
        setResults({ error: message || 'Arama sırasında bir hata oluştu' })
        setSnackbar({
          isOpen: true,
          message: message || 'Arama yapılırken bir hata oluştu. Lütfen tekrar deneyin.',
          type: 'error'
        })
      }
    } catch (error) {
      console.error('Arama hatası:', error)
      const errorMessage = error.response?.data?.Message || error.response?.data?.message || 'Arama yapılırken bir hata oluştu'
      setResults({ error: errorMessage })
      setSnackbar({
        isOpen: true,
        message: errorMessage,
        type: 'error'
      })
    } finally {
      setLoading(false)
    }
  }

  const formatTime = (time) => {
    if (!time) return ''
    
    // String formatı (HH:mm)
    if (typeof time === 'string') {
      return time.substring(0, 5)
    }
    
    // TimeSpan formatı (C# backend'den geliyor)
    if (time.totalSeconds !== undefined) {
      const hours = Math.floor(time.totalSeconds / 3600)
      const minutes = Math.floor((time.totalSeconds % 3600) / 60)
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    
    // TimeSpan object formatı
    if (time.hours !== undefined || time.minutes !== undefined) {
      const hours = time.hours || 0
      const minutes = time.minutes || 0
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    
    // Ticks formatı
    if (time.ticks !== undefined) {
      const hours = Math.floor(time.ticks / 36000000000)
      const minutes = Math.floor((time.ticks % 36000000000) / 600000000)
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    
    // Number formatı (saniye cinsinden)
    if (typeof time === 'number') {
      const hours = Math.floor(time / 3600)
      const minutes = Math.floor((time % 3600) / 60)
      return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    }
    
    return ''
  }

  // Component render kontrolü
  useEffect(() => {
    console.log('✅ TrainSearch component render edildi')
  }, [])

  return (
    <div className="search-page">
      <div className="container">
        <div className="page-header">
          <h1 className="page-title">
            <span className="page-title-emoji">🚄</span>
            Tren Bileti Ara
          </h1>
          <p className="page-subtitle">
            İstediğiniz tarih ve rota için tren seferlerini bulun
          </p>
        </div>

        <div className="search-card card">
          <form onSubmit={handleSubmit} className="search-form">
            <div className="grid grid-3">
              <div className="input-group">
                <label htmlFor="fromCityId">Nereden</label>
                <select
                  id="fromCityId"
                  name="fromCityId"
                  value={formData.fromCityId}
                  onChange={handleChange}
                  required
                  disabled={loadingCities}
                >
                  <option value="">{loadingCities ? 'Yükleniyor...' : 'Şehir Seçin'}</option>
                  {cities.length > 0 && cities.map(city => {
                    if (!city || !city.CityID || !city.CityName) return null
                    
                    // Önce citiesWithStations kontrolü yap
                    if (citiesWithStations.length > 0) {
                      const cityWithStat = citiesWithStations.find(c => c.CityID === city.CityID)
                      if (cityWithStat) {
                        const stations = cityWithStat.stations || []
                        
                        if (stations.length > 0) {
                          return stations.map((station, index) => {
                            const stationName = station.StationName || station.stationName || station.Station_Name
                            return (
                              <option key={`from-${city.CityID}-${index}`} value={city.CityID.toString()}>
                                {stationName}, {city.CityName}
                              </option>
                            )
                          })
                        }
                      }
                    }
                    
                    // Sadece şehir adı
                    return (
                      <option key={`from-${city.CityID}`} value={city.CityID.toString()}>
                        {city.CityName}
                      </option>
                    )
                  })}
                  {!loadingCities && cities.length === 0 && citiesWithStations.length === 0 && (
                    <option value="" disabled>Şehir bulunamadı</option>
                  )}
                </select>
              </div>
              <div className="input-group">
                <label htmlFor="toCityId">Nereye</label>
                <select
                  id="toCityId"
                  name="toCityId"
                  value={formData.toCityId}
                  onChange={handleChange}
                  required
                  disabled={loadingCities}
                >
                  <option value="">{loadingCities ? 'Yükleniyor...' : 'Şehir Seçin'}</option>
                  {cities.length > 0 && cities.map(city => {
                    if (!city || !city.CityID || !city.CityName) return null
                    
                    // Önce citiesWithStations kontrolü yap
                    if (citiesWithStations.length > 0) {
                      const cityWithStat = citiesWithStations.find(c => c.CityID === city.CityID)
                      if (cityWithStat) {
                        const stations = cityWithStat.stations || []
                        
                        if (stations.length > 0) {
                          return stations.map((station, index) => {
                            const stationName = station.StationName || station.stationName || station.Station_Name
                            return (
                              <option key={`to-${city.CityID}-${index}`} value={city.CityID.toString()}>
                                {stationName}, {city.CityName}
                              </option>
                            )
                          })
                        }
                      }
                    }
                    
                    // Sadece şehir adı
                    return (
                      <option key={`to-${city.CityID}`} value={city.CityID.toString()}>
                        {city.CityName}
                      </option>
                    )
                  })}
                  {!loadingCities && cities.length === 0 && citiesWithStations.length === 0 && (
                    <option value="" disabled>Şehir bulunamadı</option>
                  )}
                </select>
              </div>
              <div className="input-group">
                <label htmlFor="date">Tarih</label>
                <input
                  type="date"
                  id="date"
                  name="date"
                  value={formData.date}
                  onChange={handleChange}
                  min={new Date().toISOString().split('T')[0]}
                  required
                />
              </div>
            </div>
            <button type="submit" className="btn btn-primary" disabled={loading || loadingCities}>
              {loading ? 'Aranıyor...' : 'Sefer Ara'}
            </button>
          </form>
        </div>

        {results !== null && (
          <div className="results-section">
            {results && typeof results === 'object' && 'error' in results ? (
              <div className="card error-card">
                <p>{results.error}</p>
              </div>
            ) : Array.isArray(results) && results.length > 0 ? (
              <div className="trip-list">
                <h2 className="results-title">Bulunan Seferler ({results.length})</h2>
                {results.map((trip, index) => {
                  // TripID kontrolü - hem PascalCase hem camelCase
                  const tripID = trip?.TripID ?? trip?.tripID ?? trip?.TripId ?? trip?.tripId
                  
                  if (!trip || !tripID) {
                    console.warn('⚠️ Geçersiz trip verisi:', trip, 'Index:', index)
                    return null
                  }
                  
                  console.log('🔍 Rendering trip:', trip)
                  
                  // Veri normalizasyonu - hem PascalCase hem camelCase
                  const fromCity = trip.FromCity ?? trip.fromCity ?? trip.KalkisSehri ?? 'N/A'
                  const toCity = trip.ToCity ?? trip.toCity ?? trip.VarisSehri ?? 'N/A'
                  const vehicleCode = trip.VehicleCode ?? trip.vehicleCode ?? trip.AracPlakaNo ?? 'N/A'
                  const availableSeats = trip.AvailableSeats ?? trip.availableSeats ?? trip.BosKoltukSayisi ?? 0
                  const price = trip.Price ?? trip.price ?? 0
                  const departureTime = trip.DepartureTime ?? trip.departureTime ?? trip.KalkisSaati
                  const departureDate = trip.DepartureDate ?? trip.departureDate
                  const departureStation = trip.DepartureStation ?? trip.departureStation ?? trip.KalkisNoktasi
                  const arrivalStation = trip.ArrivalStation ?? trip.arrivalStation ?? trip.VarisNoktasi
                  const arrivalTime = trip.ArrivalTime ?? trip.arrivalTime
                  
                  return (
                    <div key={tripID} className="card trip-card">
                      <div className="trip-card-content">
                        <div className="trip-route-info">
                          <div className="route-time">
                            <span className="time">{formatTime(departureTime)}</span>
                            <div className="location-info">
                              <span className="city">{fromCity}</span>
                              {departureStation && (
                                <span className="station">🚉 {departureStation}</span>
                              )}
                            </div>
                            {departureDate && (
                              <span className="date">{new Date(departureDate).toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' })}</span>
                            )}
                          </div>
                          <div className="route-arrow">→</div>
                          <div className="route-time">
                            {arrivalTime && (
                              <span className="time">{formatTime(arrivalTime)}</span>
                            )}
                            <div className="location-info">
                              <span className="city">{toCity}</span>
                              {arrivalStation && (
                                <span className="station">🚉 {arrivalStation}</span>
                              )}
                            </div>
                          </div>
                        </div>
                        <div className="trip-details">
                          <div className="detail-item">
                            <span className="label">Araç Kodu:</span>
                            <span className="value">{vehicleCode}</span>
                          </div>
                          <div className="detail-item">
                            <span className="label">Boş Koltuk:</span>
                            <span className="value highlight">{availableSeats}</span>
                          </div>
                        </div>
                        <div className="trip-price">
                          <div className="price-section">
                            <span className="price-label">Bilet Fiyatı</span>
                            <span className="price">
                              {price ? Number(price).toFixed(2) : '0.00'} ₺
                            </span>
                          </div>
                          <button 
                            className="btn btn-primary"
                            onClick={() => navigate(`/sefer/${tripID}`)}
                          >
                            Koltuk Seç
                          </button>
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            ) : (
              <div className="card">
                <p className="info-text">Bu rota için sefer bulunamadı.</p>
              </div>
            )}
          </div>
        )}
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

export default TrainSearch
