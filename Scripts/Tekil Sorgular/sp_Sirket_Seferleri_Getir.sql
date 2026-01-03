-- =============================================
-- Stored Procedure: sp_Sirket_Seferleri_Getir
-- Açıklama: Şirket panelinde şirkete ait seferleri getirir (dolu koltuk sayısı ve kapasite ile)
-- Güncelleme: Admin tarafından oluşturulan seferleri de gösterir (CompanyID NULL olan araçlar)
-- Parametreler:
--   @SirketID: Şirket ID (JWT token'dan alınır)
-- =============================================

CREATE OR ALTER PROCEDURE sp_Sirket_Seferleri_Getir
    @SirketID INT -- Giriş yapan şirketin ID'si
AS
BEGIN
    SELECT 
        T.TripID,
        V.PlateOrCode AS AracPlaka,
        C1.CityName + ' > ' + C2.CityName AS Guzergah,
        T.DepartureDate AS Tarih,
        T.DepartureTime AS Saat,
        T.Price AS Fiyat,
        
        -- Durum kontrolü (1: Aktif, 0: İptal/Pasif)
        CASE 
            WHEN T.Status = 1 THEN 'Aktif'
            ELSE 'İptal'
        END AS Durum,
        
        -- O seferde kaç bilet satılmış? (TripSeats tablosundan)
        (SELECT COUNT(*) FROM dbo.TripSeats WHERE TripID = T.TripID AND IsReserved = 1) AS DoluKoltukSayisi,
        
        -- Aracın kapasitesi nedir? (Seats tablosundan sayıyoruz)
        (SELECT COUNT(*) FROM dbo.Seats WHERE VehicleID = V.VehicleID) AS ToplamKoltuk,
        
        -- Araç şirkete mi ait yoksa admin tarafından mı oluşturulmuş?
        CASE 
            WHEN V.CompanyID IS NULL THEN 'Admin'
            WHEN V.CompanyID = @SirketID THEN 'Şirket'
            ELSE 'Diğer'
        END AS SeferTipi
        
    FROM dbo.Trips T
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    INNER JOIN dbo.Cities C1 ON T.FromCityID = C1.CityID
    INNER JOIN dbo.Cities C2 ON T.ToCityID = C2.CityID
    WHERE 
        -- Şirkete ait araçların seferleri (admin panelinden eklenen seferler dahil)
        -- Admin panelinden bir şirkete ait araç seçilerek eklenen seferler de burada görünmeli
        V.CompanyID = @SirketID
        -- Tüm seferleri göster (aktif ve iptal edilmiş)
        -- Status kontrolü kaldırıldı - şirket tüm seferlerini görebilmeli
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
END;
GO
