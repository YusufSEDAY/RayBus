-- =============================================
-- View: vw_Sirket_Seferleri
-- Açıklama: Şirket paneli için şirkete ait seferleri detaylı bilgilerle getirir
-- Parametre: WHERE CompanyID = @SirketID ile filtrelenir
-- Performans: Karmaşık Include sorguları yerine tek view kullanır
-- =============================================

-- Önce view'i drop et (varsa)
IF OBJECT_ID('dbo.vw_Sirket_Seferleri', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Sirket_Seferleri;
GO

CREATE VIEW dbo.vw_Sirket_Seferleri
AS
SELECT 
    T.TripID,
    V.PlateOrCode AS AracPlaka,
    CASE 
        WHEN FromCity.CityName IS NOT NULL AND ToCity.CityName IS NOT NULL 
        THEN FromCity.CityName + ' > ' + ToCity.CityName
        ELSE 'Güzergah Bilgisi Yok'
    END AS Guzergah,
    T.DepartureDate AS Tarih,
    T.DepartureTime AS Saat,
    T.Price AS Fiyat,
    CASE 
        WHEN T.Status = 1 THEN 'Aktif'
        WHEN T.Status = 0 THEN 'İptal'
        ELSE 'Bilinmiyor'
    END AS Durum,
    -- Koltuk bilgileri
    (SELECT COUNT(*) FROM dbo.TripSeats TS WHERE TS.TripID = T.TripID AND TS.IsReserved = 1) AS DoluKoltukSayisi,
    (SELECT COUNT(*) FROM dbo.Seats S WHERE S.VehicleID = V.VehicleID AND S.IsActive = 1) AS ToplamKoltuk,
    -- Ekstra alanlar
    T.FromCityID,
    FromCity.CityName AS FromCity,
    T.ToCityID,
    ToCity.CityName AS ToCity,
    V.VehicleType,
    T.Status,
    T.DepartureDate,
    T.DepartureTime,
    V.CompanyID
FROM dbo.Trips T
INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
LEFT JOIN dbo.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN dbo.Cities ToCity ON T.ToCityID = ToCity.CityID;
GO

-- View'i test et (örnek: SirketID = 11 için)
-- SELECT * FROM dbo.vw_Sirket_Seferleri WHERE CompanyID = 11 ORDER BY Tarih DESC, Saat DESC;
GO

