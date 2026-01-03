-- =============================================
-- Stored Procedure: sp_Admin_Kullanici_Durum_Degistir
-- Açıklama: Admin panelinde kullanıcı durumunu değiştirir (Aktif/Pasif)
-- Parametreler:
--   @UserID: Güncellenecek kullanıcı ID
--   @YeniDurum: 0 = Pasif (Banla), 1 = Aktif
--   @AdminID: İşlemi yapan admin ID (güvenlik kontrolü için)
--   @Sebep: Ban nedeni (gelecekte log için)
-- =============================================

CREATE PROCEDURE sp_Admin_Kullanici_Durum_Degistir
    @UserID INT,
    @YeniDurum BIT, -- 0: Pasife Al (Banla), 1: Aktif Et
    @AdminID INT,   -- İşlemi yapan Admin (Loglamak istersek diye)
    @Sebep NVARCHAR(200) -- Neden banlandı?
AS
BEGIN
    SET NOCOUNT ON;

    -- Admin kendini banlayamasın :)
    IF @UserID = @AdminID
    BEGIN
        THROW 50001, 'Yönetici kendi hesabını pasife alamaz.', 1;
    END

    UPDATE dbo.Users
    SET Status = @YeniDurum
    WHERE UserID = @UserID;

    -- İstersen buraya bir "Audit Log" ekleyebiliriz ama şimdilik işlem yeterli.
    SELECT 'Kullanıcı durumu güncellendi.' AS Mesaj;
END;
GO

