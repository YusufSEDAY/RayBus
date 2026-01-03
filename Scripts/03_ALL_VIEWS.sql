-- =============================================
-- TÜM VIEW'LAR - KATEGORİZE EDİLMİŞ
-- Açıklama: Tüm view'ları tek dosyada toplar (Schema migration sonrası için hazır)
-- Schema: report (tüm view'lar report schema'sında oluşturulur)


USE [RayBusDB]
GO


-- 1. vw_Admin_Dashboard_Istatistikleri
PRINT '1. vw_Admin_Dashboard_Istatistikleri oluşturuluyor...';

IF OBJECT_ID('report.vw_Admin_Dashboard_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Dashboard_Istatistikleri;
GO

CREATE VIEW report.vw_Admin_Dashboard_Istatistikleri
AS
SELECT 
    -- Kullanıcı İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Users) AS TotalUsers,
    (SELECT COUNT(*) FROM app.Users WHERE Status = 1) AS ActiveUsers,
    
    -- Rezervasyon İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Reservations) AS TotalReservations,
    (SELECT COUNT(*) FROM app.Reservations WHERE Status != 'Cancelled') AS ActiveReservations,
    
    -- Sefer İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Trips) AS TotalTrips,
    (SELECT COUNT(*) FROM app.Trips WHERE Status = 1) AS ActiveTrips,
    
    -- Gelir İstatistikleri (app schema)
    (SELECT ISNULL(SUM(Amount), 0) FROM app.Payments WHERE Status = 'Completed') AS TotalRevenue,
    
    -- Son Güncelleme Tarihi
    GETDATE() AS SonGuncellemeTarihi;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 2. vw_Admin_Rezervasyonlar
-- =============================================
PRINT '2. vw_Admin_Rezervasyonlar oluşturuluyor...';

