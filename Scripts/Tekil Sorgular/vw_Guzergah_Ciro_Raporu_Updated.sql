-- =============================================
-- RayBus - Güzergah Ciro Raporu View'ı (Güncellenmiş)
-- =============================================
-- Güzergah bazlı ciro raporu
-- Kullanım: SELECT * FROM vw_Guzergah_Ciro_Raporu
-- Güncelleme: Payments.Status = 'Completed' kontrolü eklendi

USE [RayBus]
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Guzergah_Ciro_Raporu')
    DROP VIEW vw_Guzergah_Ciro_Raporu;
GO

CREATE VIEW vw_Guzergah_Ciro_Raporu
AS
SELECT 
    F.CityName + ' - ' + T.CityName AS Guzergah,
    V.VehicleType AS AracTipi,
    COUNT(R.ReservationID) AS ToplamSatisAdedi,
    ISNULL(SUM(CASE WHEN P.Status = 'Completed' THEN P.Amount ELSE 0 END), 0) AS ToplamCiro,
    AVG(CASE WHEN P.Status = 'Completed' THEN P.Amount ELSE NULL END) AS OrtalamaBiletFiyati

FROM dbo.Reservations R
INNER JOIN dbo.Trips Trip ON R.TripID = Trip.TripID
INNER JOIN dbo.Cities F ON Trip.FromCityID = F.CityID -- Nereden
INNER JOIN dbo.Cities T ON Trip.ToCityID = T.CityID -- Nereye
INNER JOIN dbo.Vehicles V ON Trip.VehicleID = V.VehicleID
LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID AND P.Status = 'Completed'

WHERE R.Status <> 'Cancelled' -- İptal edilenleri ciroya katma
GROUP BY F.CityName, T.CityName, V.VehicleType;
GO

PRINT 'vw_Guzergah_Ciro_Raporu view''ı başarıyla oluşturuldu! (Güncellenmiş)';
GO

