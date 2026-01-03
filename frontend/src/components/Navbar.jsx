import { useState, useEffect, useRef } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import LoginModal from './LoginModal'
import './Navbar.css'

const Navbar = () => {
  const location = useLocation()
  const navigate = useNavigate()
  const [isLoggedIn, setIsLoggedIn] = useState(false)
  const [isLoginModalOpen, setIsLoginModalOpen] = useState(false)
  const [user, setUser] = useState(null)
  const [showUserDropdown, setShowUserDropdown] = useState(false)
  const dropdownRef = useRef(null)

  useEffect(() => {
    // Sayfa yüklendiğinde localStorage'dan kullanıcı bilgisini kontrol et
    const savedUser = localStorage.getItem('raybus_user')
    if (savedUser) {
      setUser(JSON.parse(savedUser))
      setIsLoggedIn(true)
    }

    // Dropdown dışına tıklanınca kapat
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setShowUserDropdown(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const isActive = (path) => location.pathname === path

  const handleLogin = (userData) => {
    setUser(userData)
    setIsLoggedIn(true)
    localStorage.setItem('raybus_user', JSON.stringify(userData))
  }

  const handleLogout = () => {
    setUser(null)
    setIsLoggedIn(false)
    localStorage.removeItem('raybus_user')
    localStorage.removeItem('raybus_token')
    setShowUserDropdown(false)
    navigate('/')
  }

  const getUserRole = () => {
    return user?.RoleName || user?.roleName || ''
  }

  const shouldShowReservations = () => {
    const role = getUserRole()
    return role === 'Kullanıcı' || role === 'Müşteri'
  }

  const shouldShowAdminPanel = () => {
    return getUserRole() === 'Admin'
  }

  const shouldShowCompanyPanel = () => {
    return getUserRole() === 'Şirket'
  }

  return (
    <>
      <nav className="navbar">
        <div className="navbar-container">
          <Link to="/" className="navbar-logo">
            <img 
              src="/logo.png" 
              alt="RayBus Logo" 
              className="logo-image"
              onError={(e) => {
                // Logo yüklenemezse emoji göster
                e.target.style.display = 'none'
                const icon = e.target.parentElement.querySelector('.logo-icon')
                if (icon) icon.style.display = 'inline-block'
              }}
            />
            <span className="logo-icon" style={{display: 'none'}}>🚄</span>
            <span className="logo-text">RayBus</span>
          </Link>
          <ul className="navbar-menu">
            <li>
              <Link 
                to="/" 
                className={`navbar-link ${isActive('/') ? 'active' : ''}`}
              >
                Ana Sayfa
              </Link>
            </li>
            <li>
              <Link 
                to="/trains" 
                className={`navbar-link ${isActive('/trains') ? 'active' : ''}`}
              >
                Tren Bileti
              </Link>
            </li>
            <li>
              <Link 
                to="/buses" 
                className={`navbar-link ${isActive('/buses') ? 'active' : ''}`}
              >
                Otobüs Bileti
              </Link>
            </li>
            {isLoggedIn ? (
              <>
                {shouldShowReservations() && (
                  <>
                    <li>
                      <Link 
                        to="/reservations" 
                        className={`navbar-link ${isActive('/reservations') ? 'active' : ''}`}
                      >
                        Rezervasyonlarım
                      </Link>
                    </li>
                    <li>
                      <Link 
                        to="/purchased-tickets" 
                        className={`navbar-link ${isActive('/purchased-tickets') ? 'active' : ''}`}
                      >
                        Satın Aldıklarım
                      </Link>
                    </li>
                  </>
                )}
                {shouldShowAdminPanel() && (
                  <li>
                    <Link 
                      to="/admin-panel" 
                      className={`navbar-link ${isActive('/admin-panel') ? 'active' : ''}`}
                    >
                      Panel
                    </Link>
                  </li>
                )}
                {shouldShowCompanyPanel() && (
                  <li>
                    <Link 
                      to="/company-panel" 
                      className={`navbar-link ${isActive('/company-panel') ? 'active' : ''}`}
                    >
                      Panel
                    </Link>
                  </li>
                )}
                <li className="user-menu" ref={dropdownRef}>
                  <div className="user-info">
                    <button 
                      className="user-name-btn"
                      onClick={() => setShowUserDropdown(!showUserDropdown)}
                    >
                      {user?.FullName || user?.fullName || user?.Email || user?.email || 'Kullanıcı'}
                    </button>
                    {showUserDropdown && (
                      <div className="user-dropdown">
                        <Link 
                          to="/profile" 
                          className="dropdown-item"
                          onClick={() => setShowUserDropdown(false)}
                        >
                          👤 Profilim
                        </Link>
                        <button 
                          className="dropdown-item"
                          onClick={handleLogout}
                        >
                          🚪 Çıkış Yap
                        </button>
                      </div>
                    )}
                  </div>
                </li>
              </>
            ) : (
              <li>
                <button 
                  className="btn btn-primary btn-login"
                  onClick={() => setIsLoginModalOpen(true)}
                >
                  Giriş Yap
                </button>
              </li>
            )}
          </ul>
        </div>
      </nav>
      <LoginModal 
        isOpen={isLoginModalOpen}
        onClose={() => setIsLoginModalOpen(false)}
        onLogin={handleLogin}
      />
    </>
  )
}

export default Navbar

