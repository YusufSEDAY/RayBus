-- =============================================
-- View: vw_Admin_Seferler
-- Açıklama: Admin paneli için tüm seferleri detaylı bilgilerle getirir
-- Performans: Karmaşık Include sorguları yerine tek view kullanır
-- =============================================

-- Önce view'i drop et (varsa)
IF OBJECT_ID('dbo.vw_Admin_Seferler', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Admin_Seferler;
GO

CREATE VIEW dbo.vw_Admin_Seferler
AS
SELECT 
    T.TripID,
    T.VehicleID,
    V.PlateOrCode AS VehiclePlate,
    V.VehicleType,
    T.FromCityID,
    FromCity.CityName AS FromCityName,
    T.ToCityID,
    ToCity.CityName AS ToCityName,
    T.DepartureTerminalID,
    DT.TerminalName AS DepartureTerminalName,
    T.ArrivalTerminalID,
    AT.TerminalName AS ArrivalTerminalName,
    T.DepartureStationID,
    DS.StationName AS DepartureStationName,
    T.ArrivalStationID,
    AS_Station.StationName AS ArrivalStationName,
    T.DepartureDate,
    T.DepartureTime,
    T.ArrivalDate,
    T.ArrivalTime,
    T.Price,
    T.Status,
    CASE 
        WHEN T.Status = 1 THEN 'Aktif'
        WHEN T.Status = 0 THEN 'İptal'
        ELSE 'Bilinmiyor'
    END AS StatusText,
    T.CreatedAt,
    T.UpdatedAt,
    -- Güzergah bilgisi
    CASE 
        WHEN FromCity.CityName IS NOT NULL AND ToCity.CityName IS NOT NULL 
        THEN FromCity.CityName + ' > ' + ToCity.CityName
        ELSE 'Güzergah Bilgisi Yok'
    END AS Route,
    -- Şirket bilgisi
    V.CompanyID,
    U.FullName AS CompanyName
FROM dbo.Trips T
INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
LEFT JOIN dbo.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN dbo.Cities ToCity ON T.ToCityID = ToCity.CityID
LEFT JOIN dbo.Terminals DT ON T.DepartureTerminalID = DT.TerminalID
LEFT JOIN dbo.Terminals AT ON T.ArrivalTerminalID = AT.TerminalID
LEFT JOIN dbo.Stations DS ON T.DepartureStationID = DS.StationID
LEFT JOIN dbo.Stations AS_Station ON T.ArrivalStationID = AS_Station.StationID
LEFT JOIN dbo.Users U ON V.CompanyID = U.UserID;
GO

-- View'i test et
-- SELECT * FROM dbo.vw_Admin_Seferler ORDER BY DepartureDate DESC;
GO

