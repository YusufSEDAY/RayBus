import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { cityAPI } from '../services/api'
import Snackbar from '../components/Snackbar'
import './Home.css'

const Home = () => {
  const navigate = useNavigate()
  const [cities, setCities] = useState([])
  const [citiesWithLocations, setCitiesWithLocations] = useState([])
  const [searchForm, setSearchForm] = useState({
    fromCityId: '',
    toCityId: '',
    date: new Date().toISOString().split('T')[0],
    type: 'train'
  })
  const [loadingCities, setLoadingCities] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  useEffect(() => {
    fetchCities()
  }, [])

  useEffect(() => {
    console.log('🔄 cities state güncellendi:', cities.length, 'şehir')
    if (cities.length > 0) {
      console.log('📋 İlk 5 şehir:', cities.slice(0, 5))
    }
  }, [cities])

  useEffect(() => {
    if (citiesWithLocations.length > 0) {
    }
  }, [searchForm.type])

  const fetchCitiesWithLocations = async (citiesList) => {
    try {
      const citiesWithData = await Promise.all(
        citiesList.map(async (city) => {
          try {
            const [stationsResponse, terminalsResponse] = await Promise.all([
              cityAPI.getStations(city.CityID).catch(() => ({ data: { Success: false, Data: [] } })),
              cityAPI.getTerminals(city.CityID).catch(() => ({ data: { Success: false, Data: [] } }))
            ])

            const stationsSuccess = stationsResponse?.data?.Success ?? stationsResponse?.data?.success
            const stationsData = stationsResponse?.data?.Data ?? stationsResponse?.data?.data ?? []
            const stationsRaw = stationsSuccess && Array.isArray(stationsData) ? stationsData : []
            const stations = stationsRaw.map(stat => ({
              StationID: stat.StationID ?? stat.stationID ?? stat.StationId ?? stat.stationId,
              StationName: stat.StationName ?? stat.stationName ?? stat.Station_Name,
              CityID: stat.CityID ?? stat.cityID ?? stat.CityId ?? stat.cityId,
              CityName: stat.CityName ?? stat.cityName ?? stat.City_Name
            })).filter(stat => stat.StationName && stat.StationID)

            const terminalsSuccess = terminalsResponse?.data?.Success ?? terminalsResponse?.data?.success
            const terminalsData = terminalsResponse?.data?.Data ?? terminalsResponse?.data?.data ?? []
            const terminalsRaw = terminalsSuccess && Array.isArray(terminalsData) ? terminalsData : []
            const terminals = terminalsRaw.map(term => ({
              TerminalID: term.TerminalID ?? term.terminalID ?? term.TerminalId ?? term.terminalId,
              TerminalName: term.TerminalName ?? term.terminalName ?? term.Terminal_Name,
              CityID: term.CityID ?? term.cityID ?? term.CityId ?? term.cityId,
              CityName: term.CityName ?? term.cityName ?? term.City_Name
            })).filter(term => term.TerminalName && term.TerminalID)

            return {
              ...city,
              stations: stations,
              terminals: terminals
            }
          } catch (error) {
            console.error(`Şehir ${city.CityID} için lokasyon bilgileri yüklenirken hata:`, error)
            return {
              ...city,
              stations: [],
              terminals: []
            }
          }
        })
      )

      setCitiesWithLocations(citiesWithData)
      console.log('✅ Şehirler lokasyon bilgileriyle yüklendi:', citiesWithData.length, 'şehir')
      const totalStations = citiesWithData.reduce((sum, city) => sum + (city.stations?.length || 0), 0)
      const totalTerminals = citiesWithData.reduce((sum, city) => sum + (city.terminals?.length || 0), 0)
      console.log('📊 Toplam istasyon sayısı:', totalStations, 'Toplam terminal sayısı:', totalTerminals)
    } catch (error) {
      console.error('Şehir lokasyon bilgileri yüklenirken hata:', error)
      setCitiesWithLocations(citiesList.map(city => ({ ...city, stations: [], terminals: [] })))
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
      
      if (response?.data) {
        const success = response.data.Success || response.data.success
        const data = response.data.Data || response.data.data
        
        console.log('🔍 Success:', success, 'Data:', data, 'Data tipi:', typeof data, 'Array mi?', Array.isArray(data))
        
        if (success && Array.isArray(data) && data.length > 0) {
          console.log('🔍 Ham şehir verileri:', data)
          console.log('🔍 İlk şehir örneği:', data[0])
          console.log('🔍 İlk şehir keys:', data[0] ? Object.keys(data[0]) : 'null')
          
          const cities = data
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
          
          console.log('✅ Şehirler yüklendi (alfabetik sıralı):', cities.length, cities.slice(0, 3))
          setCities(cities)
          console.log('✅ setCities çağrıldı, cities state güncellendi:', cities.length, 'şehir')
          
          fetchCitiesWithLocations(cities).catch(err => {
            console.error('Lokasyon bilgileri yüklenirken hata:', err)
          })
          
          setErrorMessage('')
          return
        }
        
        if (Array.isArray(response.data)) {
          const cities = response.data
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
          setCities(cities)
          setErrorMessage('')
          console.log('✅ Şehirler yüklendi (Format 2, alfabetik sıralı):', cities.length)
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
      
      if (error.response) {
        console.error('HTTP Hatası:', error.response.status, error.response.data)
        const errorData = error.response.data
        const errorMessage = errorData?.message || errorData?.Message || `HTTP ${error.response.status} Hatası`
        const errors = errorData?.errors || errorData?.Errors || []
        const fullError = errors.length > 0 
          ? `${errorMessage}: ${errors.join(', ')}`
          : errorMessage
        setErrorMessage(fullError)
        setSnackbar({
          isOpen: true,
          message: fullError,
          type: 'error'
        })
        console.error('Detaylı hata:', errorData)
      } else if (error.request) {
        console.error('Sunucuya ulaşılamadı. Backend çalışıyor mu?')
        const errorMsg = 'Backend\'e ulaşılamıyor. Lütfen backend\'in çalıştığından emin olun.'
        setErrorMessage(errorMsg)
        setSnackbar({
          isOpen: true,
          message: errorMsg,
          type: 'error'
        })
      } else {
        console.error('İstek hatası:', error.message)
        const errorMsg = `İstek hatası: ${error.message}`
        setErrorMessage(errorMsg)
        setSnackbar({
          isOpen: true,
          message: errorMsg,
          type: 'error'
        })
      }
      setCities([])
    } finally {
      setLoadingCities(false)
    }
  }

  const handleChange = (e) => {
    const { name, value } = e.target
    setSearchForm({
      ...searchForm,
      [name]: value
    })
  }

  const handleSearch = (e) => {
    e.preventDefault()
    if (!searchForm.fromCityId || !searchForm.toCityId) {
      setSnackbar({
        isOpen: true,
        message: 'Lütfen kalkış ve varış şehirlerini seçin',
        type: 'warning'
      })
      return
    }

    const fromCity = cities.find(c => c && c.CityID === parseInt(searchForm.fromCityId))
    const toCity = cities.find(c => c && c.CityID === parseInt(searchForm.toCityId))
    
    if (!fromCity || !toCity) {
      setSnackbar({
        isOpen: true,
        message: 'Seçilen şehirler bulunamadı. Lütfen tekrar seçin.',
        type: 'error'
      })
      return
    }
    
    if (searchForm.type === 'train') {
      navigate(`/tren?from=${fromCity.CityName}&to=${toCity.CityName}&date=${searchForm.date}`)
    } else {
      navigate(`/otobüs?from=${fromCity.CityName}&to=${toCity.CityName}&date=${searchForm.date}`)
    }
  }

  return (
    <div className="home">
      <div className="container">
        <div className="hero">
          <h1>RayBus ile Yolculuğa Başla</h1>
          <p>
            Tren ve otobüs biletlerinizi kolayca bulun, rezerve edin ve 
            unutulmaz bir yolculuk deneyimi yaşayın.
          </p>
          
          {/* Hızlı Arama Formu */}
          <div className="quick-search card">
            {errorMessage && (
              <div style={{ 
                padding: '10px', 
                marginBottom: '15px', 
                backgroundColor: '#fee', 
                color: '#c33', 
                borderRadius: '5px',
                border: '1px solid #fcc'
              }}>
                ⚠️ {errorMessage}
              </div>
            )}
            <form onSubmit={handleSearch} className="quick-search-form">
              <div className="search-tabs">
                <button
                  type="button"
                  className={`tab ${searchForm.type === 'train' ? 'active' : ''}`}
                  onClick={() => setSearchForm({...searchForm, type: 'train'})}
                >
                  🚄 Tren
                </button>
                <button
                  type="button"
                  className={`tab ${searchForm.type === 'bus' ? 'active' : ''}`}
                  onClick={() => setSearchForm({...searchForm, type: 'bus'})}
                >
                  🚌 Otobüs
                </button>
              </div>
              <div className="search-fields">
                <div className="search-field">
                  <label>Nereden</label>
                  <select
                    name="fromCityId"
                    value={searchForm.fromCityId}
                    onChange={handleChange}
                    required
                    disabled={loadingCities}
                  >
                    <option value="">{loadingCities ? 'Yükleniyor...' : 'Şehir Seçin'}</option>
                    {cities.length > 0 && cities.map(city => {
                      if (!city || !city.CityID || !city.CityName) return null
                      
                      if (citiesWithLocations.length > 0) {
                        const cityWithLoc = citiesWithLocations.find(c => c.CityID === city.CityID)
                        if (cityWithLoc) {
                          const locations = searchForm.type === 'train' ? (cityWithLoc.stations || []) : (cityWithLoc.terminals || [])
                          
                          if (locations && locations.length > 0) {
                            return locations.map((loc, index) => {
                              const locationName = loc.StationName || loc.TerminalName || loc.stationName || loc.terminalName || loc.Station_Name || loc.Terminal_Name
                              return (
                                <option key={`from-${city.CityID}-${index}`} value={city.CityID.toString()}>
                                  {locationName}, {city.CityName}
                                </option>
                              )
                            })
                          }
                        }
                      }
                      
                      return (
                        <option key={`from-${city.CityID}`} value={city.CityID.toString()}>
                          {city.CityName}
                        </option>
                      )
                    })}
                    {!loadingCities && cities.length === 0 && citiesWithLocations.length === 0 && (
                      <option value="" disabled>Şehir bulunamadı</option>
                    )}
                  </select>
                </div>
                <div className="search-field">
                  <label>Nereye</label>
                  <select
                    name="toCityId"
                    value={searchForm.toCityId}
                    onChange={handleChange}
                    required
                    disabled={loadingCities}
                  >
                    <option value="">{loadingCities ? 'Yükleniyor...' : 'Şehir Seçin'}</option>
                    {cities.length > 0 && cities.map(city => {
                      if (!city || !city.CityID || !city.CityName) return null
                      
                      if (citiesWithLocations.length > 0) {
                        const cityWithLoc = citiesWithLocations.find(c => c.CityID === city.CityID)
                        if (cityWithLoc) {
                          const locations = searchForm.type === 'train' ? (cityWithLoc.stations || []) : (cityWithLoc.terminals || [])
                          
                          if (locations && locations.length > 0) {
                            return locations.map((loc, index) => {
                              const locationName = loc.StationName || loc.TerminalName || loc.stationName || loc.terminalName || loc.Station_Name || loc.Terminal_Name
                              return (
                                <option key={`to-${city.CityID}-${index}`} value={city.CityID.toString()}>
                                  {locationName}, {city.CityName}
                                </option>
                              )
                            })
                          }
                        }
                      }
                      
                      return (
                        <option key={`to-${city.CityID}`} value={city.CityID.toString()}>
                          {city.CityName}
                        </option>
                      )
                    })}
                    {!loadingCities && cities.length === 0 && citiesWithLocations.length === 0 && (
                      <option value="" disabled>Şehir bulunamadı</option>
                    )}
                  </select>
                </div>
                <div className="search-field">
                  <label>Tarih</label>
                  <input
                    type="date"
                    name="date"
                    value={searchForm.date}
                    onChange={handleChange}
                    min={new Date().toISOString().split('T')[0]}
                    required
                  />
                </div>
                <button type="submit" className="btn btn-primary search-btn" disabled={loadingCities}>
                  Sefer Ara
                </button>
              </div>
            </form>
          </div>

          <div className="hero-buttons">
            <Link to="/tren" className="btn btn-outline">
              Tüm Tren Seferleri
            </Link>
            <Link to="/otobüs" className="btn btn-outline">
              Tüm Otobüs Seferleri
            </Link>
          </div>
        </div>

        <div className="features section">
          <h2 className="section-title">Neden RayBus?</h2>
          <div className="grid grid-3">
            <div className="card feature-card">
              <div className="feature-icon">⚡</div>
              <h3>Hızlı Rezervasyon</h3>
              <p>
                Sadece birkaç tıkla biletinizi rezerve edin. 
                Hızlı ve kolay rezervasyon sistemi.
              </p>
            </div>
            <div className="card feature-card">
              <div className="feature-icon">🔒</div>
              <h3>Güvenli Ödeme</h3>
              <p>
                Tüm ödemeleriniz SSL sertifikası ile korunur. 
                Güvenli ödeme altyapısı.
              </p>
            </div>
            <div className="card feature-card">
              <div className="feature-icon">📱</div>
              <h3>Mobil Uyumlu</h3>
              <p>
                Her cihazdan erişilebilir, modern ve kullanıcı dostu arayüz. 
                İstediğiniz yerden rezervasyon yapın.
              </p>
            </div>
            <div className="card feature-card">
              <div className="feature-icon">🎫</div>
              <h3>Anında Onay</h3>
              <p>
                Rezervasyonunuz anında onaylanır ve e-posta ile size ulaştırılır. 
                Biletlerinizi kolayca yönetin.
              </p>
            </div>
            <div className="card feature-card">
              <div className="feature-icon">💰</div>
              <h3>En İyi Fiyatlar</h3>
              <p>
                En uygun fiyatları bulun. Özel kampanyalar ve indirimlerden 
                faydalanın.
              </p>
            </div>
            <div className="card feature-card">
              <div className="feature-icon">🔄</div>
              <h3>Kolay İptal</h3>
              <p>
                Rezervasyonlarınızı kolayca iptal edebilir veya değiştirebilirsiniz. 
                Esnek iptal politikası.
              </p>
            </div>
          </div>
        </div>

        <div className="cta-section">
          <div className="card cta-card">
            <h2>Hemen Yolculuğa Başla</h2>
            <p>Binlerce sefer arasından size en uygun olanı bulun</p>
            <div className="cta-buttons">
              <Link to="/tren" className="btn btn-primary">
                Tren Seferlerini Gör
              </Link>
              <Link to="/otobüs" className="btn btn-secondary">
                Otobüs Seferlerini Gör
              </Link>
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

export default Home
