-- =============================================
-- Stored Procedure: sp_Admin_Kullanicilari_Getir
-- Açıklama: Admin panel için kullanıcıları getirir (arama ve filtreleme ile)
-- Parametreler:
--   @AramaMetni: İsim veya Email ile arama (NULL = hepsi)
--   @RolID: Rol bazlı filtreleme (NULL = hepsi)
-- =============================================

CREATE PROCEDURE sp_Admin_Kullanicilari_Getir
    @AramaMetni NVARCHAR(50) = NULL, -- İsim veya Email (Boş gelirse hepsi)
    @RolID INT = NULL -- Sadece Müşterileri veya Şirketleri getir diyebilir
AS
BEGIN
    SELECT 
        U.UserID,
        U.FullName,
        U.Email,
        U.Phone,
        R.RoleName,
        U.Status AS Durum, -- 1: Aktif, 0: Pasif
        U.CreatedAt AS KayitTarihi,
        -- Kullanıcının Toplam Harcaması (Bonus Bilgi)
        (SELECT ISNULL(SUM(Amount), 0) 
         FROM dbo.Payments P 
         INNER JOIN dbo.Reservations Res ON P.ReservationID = Res.ReservationID 
         WHERE Res.UserID = U.UserID) AS ToplamHarcama
    FROM dbo.Users U
    INNER JOIN dbo.Roles R ON U.RoleID = R.RoleID
    WHERE 
        (@RolID IS NULL OR U.RoleID = @RolID)
        AND
        (@AramaMetni IS NULL OR (U.FullName LIKE '%' + @AramaMetni + '%' OR U.Email LIKE '%' + @AramaMetni + '%'))
    ORDER BY U.CreatedAt DESC;
END;
GO

