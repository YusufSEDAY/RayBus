import axios from 'axios'

// Vite proxy kullanıyoruz, bu yüzden relative path kullanıyoruz
// Vite config'de /api -> http://localhost:5000 proxy'si var
const API_BASE_URL = '/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 saniye timeout
  // Vite proxy kullanıyoruz, adapter'ı belirtmiyoruz (default kullanılacak)
})

// Request interceptor - Optimize edildi: Sadece kritik hatalar loglanıyor
api.interceptors.request.use(
  (config) => {
    // baseURL'i kontrol et - mutlaka relative path olmalı (/api)
    if (config.baseURL && (config.baseURL.startsWith('http://') || config.baseURL.startsWith('https://'))) {
      config.baseURL = '/api'
    }
    
    // URL'i kontrol et - absolute path ise relative yap
    if (config.url && (config.url.startsWith('http://') || config.url.startsWith('https://'))) {
      const urlObj = new URL(config.url)
      config.url = urlObj.pathname + urlObj.search
    }
    
    // JWT kaldırıldı - Token yönetimi devre dışı
    
    return config
  },
  (error) => {
    // Sadece gerçek hataları logla
    if (process.env.NODE_ENV === 'development') {
      console.error('API İstek Hatası:', error)
    }
    return Promise.reject(error)
  }
)

// Response interceptor - Optimize edildi: Sadece hatalar loglanıyor
api.interceptors.response.use(
  (response) => {
    // JWT kaldırıldı - Token kaydetme devre dışı
    return response
  },
  (error) => {
    // Sadece development'ta ve gerçek hatalarda logla
    if (process.env.NODE_ENV === 'development') {
      if (error.response) {
        // Sadece 4xx ve 5xx hatalarını logla
        if (error.response.status >= 400) {
          console.error(`API Hatası [${error.response.status}]:`, error.config?.url, error.message)
        }
      } else if (error.request) {
        console.error('Sunucuya ulaşılamadı:', error.config?.url)
      }
    }
    return Promise.reject(error)
  }
)

// Tren API'leri
export const trainAPI = {
  getAll: () => api.get('/train'),
  getById: (id) => api.get(`/train/${id}`),
  search: (from, to, date) => api.get('/train/search', { params: { from, to, date } }),
}

// Otobüs API'leri
export const busAPI = {
  getAll: () => api.get('/bus'),
  getById: (id) => api.get(`/bus/${id}`),
  search: (from, to, date) => api.get('/bus/search', { params: { from, to, date } }),
}

// Rezervasyon API'leri
export const reservationAPI = {
  getAll: () => api.get('/reservation'),
  getById: (id) => api.get(`/reservation/${id}`),
  create: (data) => api.post('/reservation', data),
  cancel: (id, cancelDto) => api.delete(`/reservation/${id}`, { data: cancelDto }),
  getByUserId: (userId) => api.get(`/reservation/user/${userId}`),
  getCancellationReasons: () => api.get('/reservation/cancellation-reasons'),
  createCancellationReason: (reasonText) => api.post('/reservation/cancellation-reasons', { ReasonText: reasonText }),
  completePayment: (data) => api.post('/reservation/complete-payment', data),
}

// Sefer API'leri
export const tripAPI = {
  getDetail: (id) => api.get(`/trip/${id}`),
}

// Şehir ve İstasyon API'leri
export const cityAPI = {
  getAll: () => api.get('/city'),
  getStations: (cityId) => api.get(`/city/${cityId}/stations`),
  getTerminals: (cityId) => api.get(`/city/${cityId}/terminals`),
}

// Otomatik İptal API'leri
export const autoCancellationAPI = {
  process: (timeoutMinutes = 15) => api.post('/AutoCancellation/process', null, { params: { timeoutMinutes } }),
  getSettings: () => api.get('/AutoCancellation/settings'),
  updateSettings: (timeoutMinutes) => api.put('/AutoCancellation/settings', timeoutMinutes),
  getLogs: (userId = null) => api.get('/AutoCancellation/logs', { params: userId ? { userId } : {} }),
}

// Kullanıcı İstatistikleri API'leri
export const userStatisticsAPI = {
  getStatistics: (userId) => api.get(`/UserStatistics/${userId}`),
  getReport: (userId) => api.get(`/UserStatistics/${userId}/report`),
}

