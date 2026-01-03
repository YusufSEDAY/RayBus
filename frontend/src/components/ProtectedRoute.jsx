import { Navigate } from 'react-router-dom'

const ProtectedRoute = ({ children, allowedRoles }) => {
  const userStr = localStorage.getItem('raybus_user')
  
  if (!userStr) {
    console.warn('⚠️ ProtectedRoute: Kullanıcı bilgisi bulunamadı')
    return <Navigate to="/" replace />
  }

  let user
  try {
    user = JSON.parse(userStr)
  } catch (e) {
    console.error('❌ ProtectedRoute: User parse hatası:', e)
    return <Navigate to="/" replace />
  }

  if (!user) {
    console.warn('⚠️ ProtectedRoute: User null')
    return <Navigate to="/" replace />
  }

  if (allowedRoles && allowedRoles.length > 0) {
    let userRole = user.RoleName || user.roleName || user.Role || user.role
    
    console.log('🔍 ProtectedRoute Debug:', {
      userRole,
      allowedRoles,
      user: user,
      userKeys: Object.keys(user)
    })
    
    // Eğer role bulunamadıysa JWT token'dan çıkarmayı dene
    if (!userRole) {
      console.warn('⚠️ ProtectedRoute: Role localStorage\'da yok, token\'dan çıkarılıyor...')
      const token = localStorage.getItem('raybus_token')
      if (token) {
        try {
          const tokenParts = token.split('.')
          if (tokenParts.length === 3) {
            const payload = JSON.parse(atob(tokenParts[1]))
            // JWT'de role claim'i farklı formatlarda olabilir
            userRole = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 
                      payload['role'] || 
                      payload['Role'] ||
                      payload['roleName'] ||
                      payload['RoleName'] ||
                      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] // Fallback
            
            console.log('🔍 Token payload:', payload)
            console.log('🔍 Token\'dan role bulundu:', userRole)
            
            if (userRole) {
              // Token'dan role bulundu, localStorage'ı güncelle
              const updatedUser = {
                ...user,
                RoleName: userRole,
                roleName: userRole
              }
              localStorage.setItem('raybus_user', JSON.stringify(updatedUser))
              console.log('✅ localStorage güncellendi:', updatedUser)
            }
          }
        } catch (e) {
          console.error('❌ Token decode hatası:', e)
        }
      }
    }
    
    if (!userRole) {
      console.error('❌ ProtectedRoute: Role bilgisi hiçbir yerden alınamadı')
      return <Navigate to="/" replace />
    }
    
    // Case-insensitive karşılaştırma
    const normalizedUserRole = userRole.trim()
    const normalizedAllowedRoles = allowedRoles.map(r => r.trim())
    
    if (!normalizedAllowedRoles.some(role => role.toLowerCase() === normalizedUserRole.toLowerCase())) {
      console.warn('⚠️ ProtectedRoute: Yetki yok. Gerekli:', allowedRoles, 'Mevcut:', userRole)
      return <Navigate to="/" replace />
    }
    
    console.log('✅ ProtectedRoute: Yetki kontrolü başarılı')
  }

  return children
}

export default ProtectedRoute
