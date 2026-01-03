-- =============================================
-- Stored Procedure: sp_Kullanici_Biletleri (Güncellenmiş)
-- Açıklama: Kullanıcının tüm biletlerini getirir - Trip fiyatı eklendi
-- Güncelleme: Trip.Price alanı eklendi (ekstra sorgu gereksin)
-- =============================================

-- Önce stored procedure'ü drop et (varsa)
IF OBJECT_ID('dbo.sp_Kullanici_Biletleri', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Kullanici_Biletleri;
GO

CREATE PROCEDURE sp_Kullanici_Biletleri
    @KullaniciID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        R.ReservationID,
        T.TripID,
        
        -- Güzergah Bilgisi
        F.CityName + ' > ' + T_City.CityName AS Guzergah,
        
        -- Tarih ve Saat
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        
        -- Araç ve Koltuk
        V.VehicleType,
        V.PlateOrCode,
        S.SeatNo,
        
        -- Finansal ve Durum
        P.Amount AS OdenenTutar,
        T.Price AS TripFiyati, -- YENİ: Trip fiyatı eklendi (ekstra sorgu gereksin)
        R.Status AS RezervasyonDurumu,
        R.PaymentStatus,
        R.ReservationDate AS IslemTarihi

    FROM dbo.Reservations R
    INNER JOIN dbo.Trips T ON R.TripID = T.TripID
    INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID
    INNER JOIN dbo.Cities T_City ON T.ToCityID = T_City.CityID
    INNER JOIN dbo.Seats S ON R.SeatID = S.SeatID
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID AND P.Status = 'Completed'

    WHERE R.UserID = @KullaniciID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
END;
GO

-- Stored procedure'ü test et
-- EXEC sp_Kullanici_Biletleri @KullaniciID = 1;
GO

