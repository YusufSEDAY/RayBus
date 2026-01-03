-- =============================================
-- sp_Kullanici_Giris - Kullanıcı Giriş Stored Procedure
-- =============================================
-- Backend BCrypt ile hash'lenmiş şifreyi gönderir
-- Stored procedure hash'lenmiş şifreyi direkt karşılaştırır
-- NOT: Şifre karşılaştırması backend'de yapılacak (BCrypt VerifyPassword)

USE [RayBusDB]
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Kullanici_Giris')
    DROP PROCEDURE sp_Kullanici_Giris;
GO

CREATE PROCEDURE sp_Kullanici_Giris
    @Email NVARCHAR(150)
    -- @Sifre parametresi kaldırıldı - şifre kontrolü backend'de BCrypt ile yapılacak
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID INT;
    DECLARE @AdSoyad NVARCHAR(100);
    DECLARE @RoleID INT;
    DECLARE @RoleName NVARCHAR(50);
    DECLARE @PasswordHash NVARCHAR(300);
    DECLARE @Telefon NVARCHAR(15);
    DECLARE @Durum TINYINT;
    DECLARE @CreatedAt DATETIME2;

    -- 1. Kullanıcıyı Email ile bul
    SELECT 
        @UserID = U.UserID,
        @AdSoyad = U.FullName,
        @RoleID = U.RoleID,
        @RoleName = R.RoleName,
        @PasswordHash = U.PasswordHash,
        @Telefon = U.Phone,
        @Durum = U.Status,
        @CreatedAt = U.CreatedAt
    FROM dbo.Users U
    INNER JOIN dbo.Roles R ON U.RoleID = R.RoleID
    WHERE U.Email = @Email;

    -- 2. Kullanıcı yoksa
    IF @UserID IS NULL
    BEGIN
        SELECT 
            CAST(0 AS BIT) AS Basarili, 
            'E-posta veya şifre hatalı.' AS Mesaj,
            NULL AS UserID,
            NULL AS AdSoyad,
            NULL AS RoleID,
            NULL AS RolAdi,
            NULL AS PasswordHash,
            NULL AS Telefon,
            NULL AS CreatedAt;
        RETURN;
    END

    -- 3. Kullanıcı Pasif ise (Banlanmış vs.)
    IF @Durum = 0
    BEGIN
        SELECT 
            CAST(0 AS BIT) AS Basarili, 
            'Hesabınız pasif durumdadır. Yönetici ile görüşün.' AS Mesaj,
            NULL AS UserID,
            NULL AS AdSoyad,
            NULL AS RoleID,
            NULL AS RolAdi,
            NULL AS PasswordHash,
            NULL AS Telefon,
            NULL AS CreatedAt;
        RETURN;
    END

    -- 4. Kullanıcı bulundu - Şifre kontrolü backend'de BCrypt ile yapılacak
    -- Frontend'in ihtiyacı olan tüm bilgileri dön (şifre hash'i dahil - backend'de kontrol için)
    SELECT 
        CAST(1 AS BIT) AS Basarili, 
        'Kullanıcı bulundu' AS Mesaj,
        @UserID AS UserID,
        @AdSoyad AS AdSoyad,
        @RoleID AS RoleID,
        @RoleName AS RolAdi,
        @PasswordHash AS PasswordHash, -- Backend'de BCrypt ile karşılaştırılacak
        @Telefon AS Telefon,
        @CreatedAt AS CreatedAt;
END;
GO

PRINT 'sp_Kullanici_Giris stored procedure''ü oluşturuldu.';
GO

