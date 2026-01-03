-- =============================================
-- RayBus - Tüm Lokasyonlar Union View'ı
-- =============================================
-- Terminaller ve istasyonları birleştiren view
-- Kullanım: SELECT * FROM vw_Tum_Lokasyonlar_Union

USE [RayBus]
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Tum_Lokasyonlar_Union')
    DROP VIEW vw_Tum_Lokasyonlar_Union;
GO

CREATE VIEW vw_Tum_Lokasyonlar_Union
AS
SELECT 
    CityID,
    TerminalName AS YerAdi,
    'Otobüs Terminali' AS YerTipi,
    Address
FROM dbo.Terminals

UNION ALL -- İki tabloyu alt alta yapıştırır

SELECT 
    CityID,
    StationName AS YerAdi,
    'Tren Garı' AS YerTipi,
    Address
FROM dbo.Stations;
GO

PRINT 'vw_Tum_Lokasyonlar_Union view''ı başarıyla oluşturuldu!';
GO

