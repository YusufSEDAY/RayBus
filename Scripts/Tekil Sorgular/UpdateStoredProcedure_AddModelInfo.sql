-- =============================================
-- RayBus - Stored Procedure Güncelleme
-- Model Bilgisi Ekleme
-- =============================================
-- Bu script sp_Seferleri_Listele stored procedure'ünü güncelleyerek
-- model bilgisini (BusModel/TrainModel) ve koltuk düzeni (LayoutType) bilgisini ekler

USE [RayBus]
GO

-- Mevcut stored procedure'ü sil
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Seferleri_Listele')
    DROP PROCEDURE sp_Seferleri_Listele;
GO

-- Güncellenmiş stored procedure'ü oluştur
CREATE PROCEDURE sp_Seferleri_Listele
    @NeredenID INT,
    @NereyeID INT,
    @Tarih DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        T.TripID,
        F.CityName AS KalkisSehri,
        TC.CityName AS VarisSehri,
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        T.Price,
        V.VehicleType, 
        V.PlateOrCode AS AracPlakaNo,
        
        -- Model bilgisi (Otobüs veya Tren)
        COALESCE(B.BusModel, TR.TrainModel, '') AS AracModeli,
        
        -- Otobüs için Layout Type
        B.LayoutType AS KoltukDuzeni,
        
        -- Boş koltuk sayısı
        (SELECT COUNT(*) 
         FROM dbo.TripSeats TS 
         WHERE TS.TripID = T.TripID AND TS.IsReserved = 0) AS BosKoltukSayisi,

        -- Kalkış Noktası (Terminal veya İstasyon)
        COALESCE(DT.TerminalName, DS.StationName) AS KalkisNoktasi,
        
        -- Varış Noktası
        COALESCE(ArrTerm.TerminalName, ArrS.StationName) AS VarisNoktasi

    FROM dbo.Trips T
    INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID
    INNER JOIN dbo.Cities TC ON T.ToCityID = TC.CityID
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    
    -- Otobüs bilgisi
    LEFT JOIN dbo.Buses B ON V.VehicleID = B.BusID AND V.VehicleType = 'Bus'
    
    -- Tren bilgisi
    LEFT JOIN dbo.Trains TR ON V.VehicleID = TR.TrainID AND V.VehicleType = 'Train'
    
    -- Terminaller 
    LEFT JOIN dbo.Terminals DT ON T.DepartureTerminalID = DT.TerminalID
    LEFT JOIN dbo.Terminals ArrTerm ON T.ArrivalTerminalID = ArrTerm.TerminalID
    
    -- İstasyonlar
    LEFT JOIN dbo.Stations DS ON T.DepartureStationID = DS.StationID
    LEFT JOIN dbo.Stations ArrS ON T.ArrivalStationID = ArrS.StationID

    WHERE 
        T.FromCityID = @NeredenID 
        AND T.ToCityID = @NereyeID
        AND T.DepartureDate = @Tarih
        AND T.Status = 1
    ORDER BY T.DepartureTime ASC;
END;
GO

PRINT 'sp_Seferleri_Listele stored procedure başarıyla güncellendi!'
PRINT 'Model bilgisi (AracModeli) ve koltuk düzeni (KoltukDuzeni) eklendi.'

