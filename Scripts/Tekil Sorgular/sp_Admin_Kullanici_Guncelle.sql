-- =============================================
-- Stored Procedure: sp_Admin_Kullanici_Guncelle
-- Açıklama: Admin panelinde kullanıcı bilgilerini günceller
-- Parametreler:
--   @UserID: Güncellenecek kullanıcı ID
--   @FullName: Yeni ad soyad (NULL ise güncellenmez)
--   @Email: Yeni email (NULL ise güncellenmez)
--   @Phone: Yeni telefon (NULL ise güncellenmez)
-- =============================================

CREATE OR ALTER PROCEDURE sp_Admin_Kullanici_Guncelle
    @UserID INT,
    @FullName NVARCHAR(100) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Kullanıcı var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserID = @UserID)
        BEGIN
            THROW 50001, 'Kullanıcı bulunamadı.', 1;
        END

        -- Email kontrolü (eğer değiştiriliyorsa)
        IF @Email IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email AND UserID != @UserID)
            BEGIN
                THROW 50002, 'Bu email adresi başka bir kullanıcı tarafından kullanılıyor.', 1;
            END
        END

        -- Güncelleme işlemi (sadece NULL olmayan alanlar güncellenir)
        UPDATE dbo.Users
        SET 
            FullName = ISNULL(@FullName, FullName),
            Email = ISNULL(@Email, Email),
            Phone = ISNULL(@Phone, Phone)
        WHERE UserID = @UserID;

        SELECT 'Kullanıcı bilgileri başarıyla güncellendi.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

