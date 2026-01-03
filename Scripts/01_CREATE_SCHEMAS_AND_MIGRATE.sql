-- Schema Kategorileri:
--   app    : Uygulama tabloları (Users, Roles, Trips, Reservations, vb.)
--   log    : Log tabloları (TripLogs, ReservationLogs, PaymentLogs, vb.)
--   report : Raporlama view'ları (vw_Admin_Dashboard_Istatistikleri, vb.)
--   proc   : Stored procedure'ler (sp_Kullanici_Kayit, sp_Rezervasyon_Yap, vb.)
--   trg    : Trigger'lar (trg_Rezervasyon_Sonrasi_Koltuk_Guncelle, vb.)
--   func   : Function'lar (fn_Toplam_Harcama, vb.)


USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'SCHEMA OLUŞTURMA VE NESNE TAŞIMA İŞLEMİ';
PRINT '========================================';
PRINT '';


-- 1. SCHEMA'LARI OLUŞTUR

PRINT '1. Schema''lar oluşturuluyor...';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'app')
BEGIN
    EXEC('CREATE SCHEMA app');
    PRINT '   ✅ app schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  app schema zaten mevcut';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'log')
BEGIN
    EXEC('CREATE SCHEMA log');
    PRINT '   ✅ log schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  log schema zaten mevcut';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'report')
BEGIN
    EXEC('CREATE SCHEMA report');
    PRINT '   ✅ report schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  report schema zaten mevcut';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'proc')
BEGIN
    EXEC('CREATE SCHEMA [proc]');
    PRINT '   ✅ proc schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  proc schema zaten mevcut';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'trg')
BEGIN
    EXEC('CREATE SCHEMA trg');
    PRINT '   ✅ trg schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  trg schema zaten mevcut';

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'func')
BEGIN
    EXEC('CREATE SCHEMA func');
    PRINT '   ✅ func schema oluşturuldu';
END
ELSE
    PRINT '   ⚠️  func schema zaten mevcut';

PRINT '';
GO

-- =============================================
-- 2. UYGULAMA TABLOLARINI app SCHEMA'SINA TAŞI
-- =============================================
PRINT '2. Uygulama tabloları app schema''sına taşınıyor...';

-- Temel tablolar
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Roles;
    PRINT '   ✅ Roles tablosu taşındı';
END

