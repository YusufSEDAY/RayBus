import { useState } from 'react'
import Snackbar from './Snackbar'
import './PaymentModal.css'

const PaymentModal = ({ isOpen, onClose, onConfirm, amount, loading = false }) => {
  const [formData, setFormData] = useState({
    cardNumber: '',
    cardHolder: '',
    expiryMonth: '',
    expiryYear: '',
    cvv: ''
  })
  const [errors, setErrors] = useState({})
  const [snackbar, setSnackbar] = useState({ isOpen: false, message: '', type: 'success' })

  if (!isOpen) return null

  console.log('🔍 PaymentModal render:', { amount, amountType: typeof amount })

  const validateCardNumber = (number) => {
    const cleaned = number.replace(/\s/g, '')
    return /^\d{13,19}$/.test(cleaned)
  }

  const formatCardNumber = (value) => {
    const cleaned = value.replace(/\D/g, '')
    const groups = cleaned.match(/.{1,4}/g) || []
    return groups.join(' ').substring(0, 19) // Maksimum 16 rakam + 3 boşluk
  }

  const handleCardNumberChange = (e) => {
    const formatted = formatCardNumber(e.target.value)
    setFormData(prev => ({ ...prev, cardNumber: formatted }))
    
    if (formatted.replace(/\s/g, '').length > 0) {
      if (!validateCardNumber(formatted)) {
        setErrors(prev => ({ ...prev, cardNumber: 'Geçerli bir kart numarası giriniz (13-19 rakam)' }))
      } else {
        setErrors(prev => {
          const newErrors = { ...prev }
          delete newErrors.cardNumber
          return newErrors
        })
      }
    } else {
      setErrors(prev => {
        const newErrors = { ...prev }
        delete newErrors.cardNumber
        return newErrors
      })
    }
  }

  const handleExpiryChange = (e, type) => {
    const value = e.target.value.replace(/\D/g, '')
    if (type === 'month') {
      const monthValue = value.substring(0, 2)
      if (monthValue === '') {
        setFormData(prev => ({ ...prev, expiryMonth: '' }))
      } else {
        const month = parseInt(monthValue)
        if (month >= 1 && month <= 12) {
          setFormData(prev => ({ ...prev, expiryMonth: monthValue }))
        } else if (monthValue.length === 1) {
          setFormData(prev => ({ ...prev, expiryMonth: monthValue }))
        }
      }
    } else if (type === 'year') {
      const year = value.substring(0, 2)
      setFormData(prev => ({ ...prev, expiryYear: year }))
    }
  }

  const handleExpiryBlur = (type) => {
    if (type === 'month') {
      const month = parseInt(formData.expiryMonth) || 0
      if (month >= 1 && month <= 12) {
        setFormData(prev => ({ ...prev, expiryMonth: month.toString().padStart(2, '0') }))
      } else if (formData.expiryMonth !== '') {
        setFormData(prev => ({ ...prev, expiryMonth: '' }))
      }
    }
  }

  const handleCvvChange = (e) => {
    const value = e.target.value.replace(/\D/g, '').substring(0, 4)
    setFormData(prev => ({ ...prev, cvv: value }))
  }

  const validateForm = () => {
    const newErrors = {}

    if (!formData.cardNumber || !validateCardNumber(formData.cardNumber)) {
      newErrors.cardNumber = 'Geçerli bir kart numarası giriniz'
    }

    if (!formData.cardHolder || formData.cardHolder.trim().length < 3) {
      newErrors.cardHolder = 'Kart sahibi adı en az 3 karakter olmalıdır'
    }

    if (!formData.expiryMonth || !formData.expiryYear) {
      newErrors.expiry = 'Son kullanma tarihi eksik'
    } else {
      const month = parseInt(formData.expiryMonth)
      const year = parseInt('20' + formData.expiryYear)
      const expiryDate = new Date(year, month - 1)
      const now = new Date()
      
      if (expiryDate < now) {
        newErrors.expiry = 'Kartın son kullanma tarihi geçmiş'
      }
    }

    if (!formData.cvv || formData.cvv.length < 3) {
      newErrors.cvv = 'CVV en az 3 rakam olmalıdır'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    
    if (validateForm()) {
      const last4Digits = formData.cardNumber.replace(/\s/g, '').slice(-4)
      
      const paymentInfo = {
        cardNumber: last4Digits,
        cardHolder: formData.cardHolder.trim(),
        expiryMonth: formData.expiryMonth,
        expiryYear: formData.expiryYear,
        maskedCardNumber: `**** **** **** ${last4Digits}`
      }

      onConfirm(paymentInfo)
      setSnackbar({
        isOpen: true,
        message: 'Ödeme bilgileri alındı. İşlem tamamlanıyor...',
        type: 'success'
      })
    } else {
      const firstError = Object.values(newErrors)[0]
      if (firstError) {
        setSnackbar({
          isOpen: true,
          message: firstError,
          type: 'error'
        })
      }
    }
  }

  const handleClose = () => {
    setFormData({
      cardNumber: '',
      cardHolder: '',
      expiryMonth: '',
      expiryYear: '',
      cvv: ''
    })
    setErrors({})
    onClose()
  }

  return (
    <div className="payment-modal-overlay" onClick={handleClose}>
      <div className="payment-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="payment-modal-header">
          <h2>💳 Ödeme Bilgileri</h2>
          <button className="payment-modal-close" onClick={handleClose}>×</button>
        </div>
        
        <div className="payment-modal-body">
          <div className="payment-amount">
            <span className="amount-label">Ödeme Tutarı:</span>
            <span className="amount-value">
              {amount && amount > 0 ? parseFloat(amount).toFixed(2) : '0.00'} ₺
            </span>
          </div>

          <form onSubmit={handleSubmit} className="payment-form">
            <div className="form-group">
              <label htmlFor="cardNumber">Kart Numarası</label>
              <input
                type="text"
                id="cardNumber"
                placeholder="1234 5678 9012 3456"
                value={formData.cardNumber}
                onChange={handleCardNumberChange}
                maxLength={19}
                className={errors.cardNumber ? 'error' : ''}
                disabled={loading}
              />
              {errors.cardNumber && <span className="error-message">{errors.cardNumber}</span>}
            </div>

            <div className="form-group">
              <label htmlFor="cardHolder">Kart Sahibi Adı</label>
              <input
                type="text"
                id="cardHolder"
                placeholder="AD SOYAD"
                value={formData.cardHolder}
                onChange={(e) => setFormData(prev => ({ ...prev, cardHolder: e.target.value.toUpperCase() }))}
                className={errors.cardHolder ? 'error' : ''}
                disabled={loading}
              />
              {errors.cardHolder && <span className="error-message">{errors.cardHolder}</span>}
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="expiryMonth">Son Kullanma Tarihi</label>
                <div className="expiry-inputs">
                  <input
                    type="text"
                    id="expiryMonth"
                    placeholder="AA"
                    value={formData.expiryMonth}
                    onChange={(e) => handleExpiryChange(e, 'month')}
                    onBlur={() => handleExpiryBlur('month')}
                    maxLength={2}
                    className={errors.expiry ? 'error' : ''}
                    disabled={loading}
                  />
                  <span>/</span>
                  <input
                    type="text"
                    id="expiryYear"
                    placeholder="YY"
                    value={formData.expiryYear}
                    onChange={(e) => handleExpiryChange(e, 'year')}
                    maxLength={2}
                    className={errors.expiry ? 'error' : ''}
                    disabled={loading}
                  />
                </div>
                {errors.expiry && <span className="error-message">{errors.expiry}</span>}
              </div>

              <div className="form-group">
                <label htmlFor="cvv">CVV</label>
                <input
                  type="text"
                  id="cvv"
                  placeholder="123"
                  value={formData.cvv}
                  onChange={handleCvvChange}
                  maxLength={4}
                  className={errors.cvv ? 'error' : ''}
                  disabled={loading}
                />
                {errors.cvv && <span className="error-message">{errors.cvv}</span>}
              </div>
            </div>

            <div className="payment-modal-actions">
              <button
                type="button"
                className="btn btn-outline"
                onClick={handleClose}
                disabled={loading}
              >
                İptal
              </button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={loading}
              >
                {loading ? (
                  <>
                    <span className="spinner"></span>
                    Ödeme Yapılıyor...
                  </>
                ) : (
                  '💳 Ödemeyi Tamamla'
                )}
              </button>
            </div>
          </form>

          <div className="payment-info">
            <p className="info-text">
              🔒 Bu bir simülasyon ödeme sayfasıdır. Gerçek ödeme işlemi yapılmamaktadır.
            </p>
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

export default PaymentModal

