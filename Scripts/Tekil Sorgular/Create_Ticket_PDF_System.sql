-- =============================================
-- Bilet PDF İndirme Sistemi
-- Açıklama: Bilet bilgilerini PDF için hazırlar
-- Tarih: 2024-12-15
-- =============================================

USE RayBusDB;
GO

-- =============================================
-- 1. Reservations Tablosuna TicketNumber Kolonu Ekle
-- =============================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Reservations') 
      AND name = 'TicketNumber'
)
BEGIN
    ALTER TABLE dbo.Reservations
    ADD TicketNumber NVARCHAR(50) NULL;
    
    CREATE INDEX IX_Reservations_TicketNumber ON dbo.Reservations(TicketNumber);
    
    PRINT '✅ TicketNumber kolonu eklendi.';
END
GO

-- =============================================
-- 2. Function: fn_Bilet_Numarasi_Uret (Kaldırıldı - NEWID() function içinde kullanılamaz)
-- Not: Bilet numarası trigger içinde direkt üretilecek
-- =============================================
IF OBJECT_ID('dbo.fn_Bilet_Numarasi_Uret', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_Bilet_Numarasi_Uret;
GO

-- =============================================
-- 3. Trigger: trg_Bilet_Numarasi
-- Açıklama: Rezervasyon oluşturulduğunda bilet numarası atar
-- Format: RB-YYYYMMDD-HHMMSS-XXXXX (RB-20241215-143025-12345)
-- =============================================
IF OBJECT_ID('dbo.trg_Bilet_Numarasi', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Bilet_Numarasi;
GO

CREATE TRIGGER dbo.trg_Bilet_Numarasi
ON dbo.Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ReservationID INT;
    DECLARE @BiletNumarasi NVARCHAR(50);
    DECLARE @Tarih NVARCHAR(8) = FORMAT(GETDATE(), 'yyyyMMdd');
    DECLARE @Saat NVARCHAR(6) = FORMAT(GETDATE(), 'HHmmss');
    DECLARE @RastgeleSayi INT;
    DECLARE @SayiStr NVARCHAR(5);
    DECLARE @Counter INT = 0;
    
    DECLARE reservation_cursor CURSOR FOR
    SELECT ReservationID
    FROM inserted
    WHERE TicketNumber IS NULL;
    
    OPEN reservation_cursor;
    FETCH NEXT FROM reservation_cursor INTO @ReservationID;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Counter = 0;
        
        -- Benzersiz bilet numarası üret
        WHILE @Counter < 100 -- Maksimum 100 deneme
        BEGIN
            SET @RastgeleSayi = ABS(CHECKSUM(NEWID())) % 99999;
            SET @SayiStr = RIGHT('00000' + CAST(@RastgeleSayi AS NVARCHAR(5)), 5);
            SET @BiletNumarasi = 'RB-' + @Tarih + '-' + @Saat + '-' + @SayiStr;
            
            -- Benzersizlik kontrolü
            IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE TicketNumber = @BiletNumarasi)
            BEGIN
                -- Bilet numarasını ata
                UPDATE dbo.Reservations
                SET TicketNumber = @BiletNumarasi
                WHERE ReservationID = @ReservationID;
                
                BREAK; -- Benzersiz numara bulundu, döngüden çık
            END
            
            SET @Counter = @Counter + 1;
        END
        
        -- Eğer 100 denemede benzersiz numara bulunamazsa, ReservationID kullan
        IF @Counter >= 100
        BEGIN
            SET @BiletNumarasi = 'RB-' + @Tarih + '-' + @Saat + '-' + RIGHT('00000' + CAST(@ReservationID AS NVARCHAR(10)), 5);
            
            UPDATE dbo.Reservations
            SET TicketNumber = @BiletNumarasi
            WHERE ReservationID = @ReservationID;
        END
        
        FETCH NEXT FROM reservation_cursor INTO @ReservationID;
    END
    
    CLOSE reservation_cursor;
    DEALLOCATE reservation_cursor;
END;
GO

-- =============================================
-- 4. View: vw_Bilet_Detay
-- Açıklama: Bilet detay bilgilerini toplar (PDF için)
-- =============================================
IF OBJECT_ID('dbo.vw_Bilet_Detay', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Bilet_Detay;
GO

CREATE VIEW dbo.vw_Bilet_Detay
AS
SELECT 
    -- Rezervasyon Bilgileri
    R.ReservationID,
    R.TicketNumber AS BiletNumarasi,
    R.ReservationDate AS RezervasyonTarihi,
    R.Status AS RezervasyonDurumu,
    
    -- Kullanıcı Bilgileri
    U.UserID,
    U.FullName AS KullaniciAdi,
    U.Email AS KullaniciEmail,
    U.Phone AS KullaniciTelefon,
    
    -- Sefer Bilgileri
    T.TripID,
    T.DepartureDate AS KalkisTarihi,
    T.DepartureTime AS KalkisSaati,
    T.ArrivalDate AS VarisTarihi,
    T.ArrivalTime AS VarisSaati,
    T.Price AS SeferFiyati,
    
    -- Şehir Bilgileri
    C1.CityName AS KalkisSehri,
    C2.CityName AS VarisSehri,
    
    -- Terminal/İstasyon Bilgileri
    TER1.TerminalName AS KalkisTerminali,
    TER2.TerminalName AS VarisTerminali,
    ST1.StationName AS KalkisIstasyonu,
    ST2.StationName AS VarisIstasyonu,
    
    -- Araç Bilgileri
    V.VehicleID,
    V.PlateOrCode AS AracPlakasi,
    V.VehicleType AS AracTipi,
    
    -- Koltuk Bilgileri
    S.SeatID,
    S.SeatNo AS KoltukNumarasi,
    TS.IsReserved AS KoltukDurumu,
    
    -- Ödeme Bilgileri
    P.PaymentID,
    ISNULL(P.Amount, T.Price) AS OdenenTutar,
    P.PaymentDate AS OdemeTarihi,
    P.PaymentMethod AS OdemeYontemi,
    P.Status AS OdemeDurumu,
    
    -- Vagon Bilgileri (Tren için)
    W.WagonNo AS VagonNumarasi,
    
    -- Otobüs Bilgileri
    B.BusModel AS OtobusModeli,
    B.LayoutType AS KoltukDuzeni,
    
    -- Tren Bilgileri
    TR.TrainModel AS TrenModeli,
    
    -- Ek Bilgiler
    CASE 
        WHEN V.VehicleType = 'Bus' THEN 'Otobüs'
        WHEN V.VehicleType = 'Train' THEN 'Tren'
        ELSE 'Bilinmiyor'
    END AS AracTipiTurkce,
    
    DATEDIFF(HOUR, T.DepartureDate, ISNULL(T.ArrivalDate, T.DepartureDate)) AS SeyahatSuresiSaat
    
FROM dbo.Reservations R
INNER JOIN dbo.Users U ON R.UserID = U.UserID
INNER JOIN dbo.Trips T ON R.TripID = T.TripID
INNER JOIN dbo.Cities C1 ON T.FromCityID = C1.CityID
INNER JOIN dbo.Cities C2 ON T.ToCityID = C2.CityID
INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
INNER JOIN dbo.TripSeats TS ON R.TripID = TS.TripID AND R.SeatID = TS.SeatID
INNER JOIN dbo.Seats S ON TS.SeatID = S.SeatID
LEFT JOIN dbo.Terminals TER1 ON T.DepartureTerminalID = TER1.TerminalID
LEFT JOIN dbo.Terminals TER2 ON T.ArrivalTerminalID = TER2.TerminalID
LEFT JOIN dbo.Stations ST1 ON T.DepartureStationID = ST1.StationID
LEFT JOIN dbo.Stations ST2 ON T.ArrivalStationID = ST2.StationID
LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
LEFT JOIN dbo.Wagons W ON S.WagonID = W.WagonID
LEFT JOIN dbo.Buses B ON V.VehicleID = B.BusID
LEFT JOIN dbo.Trains TR ON V.VehicleID = TR.TrainID;
GO

-- =============================================
-- 5. Stored Procedure: sp_Bilet_Bilgileri
-- Açıklama: Bilet bilgilerini getirir (PDF için)
-- =============================================
IF OBJECT_ID('dbo.sp_Bilet_Bilgileri', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Bilet_Bilgileri;
GO

CREATE PROCEDURE dbo.sp_Bilet_Bilgileri
    @ReservationID INT = NULL,
    @TicketNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @ReservationID IS NOT NULL
    BEGIN
        SELECT * 
        FROM dbo.vw_Bilet_Detay
        WHERE ReservationID = @ReservationID;
    END
    ELSE IF @TicketNumber IS NOT NULL
    BEGIN
        SELECT * 
        FROM dbo.vw_Bilet_Detay
        WHERE BiletNumarasi = @TicketNumber;
    END
    ELSE
    BEGIN
        RAISERROR('ReservationID veya TicketNumber parametresi gereklidir.', 16, 1);
    END
END;
GO

-- Test sorguları
-- SELECT * FROM dbo.vw_Bilet_Detay WHERE ReservationID = 1;
-- EXEC sp_Bilet_Bilgileri @ReservationID = 1;
-- EXEC sp_Bilet_Bilgileri @TicketNumber = 'RB-20241215-143025-12345';

PRINT '✅ Bilet PDF İndirme Sistemi başarıyla oluşturuldu!';
PRINT '📋 Oluşturulan nesneler:';
PRINT '   - Kolon: Reservations.TicketNumber';
PRINT '   - Trigger: trg_Bilet_Numarasi (bilet numarası üretir)';
PRINT '   - View: vw_Bilet_Detay';
PRINT '   - SP: sp_Bilet_Bilgileri';
PRINT '';
PRINT '🔧 Kullanım:';
PRINT '   SELECT * FROM dbo.vw_Bilet_Detay WHERE ReservationID = 1;';
PRINT '   EXEC sp_Bilet_Bilgileri @ReservationID = 1;';
GO