IF OBJECT_ID('dbo.Cities', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Cities;
    PRINT '   ✅ Cities tablosu taşındı';
END

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Users;
    PRINT '   ✅ Users tablosu taşındı';
END

-- Lokasyon tabloları
IF OBJECT_ID('dbo.Terminals', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Terminals;
    PRINT '   ✅ Terminals tablosu taşındı';
END

IF OBJECT_ID('dbo.Stations', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Stations;
    PRINT '   ✅ Stations tablosu taşındı';
END

-- Araç tabloları
IF OBJECT_ID('dbo.Vehicles', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Vehicles;
    PRINT '   ✅ Vehicles tablosu taşındı';
END

IF OBJECT_ID('dbo.Buses', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Buses;
    PRINT '   ✅ Buses tablosu taşındı';
END

IF OBJECT_ID('dbo.Trains', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Trains;
    PRINT '   ✅ Trains tablosu taşındı';
END

IF OBJECT_ID('dbo.Wagons', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Wagons;
    PRINT '   ✅ Wagons tablosu taşındı';
END

IF OBJECT_ID('dbo.Seats', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Seats;
    PRINT '   ✅ Seats tablosu taşındı';
END

-- Sefer ve rezervasyon tabloları
IF OBJECT_ID('dbo.Trips', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Trips;
    PRINT '   ✅ Trips tablosu taşındı';
END

IF OBJECT_ID('dbo.TripSeats', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.TripSeats;
    PRINT '   ✅ TripSeats tablosu taşındı';
END

IF OBJECT_ID('dbo.CancellationReasons', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.CancellationReasons;
    PRINT '   ✅ CancellationReasons tablosu taşındı';
END

IF OBJECT_ID('dbo.Reservations', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Reservations;
    PRINT '   ✅ Reservations tablosu taşındı';
END

IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Payments;
    PRINT '   ✅ Payments tablosu taşındı';
END

-- Sistem tabloları
IF OBJECT_ID('dbo.Settings', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.Settings;
    PRINT '   ✅ Settings tablosu taşındı';
END

PRINT '';
GO


-- 3. LOG TABLOLARINI log SCHEMA'SINA TAŞI

PRINT '3. Log tabloları log schema''sına taşınıyor...';

IF OBJECT_ID('dbo.TripLogs', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.TripLogs;
    PRINT '   ✅ TripLogs tablosu taşındı';
END

IF OBJECT_ID('dbo.ReservationLogs', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.ReservationLogs;
    PRINT '   ✅ ReservationLogs tablosu taşındı';
END

IF OBJECT_ID('dbo.PaymentLogs', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.PaymentLogs;
    PRINT '   ✅ PaymentLogs tablosu taşındı';
END

IF OBJECT_ID('dbo.AutoCancellationLog', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.AutoCancellationLog;
    PRINT '   ✅ AutoCancellationLog tablosu taşındı';
END

IF OBJECT_ID('dbo.NotificationQueue', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.NotificationQueue;
    PRINT '   ✅ NotificationQueue tablosu taşındı';
END

IF OBJECT_ID('dbo.UserNotificationPreferences', 'U') IS NOT NULL
BEGIN
    ALTER SCHEMA log TRANSFER dbo.UserNotificationPreferences;
    PRINT '   ✅ UserNotificationPreferences tablosu taşındı';
END

PRINT '';
GO


-- 4. VIEW'LARI report SCHEMA'SINA TAŞI

PRINT '4. View''lar report schema''sına taşınıyor...';

-- Admin view'ları
IF OBJECT_ID('dbo.vw_Admin_Dashboard_Istatistikleri', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Admin_Dashboard_Istatistikleri;
    PRINT '   ✅ vw_Admin_Dashboard_Istatistikleri taşındı';
END

IF OBJECT_ID('dbo.vw_Admin_Rezervasyonlar', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Admin_Rezervasyonlar;
    PRINT '   ✅ vw_Admin_Rezervasyonlar taşındı';
END

IF OBJECT_ID('dbo.vw_Admin_Seferler', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Admin_Seferler;
    PRINT '   ✅ vw_Admin_Seferler taşındı';
END

IF OBJECT_ID('dbo.vw_Admin_Dashboard_Ozet', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Admin_Dashboard_Ozet;
    PRINT '   ✅ vw_Admin_Dashboard_Ozet taşındı';
END

IF OBJECT_ID('dbo.vw_Admin_Gunluk_Finansal_Rapor', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Admin_Gunluk_Finansal_Rapor;
    PRINT '   ✅ vw_Admin_Gunluk_Finansal_Rapor taşındı';
END

-- Şirket view'ları
IF OBJECT_ID('dbo.vw_Sirket_Istatistikleri', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Sirket_Istatistikleri;
    PRINT '   ✅ vw_Sirket_Istatistikleri taşındı';
END

IF OBJECT_ID('dbo.vw_Sirket_Seferleri', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Sirket_Seferleri;
    PRINT '   ✅ vw_Sirket_Seferleri taşındı';
END

-- Kullanıcı view'ları
IF OBJECT_ID('dbo.vw_Kullanici_Istatistikleri', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Kullanici_Istatistikleri;
    PRINT '   ✅ vw_Kullanici_Istatistikleri taşındı';
END

-- Bilet ve sefer view'ları
IF OBJECT_ID('dbo.vw_Bilet_Detay', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Bilet_Detay;
    PRINT '   ✅ vw_Bilet_Detay taşındı';
END

IF OBJECT_ID('dbo.vw_Sefer_Detaylari', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Sefer_Detaylari;
    PRINT '   ✅ vw_Sefer_Detaylari taşındı';
END

-- Raporlama view'ları
IF OBJECT_ID('dbo.vw_Guzergah_Ciro_Raporu', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Guzergah_Ciro_Raporu;
    PRINT '   ✅ vw_Guzergah_Ciro_Raporu taşındı';
END

IF OBJECT_ID('dbo.vw_Tum_Lokasyonlar_Union', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Tum_Lokasyonlar_Union;
    PRINT '   ✅ vw_Tum_Lokasyonlar_Union taşındı';
END

IF OBJECT_ID('dbo.vw_Bekleyen_Iptaller', 'V') IS NOT NULL
BEGIN
    ALTER SCHEMA report TRANSFER dbo.vw_Bekleyen_Iptaller;
    PRINT '   ✅ vw_Bekleyen_Iptaller taşındı';
END

PRINT '';
GO


-- 5. STORED PROCEDURE'LERİ proc SCHEMA'SINA TAŞI

PRINT '5. Stored procedure''ler proc schema''sına taşınıyor...';

-- Kullanıcı stored procedure'leri
IF OBJECT_ID('dbo.sp_Kullanici_Kayit', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Kullanici_Kayit;
    PRINT '   ✅ sp_Kullanici_Kayit taşındı';
END

IF OBJECT_ID('dbo.sp_Kullanici_Giris', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Kullanici_Giris;
    PRINT '   ✅ sp_Kullanici_Giris taşındı';
END

IF OBJECT_ID('dbo.sp_Kullanici_Istatistikleri_Getir', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Kullanici_Istatistikleri_Getir;
    PRINT '   ✅ sp_Kullanici_Istatistikleri_Getir taşındı';
END

IF OBJECT_ID('dbo.sp_Kullanici_Biletleri', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Kullanici_Biletleri;
    PRINT '   ✅ sp_Kullanici_Biletleri taşındı';
END

IF OBJECT_ID('dbo.sp_Kullanici_Raporu', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Kullanici_Raporu;
    PRINT '   ✅ sp_Kullanici_Raporu taşındı';
END

-- Rezervasyon stored procedure'leri
IF OBJECT_ID('dbo.sp_Rezervasyon_Yap', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Rezervasyon_Yap;
    PRINT '   ✅ sp_Rezervasyon_Yap taşındı';
END

IF OBJECT_ID('dbo.sp_Odeme_Tamamla', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Odeme_Tamamla;
    PRINT '   ✅ sp_Odeme_Tamamla taşındı';
END

-- Şirket stored procedure'leri
IF OBJECT_ID('dbo.sp_Sirket_Istatistikleri_Getir', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sirket_Istatistikleri_Getir;
    PRINT '   ✅ sp_Sirket_Istatistikleri_Getir taşındı';
END

IF OBJECT_ID('dbo.sp_Sirket_Seferleri_Getir', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sirket_Seferleri_Getir;
    PRINT '   ✅ sp_Sirket_Seferleri_Getir taşındı';
END

IF OBJECT_ID('dbo.sp_Sirket_Sefer_Ekle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sirket_Sefer_Ekle;
    PRINT '   ✅ sp_Sirket_Sefer_Ekle taşındı';
END

IF OBJECT_ID('dbo.sp_Sirket_Sefer_Guncelle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sirket_Sefer_Guncelle;
    PRINT '   ✅ sp_Sirket_Sefer_Guncelle taşındı';
END

IF OBJECT_ID('dbo.sp_Sirket_Sefer_Iptal', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sirket_Sefer_Iptal;
    PRINT '   ✅ sp_Sirket_Sefer_Iptal taşındı';
END

-- Admin stored procedure'leri
IF OBJECT_ID('dbo.sp_Admin_Kullanicilari_Getir', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Kullanicilari_Getir;
    PRINT '   ✅ sp_Admin_Kullanicilari_Getir taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Kullanici_Durum_Degistir', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Kullanici_Durum_Degistir;
    PRINT '   ✅ sp_Admin_Kullanici_Durum_Degistir taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Kullanici_Guncelle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Kullanici_Guncelle;
    PRINT '   ✅ sp_Admin_Kullanici_Guncelle taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Arac_Ekle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Arac_Ekle;
    PRINT '   ✅ sp_Admin_Arac_Ekle taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Arac_Guncelle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Arac_Guncelle;
    PRINT '   ✅ sp_Admin_Arac_Guncelle taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Sefer_Ekle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Sefer_Ekle;
    PRINT '   ✅ sp_Admin_Sefer_Ekle taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Sefer_Guncelle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Sefer_Guncelle;
    PRINT '   ✅ sp_Admin_Sefer_Guncelle taşındı';
END

IF OBJECT_ID('dbo.sp_Admin_Sefer_Iptal', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Admin_Sefer_Iptal;
    PRINT '   ✅ sp_Admin_Sefer_Iptal taşındı';
END

-- Otomatik işlem stored procedure'leri
IF OBJECT_ID('dbo.sp_Zaman_Asimi_Rezervasyonlar', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Zaman_Asimi_Rezervasyonlar;
    PRINT '   ✅ sp_Zaman_Asimi_Rezervasyonlar taşındı';
END

IF OBJECT_ID('dbo.sp_Otomatik_Iptal_Ayarlari', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Otomatik_Iptal_Ayarlari;
    PRINT '   ✅ sp_Otomatik_Iptal_Ayarlari taşındı';
END

IF OBJECT_ID('dbo.sp_Otomatik_Zam_Cursor', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Otomatik_Zam_Cursor;
    PRINT '   ✅ sp_Otomatik_Zam_Cursor taşındı';
END

-- Bildirim stored procedure'leri
IF OBJECT_ID('dbo.sp_Bildirim_Gonder', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Bildirim_Gonder;
    PRINT '   ✅ sp_Bildirim_Gonder taşındı';
END

IF OBJECT_ID('dbo.sp_Bildirim_Kuyrugu_Isle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Bildirim_Kuyrugu_Isle;
    PRINT '   ✅ sp_Bildirim_Kuyrugu_Isle taşındı';
END

IF OBJECT_ID('dbo.sp_Bildirim_Durum_Guncelle', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Bildirim_Durum_Guncelle;
    PRINT '   ✅ sp_Bildirim_Durum_Guncelle taşındı';
END

-- Bilet stored procedure'leri
IF OBJECT_ID('dbo.sp_Bilet_Bilgileri', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Bilet_Bilgileri;
    PRINT '   ✅ sp_Bilet_Bilgileri taşındı';
END

-- Sefer stored procedure'leri
IF OBJECT_ID('dbo.sp_Seferleri_Listele', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Seferleri_Listele;
    PRINT '   ✅ sp_Seferleri_Listele taşındı';
END

IF OBJECT_ID('dbo.sp_Sefer_Koltuk_Durumu', 'P') IS NOT NULL
BEGIN
    ALTER SCHEMA [proc] TRANSFER dbo.sp_Sefer_Koltuk_Durumu;
    PRINT '   ✅ sp_Sefer_Koltuk_Durumu taşındı';
END

PRINT '';
GO


-- 6. TRIGGER'LARI app SCHEMA'SINA TAŞI

-- NOT: SQL Server trigger'ların hedef tablo ile aynı schema'da olmasını zorunlu kılar
-- Bu yüzden trigger'lar app schema'sına taşınır
PRINT '6. Trigger''lar app schema''sına taşınıyor...';

IF OBJECT_ID('dbo.trg_Rezervasyon_Sonrasi_Koltuk_Guncelle', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA app TRANSFER dbo.trg_Rezervasyon_Sonrasi_Koltuk_Guncelle;
    PRINT '   ✅ trg_Rezervasyon_Sonrasi_Koltuk_Guncelle taşındı';
END

IF OBJECT_ID('dbo.trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar;
    PRINT '   ✅ trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar taşındı';
END

IF OBJECT_ID('dbo.trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur;
    PRINT '   ✅ trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur taşındı';
END

IF OBJECT_ID('dbo.trg_Sefer_Guncellendiginde_Log_Tut', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Sefer_Guncellendiginde_Log_Tut;
    PRINT '   ✅ trg_Sefer_Guncellendiginde_Log_Tut taşındı';
END

IF OBJECT_ID('dbo.trg_Odeme_Islemleri_Logla', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Odeme_Islemleri_Logla;
    PRINT '   ✅ trg_Odeme_Islemleri_Logla taşındı';
END

IF OBJECT_ID('dbo.trg_Bilet_Numarasi', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Bilet_Numarasi;
    PRINT '   ✅ trg_Bilet_Numarasi taşındı';
END

IF OBJECT_ID('dbo.trg_Rezervasyon_Bildirim', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Rezervasyon_Bildirim;
    PRINT '   ✅ trg_Rezervasyon_Bildirim taşındı';
END

IF OBJECT_ID('dbo.trg_Odeme_Bildirim', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Odeme_Bildirim;
    PRINT '   ✅ trg_Odeme_Bildirim taşındı';
END

IF OBJECT_ID('dbo.trg_Iptal_Bildirim', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Iptal_Bildirim;
    PRINT '   ✅ trg_Iptal_Bildirim taşındı';
END

IF OBJECT_ID('dbo.trg_Kullanici_Kayit_Bildirim', 'TR') IS NOT NULL
BEGIN
    ALTER SCHEMA trg TRANSFER dbo.trg_Kullanici_Kayit_Bildirim;
    PRINT '   ✅ trg_Kullanici_Kayit_Bildirim taşındı';
END

PRINT '';
GO


-- 7. FUNCTION'LARI func SCHEMA'SINA TAŞI

PRINT '7. Function''lar func schema''sına taşınıyor...';

IF OBJECT_ID('dbo.fn_Toplam_Harcama', 'FN') IS NOT NULL
BEGIN
    ALTER SCHEMA func TRANSFER dbo.fn_Toplam_Harcama;
    PRINT '   ✅ fn_Toplam_Harcama taşındı';
END

IF OBJECT_ID('dbo.fn_Seyahat_Sayisi', 'FN') IS NOT NULL
BEGIN
    ALTER SCHEMA func TRANSFER dbo.fn_Seyahat_Sayisi;
    PRINT '   ✅ fn_Seyahat_Sayisi taşındı';
END

IF OBJECT_ID('dbo.fn_Ortalama_Seyahat_Fiyati', 'FN') IS NOT NULL
BEGIN
    ALTER SCHEMA func TRANSFER dbo.fn_Ortalama_Seyahat_Fiyati;
    PRINT '   ✅ fn_Ortalama_Seyahat_Fiyati taşındı';
END

PRINT '';
GO


-- 8. SCHEMA İZİNLERİNİ AYARLA

PRINT '8. Schema izinleri ayarlanıyor...';

-- app schema için SELECT, INSERT, UPDATE, DELETE izinleri
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::app TO PUBLIC;
PRINT '   ✅ app schema izinleri verildi';

-- log schema için SELECT, INSERT izinleri (log tabloları genelde sadece yazılır)
GRANT SELECT, INSERT ON SCHEMA::log TO PUBLIC;
PRINT '   ✅ log schema izinleri verildi';

-- report schema için SELECT izinleri (view'lar sadece okunur)
GRANT SELECT ON SCHEMA::report TO PUBLIC;
PRINT '   ✅ report schema izinleri verildi';

-- proc schema için EXECUTE izinleri
GRANT EXECUTE ON SCHEMA::[proc] TO PUBLIC;
PRINT '   ✅ proc schema izinleri verildi';

-- trg schema için izin gerekmez (trigger'lar otomatik çalışır)
PRINT '   ✅ trg schema (trigger''lar otomatik çalışır)';

-- func schema için EXECUTE izinleri
GRANT EXECUTE ON SCHEMA::func TO PUBLIC;
PRINT '   ✅ func schema izinleri verildi';

PRINT '';
GO