IF OBJECT_ID('report.vw_Admin_Rezervasyonlar', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Rezervasyonlar;
GO

CREATE VIEW report.vw_Admin_Rezervasyonlar
AS
SELECT 
    R.ReservationID,
    R.UserID,
    U.FullName AS UserName,
    R.TripID,
    CASE 
        WHEN FromCity.CityName IS NOT NULL AND ToCity.CityName IS NOT NULL 
        THEN FromCity.CityName + ' - ' + ToCity.CityName
        ELSE 'Sefer #' + CAST(R.TripID AS VARCHAR(10))
    END AS TripRoute,
    R.SeatID,
    CASE 
        WHEN S.SeatNo IS NOT NULL 
        THEN S.SeatNo
        ELSE 'Koltuk #' + CAST(R.SeatID AS VARCHAR(10))
    END AS SeatNo,
    R.Status,
    R.PaymentStatus,
    R.ReservationDate
FROM app.Reservations R
INNER JOIN app.Users U ON R.UserID = U.UserID
LEFT JOIN app.TripSeats TS ON R.TripID = TS.TripID AND R.SeatID = TS.SeatID
LEFT JOIN app.Trips T ON TS.TripID = T.TripID
LEFT JOIN app.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN app.Cities ToCity ON T.ToCityID = ToCity.CityID
LEFT JOIN app.Seats S ON TS.SeatID = S.SeatID;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 3. vw_Admin_Seferler
-- =============================================
PRINT '3. vw_Admin_Seferler oluşturuluyor...';

IF OBJECT_ID('report.vw_Admin_Seferler', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Seferler;
GO

CREATE VIEW report.vw_Admin_Seferler
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
    -- Güzergah bilgisi
    CASE 
        WHEN FromCity.CityName IS NOT NULL AND ToCity.CityName IS NOT NULL 
        THEN FromCity.CityName + ' > ' + ToCity.CityName
        ELSE 'Güzergah Bilgisi Yok'
    END AS Route,
    -- Şirket bilgisi
    V.CompanyID,
    U.FullName AS CompanyName
FROM app.Trips T
INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
LEFT JOIN app.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN app.Cities ToCity ON T.ToCityID = ToCity.CityID
LEFT JOIN app.Terminals DT ON T.DepartureTerminalID = DT.TerminalID
LEFT JOIN app.Terminals AT ON T.ArrivalTerminalID = AT.TerminalID
LEFT JOIN app.Stations DS ON T.DepartureStationID = DS.StationID
LEFT JOIN app.Stations AS_Station ON T.ArrivalStationID = AS_Station.StationID
LEFT JOIN app.Users U ON V.CompanyID = U.UserID;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 4. vw_Sirket_Istatistikleri
-- =============================================
PRINT '4. vw_Sirket_Istatistikleri oluşturuluyor...';

IF OBJECT_ID('report.vw_Sirket_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW report.vw_Sirket_Istatistikleri;
GO

CREATE VIEW report.vw_Sirket_Istatistikleri
AS
SELECT 
    -- Şirket Bilgileri
    U.UserID AS SirketID,
    U.FullName AS SirketAdi,
    U.Email AS SirketEmail,
    
    -- Sefer İstatistikleri
    (SELECT COUNT(*) 
     FROM app.Trips T
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID) AS ToplamSefer,
    
    (SELECT COUNT(*) 
     FROM app.Trips T
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID AND T.Status = 1) AS AktifSefer,
    
    (SELECT COUNT(*) 
     FROM app.Trips T
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID AND T.Status = 0) AS IptalSefer,
    
    -- Rezervasyon İstatistikleri
    (SELECT COUNT(*) 
     FROM app.Reservations R
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND R.Status != 'Cancelled') AS ToplamRezervasyon,
    
    (SELECT COUNT(*) 
     FROM app.Reservations R
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND (R.Status = 'Reserved' OR R.Status = 'Confirmed')) AS AktifRezervasyon,
    
    (SELECT COUNT(*) 
     FROM app.Reservations R
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND R.Status = 'Cancelled') AS IptalRezervasyon,
    
    -- Gelir İstatistikleri
    (SELECT ISNULL(SUM(P.Amount), 0)
     FROM app.Payments P
     INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND P.Status = 'Completed') AS ToplamGelir,
    
    (SELECT ISNULL(SUM(P.Amount), 0)
     FROM app.Payments P
     INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND P.Status = 'Completed'
       AND P.PaymentDate >= DATEADD(MONTH, -1, GETDATE())) AS SonBirAyGelir,
    
    -- Araç İstatistikleri
    (SELECT COUNT(*) 
     FROM app.Vehicles V
     WHERE V.CompanyID = U.UserID AND V.Active = 1) AS ToplamArac,
    
    (SELECT COUNT(*) 
     FROM app.Vehicles V
     WHERE V.CompanyID = U.UserID 
       AND V.Active = 1 
       AND V.VehicleType = 'Bus') AS OtobusSayisi,
    
    (SELECT COUNT(*) 
     FROM app.Vehicles V
     WHERE V.CompanyID = U.UserID 
       AND V.Active = 1 
       AND V.VehicleType = 'Train') AS TrenSayisi,
    
    -- Dolu Koltuk Oranı (Ortalama)
    (SELECT CASE 
        WHEN COUNT(DISTINCT T.TripID) > 0 THEN
            CAST(SUM(CASE WHEN TS.IsReserved = 1 THEN 1 ELSE 0 END) AS FLOAT) / 
            CAST(COUNT(*) AS FLOAT) * 100
        ELSE 0
     END
     FROM app.Trips T
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     INNER JOIN app.TripSeats TS ON T.TripID = TS.TripID
     WHERE V.CompanyID = U.UserID 
       AND T.Status = 1) AS OrtalamaDoluKoltukOrani,
    
    -- Bu Ay Eklenen Sefer Sayısı
    (SELECT COUNT(*) 
     FROM app.Trips T
     INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND T.CreatedAt >= DATEADD(MONTH, -1, GETDATE())) AS BuAyEklenenSefer,
    
    -- Son Güncelleme Tarihi
    GETDATE() AS SonGuncellemeTarihi

FROM app.Users U
INNER JOIN app.Roles R ON U.RoleID = R.RoleID
WHERE R.RoleName = 'Şirket' AND U.Status = 1;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 5. vw_Sirket_Seferleri
-- =============================================
PRINT '5. vw_Sirket_Seferleri oluşturuluyor...';

IF OBJECT_ID('report.vw_Sirket_Seferleri', 'V') IS NOT NULL
    DROP VIEW report.vw_Sirket_Seferleri;
GO

CREATE VIEW report.vw_Sirket_Seferleri
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
    (SELECT COUNT(*) FROM app.TripSeats TS WHERE TS.TripID = T.TripID AND TS.IsReserved = 1) AS DoluKoltukSayisi,
    (SELECT COUNT(*) FROM app.Seats S WHERE S.VehicleID = V.VehicleID AND S.IsActive = 1) AS ToplamKoltuk,
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
FROM app.Trips T
INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
LEFT JOIN app.Cities FromCity ON T.FromCityID = FromCity.CityID
LEFT JOIN app.Cities ToCity ON T.ToCityID = ToCity.CityID;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 6. vw_Kullanici_Istatistikleri
-- =============================================
PRINT '6. vw_Kullanici_Istatistikleri oluşturuluyor...';

IF OBJECT_ID('report.vw_Kullanici_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW report.vw_Kullanici_Istatistikleri;
GO

CREATE VIEW report.vw_Kullanici_Istatistikleri
AS
SELECT 
    U.UserID,
    U.FullName AS KullaniciAdi,
    U.Email AS KullaniciEmail,
    
    -- Toplam Harcama (func schema function kullanımı)
    [func].fn_Toplam_Harcama(U.UserID) AS ToplamHarcama,
    
    -- Seyahat Sayıları
    [func].fn_Seyahat_Sayisi(U.UserID) AS ToplamSeyahatSayisi,
    
    (SELECT COUNT(*) 
     FROM app.Reservations R
     INNER JOIN app.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Completed'
       AND T.DepartureDate >= GETDATE()) AS GelecekSeyahatSayisi,
    
    (SELECT COUNT(*) 
     FROM app.Reservations R
     INNER JOIN app.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Completed'
       AND T.DepartureDate < GETDATE()) AS GecmisSeyahatSayisi,
    
    -- Ortalama Fiyat
    [func].fn_Ortalama_Seyahat_Fiyati(U.UserID) AS OrtalamaSeyahatFiyati,
    
    -- En Çok Gidilen Şehirler (Top 3)
    (SELECT TOP 1 C2.CityName
     FROM app.Reservations R
     INNER JOIN app.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     INNER JOIN app.Cities C2 ON T.ToCityID = C2.CityID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Completed'
     GROUP BY C2.CityName
     ORDER BY COUNT(*) DESC) AS EnCokGidilenSehir,
    
    -- Son Seyahat Tarihi
    (SELECT MAX(T.DepartureDate)
     FROM app.Reservations R
     INNER JOIN app.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN app.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Completed') AS SonSeyahatTarihi,
    
    -- Toplam Rezervasyon Sayısı
    (SELECT COUNT(*) 
     FROM app.Reservations R
     WHERE R.UserID = U.UserID) AS ToplamRezervasyonSayisi,
    
    -- İptal Edilen Rezervasyon Sayısı
    (SELECT COUNT(*) 
     FROM app.Reservations R
     WHERE R.UserID = U.UserID 
       AND R.Status = 'Cancelled') AS IptalEdilenRezervasyonSayisi,
    
    -- Kayıt Tarihi
    U.CreatedAt AS KayitTarihi
    
FROM app.Users U
WHERE U.Status = 1;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 7. vw_Bilet_Detay
-- =============================================
PRINT '7. vw_Bilet_Detay oluşturuluyor...';

IF OBJECT_ID('report.vw_Bilet_Detay', 'V') IS NOT NULL
    DROP VIEW report.vw_Bilet_Detay;
GO

CREATE VIEW report.vw_Bilet_Detay
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
    
FROM app.Reservations R
INNER JOIN app.Users U ON R.UserID = U.UserID
INNER JOIN app.Trips T ON R.TripID = T.TripID
INNER JOIN app.Cities C1 ON T.FromCityID = C1.CityID
INNER JOIN app.Cities C2 ON T.ToCityID = C2.CityID
INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
INNER JOIN app.TripSeats TS ON R.TripID = TS.TripID AND R.SeatID = TS.SeatID
INNER JOIN app.Seats S ON TS.SeatID = S.SeatID
LEFT JOIN app.Terminals TER1 ON T.DepartureTerminalID = TER1.TerminalID
LEFT JOIN app.Terminals TER2 ON T.ArrivalTerminalID = TER2.TerminalID
LEFT JOIN app.Stations ST1 ON T.DepartureStationID = ST1.StationID
LEFT JOIN app.Stations ST2 ON T.ArrivalStationID = ST2.StationID
LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID
LEFT JOIN app.Wagons W ON S.WagonID = W.WagonID
LEFT JOIN app.Buses B ON V.VehicleID = B.BusID
LEFT JOIN app.Trains TR ON V.VehicleID = TR.TrainID;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 8. vw_Sefer_Detaylari
-- =============================================
PRINT '8. vw_Sefer_Detaylari oluşturuluyor...';

IF OBJECT_ID('report.vw_Sefer_Detaylari', 'V') IS NOT NULL
    DROP VIEW report.vw_Sefer_Detaylari;
GO

CREATE VIEW report.vw_Sefer_Detaylari
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
    (SELECT COUNT(*) FROM app.TripSeats TS WHERE TS.TripID = T.TripID) AS ToplamKoltuk,
    (SELECT COUNT(*) FROM app.TripSeats TS WHERE TS.TripID = T.TripID AND TS.IsReserved = 1) AS SatilanKoltuk,
    
    -- Durum
    CASE 
        WHEN T.Status = 1 THEN 'Aktif' 
        ELSE 'İptal' 
    END AS SeferDurumu

FROM app.Trips T
INNER JOIN app.Cities F ON T.FromCityID = F.CityID
INNER JOIN app.Cities TC ON T.ToCityID = TC.CityID
INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 9. vw_Guzergah_Ciro_Raporu
-- =============================================
PRINT '9. vw_Guzergah_Ciro_Raporu oluşturuluyor...';

IF OBJECT_ID('report.vw_Guzergah_Ciro_Raporu', 'V') IS NOT NULL
    DROP VIEW report.vw_Guzergah_Ciro_Raporu;
GO

CREATE VIEW report.vw_Guzergah_Ciro_Raporu
AS
SELECT 
    F.CityName + ' - ' + T.CityName AS Guzergah,
    V.VehicleType AS AracTipi,
    COUNT(R.ReservationID) AS ToplamSatisAdedi,
    ISNULL(SUM(P.Amount), 0) AS ToplamCiro,
    AVG(P.Amount) AS OrtalamaBiletFiyati

FROM app.Reservations R
INNER JOIN app.Trips Trip ON R.TripID = Trip.TripID
INNER JOIN app.Cities F ON Trip.FromCityID = F.CityID
INNER JOIN app.Cities T ON Trip.ToCityID = T.CityID
INNER JOIN app.Vehicles V ON Trip.VehicleID = V.VehicleID
LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID

WHERE R.Status <> 'Cancelled'
GROUP BY F.CityName, T.CityName, V.VehicleType;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 10. vw_Tum_Lokasyonlar_Union
-- =============================================
PRINT '10. vw_Tum_Lokasyonlar_Union oluşturuluyor...';

IF OBJECT_ID('report.vw_Tum_Lokasyonlar_Union', 'V') IS NOT NULL
    DROP VIEW report.vw_Tum_Lokasyonlar_Union;
GO

CREATE VIEW report.vw_Tum_Lokasyonlar_Union
AS
SELECT 
    CityID,
    TerminalName AS YerAdi,
    'Otobüs Terminali' AS YerTipi,
    Address
FROM app.Terminals

UNION ALL

SELECT 
    CityID,
    StationName AS YerAdi,
    'Tren Garı' AS YerTipi,
    Address
FROM app.Stations;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 11. vw_Admin_Dashboard_Ozet
-- =============================================
PRINT '11. vw_Admin_Dashboard_Ozet oluşturuluyor...';

IF OBJECT_ID('report.vw_Admin_Dashboard_Ozet', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Dashboard_Ozet;
GO

CREATE VIEW report.vw_Admin_Dashboard_Ozet
AS
SELECT 
    -- 1. Toplam Üye Sayısı
    (SELECT COUNT(*) FROM app.Users WHERE Status = 1) AS ToplamAktifUye,
    
    -- 2. Gelecekteki Aktif Sefer Sayısı
    (SELECT COUNT(*) FROM app.Trips WHERE DepartureDate >= CAST(GETDATE() AS DATE) AND Status = 1) AS GelecekSeferler,
    
    -- 3. Bugünün Toplam Cirosu (İptal edilmeyenler)
    (SELECT ISNULL(SUM(Amount), 0) 
     FROM app.Payments P 
     INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
     WHERE R.Status = 'Reserved' 
       AND CAST(P.PaymentDate AS DATE) = CAST(GETDATE() AS DATE)) AS GunlukCiro,

    -- 4. Toplam Satılan Bilet (Tüm Zamanlar)
    (SELECT COUNT(*) FROM app.Reservations WHERE Status = 'Reserved') AS ToplamSatis,

    -- 5. Son 24 Saatteki Hata/Log Sayısı (Sistem Sağlığı İçin)
    (SELECT COUNT(*) FROM log.PaymentLogs WHERE LogDate >= DATEADD(day, -1, SYSUTCDATETIME())) AS SonIslemLoglari;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 12. vw_Admin_Gunluk_Finansal_Rapor
-- =============================================
PRINT '12. vw_Admin_Gunluk_Finansal_Rapor oluşturuluyor...';

IF OBJECT_ID('report.vw_Admin_Gunluk_Finansal_Rapor', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Gunluk_Finansal_Rapor;
GO

CREATE VIEW report.vw_Admin_Gunluk_Finansal_Rapor
AS
SELECT 
    -- Tarih bilgisini saatten arındırıp sadece GÜN yapıyoruz
    CAST(PaymentDate AS DATE) AS IslemTarihi,
    
    -- Kredi Kartı mı Havale mi? Kırılımı görmek için
    PaymentMethod AS OdemeYontemi,
    
    -- O gün kaç tane bilet satılmış?
    COUNT(*) AS ToplamSatisAdedi,
    
    -- O gün kasaya giren toplam para
    SUM(Amount) AS ToplamCiro
FROM app.Payments
WHERE Status = 'Completed'
GROUP BY CAST(PaymentDate AS DATE), PaymentMethod;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 13. vw_Bekleyen_Iptaller
-- =============================================
PRINT '13. vw_Bekleyen_Iptaller oluşturuluyor...';

IF OBJECT_ID('report.vw_Bekleyen_Iptaller', 'V') IS NOT NULL
    DROP VIEW report.vw_Bekleyen_Iptaller;
GO

CREATE VIEW report.vw_Bekleyen_Iptaller
AS
SELECT 
    R.ReservationID,
    R.UserID,
    U.FullName AS KullaniciAdi,
    U.Email AS KullaniciEmail,
    R.ReservationDate,
    DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) AS GecenDakika,
    CASE 
        WHEN DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) >= 15 THEN 'İptal Edilmeli'
        ELSE 'Beklemede'
    END AS Durum,
    T.TripID,
    T.DepartureDate,
    T.DepartureTime,
    C1.CityName AS KalkisSehri,
    C2.CityName AS VarisSehri,
    T.Price AS SeferFiyati
FROM app.Reservations R
INNER JOIN app.Users U ON R.UserID = U.UserID
INNER JOIN app.Trips T ON R.TripID = T.TripID
INNER JOIN app.Cities C1 ON T.FromCityID = C1.CityID
INNER JOIN app.Cities C2 ON T.ToCityID = C2.CityID
LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID
WHERE R.Status = 'Reserved'
  AND (P.Status IS NULL OR P.Status = 'Pending')
  AND DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) < 60;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- TAMAMLANDI
-- =============================================
PRINT '========================================';
PRINT 'TÜM VIEW''LAR BAŞARIYLA OLUŞTURULDU!';
PRINT '========================================';
PRINT '';
PRINT 'Oluşturulan View''lar:';
PRINT '  1. report.vw_Admin_Dashboard_Istatistikleri';
PRINT '  2. report.vw_Admin_Rezervasyonlar';
PRINT '  3. report.vw_Admin_Seferler';
PRINT '  4. report.vw_Sirket_Istatistikleri';
PRINT '  5. report.vw_Sirket_Seferleri';
PRINT '  6. report.vw_Kullanici_Istatistikleri';
PRINT '  7. report.vw_Bilet_Detay';
PRINT '  8. report.vw_Sefer_Detaylari';
PRINT '  9. report.vw_Guzergah_Ciro_Raporu';
PRINT '  10. report.vw_Tum_Lokasyonlar_Union';
PRINT '  11. report.vw_Admin_Dashboard_Ozet';
PRINT '  12. report.vw_Admin_Gunluk_Finansal_Rapor';
PRINT '  13. report.vw_Bekleyen_Iptaller';
PRINT '';
PRINT 'Test sorguları:';
PRINT '  SELECT * FROM report.vw_Admin_Dashboard_Istatistikleri;';
PRINT '  SELECT TOP 10 * FROM report.vw_Admin_Rezervasyonlar ORDER BY ReservationDate DESC;';
PRINT '  SELECT * FROM report.vw_Sirket_Istatistikleri WHERE SirketID = 11;';
PRINT '';
GO

