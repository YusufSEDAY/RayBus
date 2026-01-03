import { useEffect } from 'react'
import './Snackbar.css'

const Snackbar = ({ message, type = 'success', isOpen, onClose, duration = 3000 }) => {
  useEffect(() => {
    if (isOpen && duration > 0) {
      const timer = setTimeout(() => {
        onClose()
      }, duration)

      return () => clearTimeout(timer)
    }
  }, [isOpen, duration, onClose])

  if (!isOpen) return null

  return (
    <div className={`snackbar snackbar-${type} ${isOpen ? 'snackbar-open' : ''}`}>
      <div className="snackbar-content">
        <span className="snackbar-icon">
          {type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️'}
        </span>
        <span className="snackbar-message">{message}</span>
        <button className="snackbar-close" onClick={onClose}>
          ✕
        </button>
      </div>
    </div>
  )
}

export default Snackbar
