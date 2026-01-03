-- =============================================
-- View: vw_Admin_Rezervasyonlar
-- Açıklama: Admin paneli için tüm rezervasyonları detaylı bilgilerle getirir
-- Performans: Karmaşık Include sorguları yerine tek view kullanır
-- =============================================

-- Önce view'i drop et (varsa)
IF OBJECT_ID('dbo.vw_Admin_Rezervasyonlar', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Admin_Rezervasyonlar;
GO

CREATE VIEW dbo.vw_Admin_Rezervasyonlar
AS
SELECT 
    R.ReservationID,
    R.UserID,
    U.FullName AS UserName,
    R.TripID,
    CASE 
        WHEN FromCity.CityName IS NOT NULL AND ToCity.CityName IS NOT NULL 
        THEN FromCity.CityName + ' - ' + ToCity.CityName
        ELSE 'Sefer #' + CAST(R.TripID AS VARCHAR(10))
    END AS TripRoute,
    R.SeatID,
    CASE 
        WHEN S.SeatNo IS NOT NULL 
        THEN S.SeatNo
        ELSE 'Koltuk #' + CAST(R.SeatID AS VARCHAR(10))
    END AS SeatNo,
    R.Status,
    R.PaymentStatus,
    R.ReservationDate
FROM dbo.Reservations R
INNER JOIN dbo.Users U ON R.UserID = U.UserID
LEFT JOIN dbo.TripSeats TS ON R.TripID = TS.TripID AND R.SeatID = TS.SeatID
LEFT JOIN dbo.Trips T ON TS.TripID = T.TripID
LEFT JOIN dbo.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN dbo.Cities ToCity ON T.ToCityID = ToCity.CityID
LEFT JOIN dbo.Seats S ON TS.SeatID = S.SeatID;
GO

-- View'i test et
-- SELECT * FROM dbo.vw_Admin_Rezervasyonlar ORDER BY ReservationDate DESC;
GO

