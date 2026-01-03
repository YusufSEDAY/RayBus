-- Trigger test scripti
-- Bu script trigger'ın düzgün çalışıp çalışmadığını test eder

-- Test 1: Yeni bir sefer ekle ve koltukların otomatik oluşturulup oluşturulmadığını kontrol et

DECLARE @TestVehicleID INT;
DECLARE @TestTripID INT;
DECLARE @FromCityID INT = 1; -- Örnek şehir ID'leri
DECLARE @ToCityID INT = 2;

-- Önce bir test aracı seç (veya oluştur)
SELECT TOP 1 @TestVehicleID = VehicleID 
FROM dbo.Vehicles 
WHERE Active = 1 AND VehicleType = 'Bus';

IF @TestVehicleID IS NULL
BEGIN
    PRINT 'Test için aktif bir otobüs bulunamadı.';
    RETURN;
END

-- Sefer oluştur
INSERT INTO dbo.Trips (
    VehicleID, FromCityID, ToCityID,
    DepartureDate, DepartureTime,
    Price, Status, CreatedAt
)
VALUES (
    @TestVehicleID,
    @FromCityID,
    @ToCityID,
    CAST(GETDATE() AS DATE),
    CAST(GETDATE() AS TIME),
    100.00,
    1,
    GETUTCDATE()
);

SET @TestTripID = SCOPE_IDENTITY();

-- Koltukların oluşturulup oluşturulmadığını kontrol et
DECLARE @SeatCount INT;
SELECT @SeatCount = COUNT(*) 
FROM dbo.TripSeats 
WHERE TripID = @TestTripID;

IF @SeatCount > 0
BEGIN
    PRINT '✓ Trigger başarılı: ' + CAST(@SeatCount AS VARCHAR) + ' koltuk oluşturuldu.';
    
    -- Test seferini temizle
    DELETE FROM dbo.TripSeats WHERE TripID = @TestTripID;
    DELETE FROM dbo.Trips WHERE TripID = @TestTripID;
    PRINT 'Test seferi temizlendi.';
END
ELSE
BEGIN
    PRINT '✗ Trigger başarısız: Koltuklar oluşturulmadı.';
    
    -- Test seferini temizle
    DELETE FROM dbo.Trips WHERE TripID = @TestTripID;
END
GO

