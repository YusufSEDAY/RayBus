-- NotificationQueue kontrol sorgusu
USE RayBusDB;
GO

-- Son 10 bildirimi göster
SELECT TOP 10
    NotificationID,
    UserID,
    NotificationType,
    NotificationMethod,
    Status,
    Subject,
    RelatedReservationID,
    CreatedAt,
    SentAt,
    RetryCount,
    ErrorMessage
FROM dbo.NotificationQueue
ORDER BY CreatedAt DESC;
GO

-- Payment tipindeki bekleyen bildirimleri göster
SELECT 
    NQ.NotificationID,
    NQ.UserID,
    U.Email,
    U.FullName,
    NQ.NotificationType,
    NQ.Status,
    NQ.RelatedReservationID,
    NQ.CreatedAt,
    NQ.RetryCount,
    NQ.ErrorMessage
FROM dbo.NotificationQueue NQ
INNER JOIN dbo.Users U ON NQ.UserID = U.UserID
WHERE NQ.NotificationType = 'Payment'
ORDER BY NQ.CreatedAt DESC;
GO

-- Kullanıcı email tercihlerini kontrol et
SELECT 
    U.UserID,
    U.Email,
    U.FullName,
    UNP.EmailNotifications,
    UNP.PaymentNotifications
FROM dbo.Users U
LEFT JOIN dbo.UserNotificationPreferences UNP ON U.UserID = UNP.UserID
WHERE U.Email IS NOT NULL;
GO

