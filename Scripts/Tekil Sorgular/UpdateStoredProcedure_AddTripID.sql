-- =============================================
-- sp_Kullanici_Biletleri Stored Procedure Güncelleme
-- TripID kolonu eklendi (Detayları Gör butonu için gerekli)
-- =============================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Kullanici_Biletleri')
    DROP PROCEDURE sp_Kullanici_Biletleri;
GO

CREATE PROCEDURE sp_Kullanici_Biletleri
    @KullaniciID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        R.ReservationID,
        T.TripID, -- Sefer ID'si eklendi (Detayları Gör butonu için gerekli)
        
        -- Güzergah Bilgisi
        F.CityName + ' > ' + T_City.CityName AS Guzergah,
        
        -- Tarih ve Saat
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        
        -- Araç ve Koltuk
        V.VehicleType, -- Otobüs/Tren
        V.PlateOrCode,
        S.SeatNo,
        
        -- Finansal ve Durum
        P.Amount AS OdenenTutar,
        R.Status AS RezervasyonDurumu, -- 'Reserved', 'Cancelled'
        R.PaymentStatus,
        R.ReservationDate AS IslemTarihi

    FROM dbo.Reservations R
    INNER JOIN dbo.Trips T ON R.TripID = T.TripID
    INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID      -- Nereden
    INNER JOIN dbo.Cities T_City ON T.ToCityID = T_City.CityID -- Nereye
    INNER JOIN dbo.Seats S ON R.SeatID = S.SeatID
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID

    WHERE R.UserID = @KullaniciID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC; -- En yeni sefer en üstte
END;
GO

