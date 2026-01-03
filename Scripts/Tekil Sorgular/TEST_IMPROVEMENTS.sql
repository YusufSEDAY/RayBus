-- =============================================
-- İYİLEŞTİRMELERİ TEST ET
-- Açıklama: Yeni eklenen view ve stored procedure'leri test eder
-- Tarih: 2024-12-19
-- =============================================

PRINT '========================================';
PRINT 'VERİTABANI İYİLEŞTİRMELERİ TEST EDİLİYOR...';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. vw_Admin_Dashboard_Istatistikleri TEST
-- =============================================
PRINT '1. vw_Admin_Dashboard_Istatistikleri test ediliyor...';
SELECT * FROM vw_Admin_Dashboard_Istatistikleri;
PRINT '   ✅ Test tamamlandı';
PRINT '';

-- =============================================
-- 2. vw_Admin_Rezervasyonlar TEST
-- =============================================
PRINT '2. vw_Admin_Rezervasyonlar test ediliyor...';
SELECT TOP 10 
    ReservationID,
    UserName,
    TripRoute,
    SeatNo,
    Status,
    PaymentStatus,
    ReservationDate
FROM vw_Admin_Rezervasyonlar 
ORDER BY ReservationDate DESC;
PRINT '   ✅ Test tamamlandı';
PRINT '';

-- =============================================
-- 3. sp_Sirket_Istatistikleri_Getir TEST
-- =============================================
PRINT '3. sp_Sirket_Istatistikleri_Getir test ediliyor...';
-- Önce bir şirket ID'si bul
DECLARE @TestSirketID INT;
SELECT TOP 1 @TestSirketID = UserID 
FROM dbo.Users 
WHERE UserID IN (SELECT DISTINCT CompanyID FROM dbo.Vehicles WHERE CompanyID IS NOT NULL);

IF @TestSirketID IS NOT NULL
BEGIN
    PRINT '   Test şirket ID: ' + CAST(@TestSirketID AS VARCHAR(10));
    EXEC sp_Sirket_Istatistikleri_Getir @SirketID = @TestSirketID;
    PRINT '   ✅ Test tamamlandı';
END
ELSE
BEGIN
    PRINT '   ⚠️ Test için şirket bulunamadı';
END
PRINT '';

-- =============================================
-- 4. vw_Admin_Seferler TEST
-- =============================================
PRINT '4. vw_Admin_Seferler test ediliyor...';
SELECT TOP 10 
    TripID,
    VehiclePlate,
    VehicleType,
    Route,
    CompanyName,
    DepartureDate,
    DepartureTime,
    Price,
    StatusText
FROM vw_Admin_Seferler 
ORDER BY DepartureDate DESC;
PRINT '   ✅ Test tamamlandı';
PRINT '';

-- =============================================
-- 5. vw_Sirket_Seferleri TEST
-- =============================================
PRINT '5. vw_Sirket_Seferleri test ediliyor...';
-- Önce bir şirket ID'si bul
DECLARE @TestSirketID2 INT;
SELECT TOP 1 @TestSirketID2 = CompanyID 
FROM dbo.Vehicles 
WHERE CompanyID IS NOT NULL;

IF @TestSirketID2 IS NOT NULL
BEGIN
    PRINT '   Test şirket ID: ' + CAST(@TestSirketID2 AS VARCHAR(10));
    SELECT TOP 10 
        TripID,
        AracPlaka,
        Guzergah,
        Tarih,
        Saat,
        Fiyat,
        Durum,
        DoluKoltukSayisi,
        ToplamKoltuk
    FROM vw_Sirket_Seferleri 
    WHERE CompanyID = @TestSirketID2
    ORDER BY Tarih DESC, Saat DESC;
    PRINT '   ✅ Test tamamlandı';
END
ELSE
BEGIN
    PRINT '   ⚠️ Test için şirket bulunamadı';
END
PRINT '';

-- =============================================
-- 6. sp_Kullanici_Istatistikleri_Getir TEST
-- =============================================
PRINT '6. sp_Kullanici_Istatistikleri_Getir test ediliyor...';
-- Önce bir kullanıcı ID'si bul
DECLARE @TestUserID INT;
SELECT TOP 1 @TestUserID = UserID 
FROM dbo.Users 
WHERE UserID IN (SELECT DISTINCT UserID FROM dbo.Reservations);

IF @TestUserID IS NOT NULL
BEGIN
    PRINT '   Test kullanıcı ID: ' + CAST(@TestUserID AS VARCHAR(10));
    EXEC sp_Kullanici_Istatistikleri_Getir @UserID = @TestUserID;
    PRINT '   ✅ Test tamamlandı';
END
ELSE
BEGIN
    PRINT '   ⚠️ Test için kullanıcı bulunamadı';
END
PRINT '';

-- =============================================
-- 7. sp_Kullanici_Biletleri TEST
-- =============================================
PRINT '7. sp_Kullanici_Biletleri test ediliyor...';
-- Önce bir kullanıcı ID'si bul
DECLARE @TestUserID2 INT;
SELECT TOP 1 @TestUserID2 = UserID 
FROM dbo.Users 
WHERE UserID IN (SELECT DISTINCT UserID FROM dbo.Reservations);

IF @TestUserID2 IS NOT NULL
BEGIN
    PRINT '   Test kullanıcı ID: ' + CAST(@TestUserID2 AS VARCHAR(10));
    SELECT TOP 10 
        ReservationID,
        TripID,
        Guzergah,
        DepartureDate,
        KalkisSaati,
        VehicleType,
        PlateOrCode,
        SeatNo,
        OdenenTutar,
        TripFiyati,
        RezervasyonDurumu,
        PaymentStatus
    FROM (
        EXEC sp_Kullanici_Biletleri @KullaniciID = @TestUserID2
    ) AS Results;
    PRINT '   ✅ Test tamamlandı';
END
ELSE
BEGIN
    PRINT '   ⚠️ Test için kullanıcı bulunamadı';
END
PRINT '';

-- =============================================
-- PERFORMANS KARŞILAŞTIRMASI
-- =============================================
PRINT '========================================';
PRINT 'PERFORMANS KARŞILAŞTIRMASI';
PRINT '========================================';
PRINT '';

-- View kullanımı (hızlı)
PRINT 'View kullanımı (Önerilen):';
SET STATISTICS TIME ON;
SELECT * FROM vw_Admin_Dashboard_Istatistikleri;
SET STATISTICS TIME OFF;
PRINT '';

-- EF Core benzeri sorgu (yavaş)
PRINT 'EF Core benzeri sorgu (Karşılaştırma için):';
SET STATISTICS TIME ON;
SELECT 
    (SELECT COUNT(*) FROM dbo.Users) AS TotalUsers,
    (SELECT COUNT(*) FROM dbo.Users WHERE Status = 1) AS ActiveUsers,
    (SELECT COUNT(*) FROM dbo.Reservations) AS TotalReservations,
    (SELECT COUNT(*) FROM dbo.Reservations WHERE Status != 'Cancelled') AS ActiveReservations,
    (SELECT COUNT(*) FROM dbo.Trips) AS TotalTrips,
    (SELECT COUNT(*) FROM dbo.Trips WHERE Status = 1) AS ActiveTrips,
    (SELECT ISNULL(SUM(Amount), 0) FROM dbo.Payments WHERE Status = 'Completed') AS TotalRevenue;
SET STATISTICS TIME OFF;
PRINT '';

PRINT '========================================';
PRINT 'TÜM TESTLER TAMAMLANDI!';
PRINT '========================================';
GO

