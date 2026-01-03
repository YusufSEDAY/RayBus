import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import Navbar from './components/Navbar'
import ProtectedRoute from './components/ProtectedRoute'
import Home from './pages/Home'
import TrainSearch from './pages/TrainSearch'
import BusSearch from './pages/BusSearch'
import TripDetail from './pages/TripDetail'
import Reservations from './pages/Reservations'
import PurchasedTickets from './pages/PurchasedTickets'
import Profile from './pages/Profile'
import Register from './pages/Register'
import AdminPanel from './pages/AdminPanel'
import CompanyPanel from './pages/CompanyPanel'
import './App.css'

console.log('📱 App component yükleniyor...')

function App() {
  console.log('🔄 App component render ediliyor...')
  
  return (
    <Router>
      <div className="App">
        <Navbar />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/tren" element={<TrainSearch />} />
            <Route path="/trains" element={<TrainSearch />} />
            <Route path="/otobüs" element={<BusSearch />} />
            <Route path="/otobus" element={<BusSearch />} />
            <Route path="/buses" element={<BusSearch />} />
            <Route path="/trip/:id" element={<TripDetail />} />
            <Route path="/sefer/:id" element={<TripDetail />} />
            <Route 
              path="/reservations" 
              element={
                <ProtectedRoute allowedRoles={['Kullanıcı', 'Müşteri']}>
                  <Reservations />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/purchased-tickets" 
              element={
                <ProtectedRoute allowedRoles={['Kullanıcı', 'Müşteri']}>
                  <PurchasedTickets />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/profile" 
              element={
                <ProtectedRoute>
                  <Profile />
                </ProtectedRoute>
              } 
            />
            <Route path="/register" element={<Register />} />
            <Route 
              path="/admin-panel" 
              element={
                <ProtectedRoute allowedRoles={['Admin']}>
                  <AdminPanel />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/company-panel" 
              element={
                <ProtectedRoute allowedRoles={['Şirket']}>
                  <CompanyPanel />
                </ProtectedRoute>
              } 
            />
          </Routes>
        </main>
      </div>
    </Router>
  )
}

export default App