// Bilet API'leri
export const ticketAPI = {
  getByReservationId: (reservationId) => api.get(`/Ticket/reservation/${reservationId}`),
  getByTicketNumber: (ticketNumber) => api.get(`/Ticket/ticket-number/${ticketNumber}`),
  generatePDF: (reservationId) => api.get(`/Ticket/pdf/${reservationId}`, { responseType: 'blob' }),
}

// Bildirim API'leri
export const notificationAPI = {
  send: (data) => api.post('/Notification/send', data),
  getPending: (maxCount = 100) => api.get('/Notification/pending', { params: { maxCount } }),
  updateStatus: (data) => api.put('/Notification/status', data),
  getUserPreferences: (userId) => api.get(`/Notification/preferences/${userId}`),
  updateUserPreferences: (userId, preferences) => api.put(`/Notification/preferences/${userId}`, preferences),
  getUserNotifications: (userId, limit = null) => api.get(`/Notification/user/${userId}`, { params: limit ? { limit } : {} }),
}

// Kullanıcı API'leri
export const userAPI = {
  login: (data) => api.post('/user/login', data),
  register: (data) => api.post('/user/register', data),
  getUser: (id) => api.get(`/user/${id}`),
  updateProfile: (id, data) => api.put(`/user/${id}/profile`, data),
}

// Admin API'leri
export const adminAPI = {
  getAllUsers: (aramaMetni, rolID) => {
    const params = {}
    if (aramaMetni) params.aramaMetni = aramaMetni
    if (rolID) params.rolID = rolID
    return api.get('/admin/users', { params })
  },
  getUserById: (id) => api.get(`/admin/users/${id}`),
  updateUserStatus: (id, data) => api.put(`/admin/users/${id}/status`, data),
  updateUser: (id, data) => api.put(`/admin/users/${id}`, data),
  deleteUser: (id) => api.delete(`/admin/users/${id}`),
  getAllReservations: () => api.get('/admin/reservations'),
  updateReservationStatus: (id, data) => api.put(`/admin/reservations/${id}/status`, data),
  cancelReservation: (id, data) => api.post(`/admin/reservations/${id}/cancel`, data),
  getAllTrips: () => api.get('/admin/trips'),
  updateTripStatus: (id, data) => api.put(`/admin/trips/${id}/status`, data),
  getDashboardStats: () => api.get('/admin/dashboard/stats'),
  addVehicle: (data) => api.post('/admin/vehicles', data),
  updateVehicle: (id, data) => api.put(`/admin/vehicles/${id}`, data),
  getAllVehicles: (vehicleType, companyID) => {
    const params = {}
    if (vehicleType) params.vehicleType = vehicleType
    if (companyID !== null && companyID !== undefined) {
      params.companyID = companyID
    }
    return api.get('/admin/vehicles', { params })
  },
  getAllCompanies: () => api.get('/admin/companies'),
  addTrip: (data) => api.post('/admin/trips', data),
  updateTrip: (id, data) => api.put(`/admin/trips/${id}`, data),
  cancelTrip: (id, data) => api.post(`/admin/trips/${id}/cancel`, data),
  getDailyFinancialReport: (startDate, endDate) => api.get('/admin/reports/daily-financial', { params: { startDate, endDate } }),
  getRouteRevenueReport: () => api.get('/admin/reports/route-revenue'),
  getTripDetails: (tripId = null) => {
    const params = {}
    if (tripId) params.tripId = tripId
    return api.get('/admin/trips/details', { params })
  },
}

// Şirket API'leri
export const companyAPI = {
  getMyTrips: (queryParams = '') => api.get(`/company/trips${queryParams}`),
  createTrip: (data) => api.post('/company/trips', data),
  getTripById: (id) => api.get(`/company/trips/${id}`),
  updateTrip: (id, data) => api.put(`/company/trips/${id}`, data),
  deleteTrip: (id) => api.delete(`/company/trips/${id}`),
  cancelTrip: (id, data) => api.post(`/company/trips/${id}/cancel`, data),
  getCompanyStats: () => api.get('/company/dashboard/stats'),
  getVehicles: (vehicleType) => api.get('/company/vehicles', { params: vehicleType ? { vehicleType } : {} }),
}

export default api


