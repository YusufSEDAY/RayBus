-- =============================================
-- sp_Kullanici_Kayit - Kullanıcı Kayıt Stored Procedure
-- =============================================
-- Backend BCrypt ile hash'lenmiş şifreyi gönderir
-- Stored procedure hash'lenmiş şifreyi direkt kaydeder

USE [RayBusDB]
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Kullanici_Kayit')
    DROP PROCEDURE sp_Kullanici_Kayit;
GO

CREATE PROCEDURE sp_Kullanici_Kayit
    @AdSoyad NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(300), -- BCrypt ile hash'lenmiş şifre (backend'den gelir)
    @Telefon NVARCHAR(15),
    @RolAdi NVARCHAR(50) -- 'Müşteri' veya 'Şirket'
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Bu e-posta zaten var mı?
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
        BEGIN
            THROW 50001, 'Bu e-posta adresi ile daha önce kayıt olunmuş.', 1;
        END

        -- 2. GÜVENLİK KONTROLÜ: Biri 'Admin' olmaya mı çalışıyor?
        -- Web formundan Admin olunamaz!
        IF @RolAdi = 'Admin'
        BEGIN
            THROW 50002, 'Güvenlik ihlali: Admin rolü ile dışarıdan kayıt olunamaz.', 1;
        END

        -- 3. ROL ID BULMA
        DECLARE @SecilenRoleID INT;
        SELECT @SecilenRoleID = RoleID FROM dbo.Roles WHERE RoleName = @RolAdi;

        -- Eğer geçersiz bir rol adı geldiyse (Örn: 'Hacker', 'Bilinmeyen')
        IF @SecilenRoleID IS NULL
        BEGIN
            THROW 50003, 'Geçersiz rol seçimi. Lütfen Müşteri veya Şirket seçiniz.', 1;
        END

        -- 4. KAYIT İŞLEMİ (Hash'lenmiş şifreyi direkt kaydet)
        INSERT INTO dbo.Users (RoleID, FullName, Email, PasswordHash, Phone, Status, CreatedAt)
        VALUES (
            @SecilenRoleID, 
            @AdSoyad, 
            @Email, 
            @PasswordHash, -- Backend'den gelen BCrypt hash'i direkt kaydet
            @Telefon, 
            1, -- Status: Aktif
            SYSUTCDATETIME() -- CreatedAt: UTC zaman
        );

        -- Başarılı mesajı dön
        SELECT 'Kayıt Başarılı' AS Mesaj, CAST(SCOPE_IDENTITY() AS INT) AS YeniUserID;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT 'sp_Kullanici_Kayit stored procedure''ü oluşturuldu.';
GO

