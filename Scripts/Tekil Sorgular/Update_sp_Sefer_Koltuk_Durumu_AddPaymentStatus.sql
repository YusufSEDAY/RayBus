-- =============================================
-- sp_Sefer_Koltuk_Durumu - PaymentStatus Ekleme
-- =============================================
-- Bu script sp_Sefer_Koltuk_Durumu stored procedure'ünü güncelleyerek
-- PaymentStatus bilgisini de döndürmesini sağlar

USE [RayBusDB]
GO

-- Mevcut stored procedure'ü sil
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Sefer_Koltuk_Durumu')
    DROP PROCEDURE sp_Sefer_Koltuk_Durumu;
GO

-- Güncellenmiş stored procedure'ü oluştur
CREATE PROCEDURE sp_Sefer_Koltuk_Durumu
    @SeferID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        TS.SeatID,
        S.SeatNo,          -- Koltuk Numarası (1A, 2B vb.)
        S.SeatPosition,    -- Cam Kenarı / Koridor
        TS.IsReserved,     -- 1: Dolu, 0: Boş
        R.PaymentStatus,   -- 'Pending', 'Paid', 'Refunded' veya NULL
        
        -- Eğer Tren ise Vagon numarasını da getir, Otobüs ise NULL gelir
        W.WagonNo AS VagonNo

    FROM dbo.TripSeats TS
    INNER JOIN dbo.Seats S ON TS.SeatID = S.SeatID
    LEFT JOIN dbo.Wagons W ON S.WagonID = W.WagonID
    LEFT JOIN dbo.Reservations R ON TS.TripID = R.TripID AND TS.SeatID = R.SeatID AND R.Status = 'Reserved'
    WHERE TS.TripID = @SeferID
    
    -- Listeyi önce Vagon numarasına (Trense), sonra Koltuk Numarasına göre sırala
    ORDER BY W.WagonNo, LEN(S.SeatNo), S.SeatNo;
END;
GO

PRINT 'sp_Sefer_Koltuk_Durumu stored procedure''ü PaymentStatus ile güncellendi.';
GO

