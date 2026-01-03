-- =============================================
-- RayBus - Sefer Detayları View'ı
-- =============================================
-- Sefer detaylarını birleştiren view
-- Kullanım: SELECT * FROM vw_Sefer_Detaylari

USE [RayBus]
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Sefer_Detaylari')
    DROP VIEW vw_Sefer_Detaylari;
GO

CREATE VIEW vw_Sefer_Detaylari
AS
SELECT 
    T.TripID,
    T.DepartureDate,
    T.DepartureTime,
    
    -- Güzergah
    F.CityName AS Nereden,
    TC.CityName AS Nereye,
    
    -- Araç Bilgisi
    V.VehicleType AS AracTipi,
    V.PlateOrCode AS PlakaNo,
    
    -- Finansal
    T.Price AS BiletFiyati,
    
    -- Doluluk Durumu (Hesaplanmış Alan)
    (SELECT COUNT(*) FROM dbo.TripSeats TS WHERE TS.TripID = T.TripID) AS ToplamKoltuk,
    (SELECT COUNT(*) FROM dbo.TripSeats TS WHERE TS.TripID = T.TripID AND TS.IsReserved = 1) AS SatilanKoltuk,
    
    -- Durum
    CASE 
        WHEN T.Status = 1 THEN 'Aktif' 
        ELSE 'İptal' 
    END AS SeferDurumu

FROM dbo.Trips T
INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID
INNER JOIN dbo.Cities TC ON T.ToCityID = TC.CityID
INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID;
GO

PRINT 'vw_Sefer_Detaylari view''ı başarıyla oluşturuldu!';
GO

