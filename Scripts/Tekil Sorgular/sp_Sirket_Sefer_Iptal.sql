-- =============================================
-- Stored Procedure: sp_Sirket_Sefer_Iptal
-- Açıklama: Şirket panelinde sefer iptal eder (güvenlik ve geçmiş sefer kontrolü ile)
-- Parametreler:
--   @SirketID: Şirket ID (JWT token'dan alınır)
--   @SeferID: İptal edilecek sefer ID
--   @IptalNedeni: İptal nedeni
-- =============================================

CREATE OR ALTER PROCEDURE sp_Sirket_Sefer_Iptal
    @SirketID INT,
    @SeferID INT,
    @IptalNedeni NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- GÜVENLİK KONTROLÜ
        IF NOT EXISTS (
            SELECT 1 FROM dbo.Trips T
            INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
            WHERE T.TripID = @SeferID AND V.CompanyID = @SirketID
        )
        BEGIN
            THROW 50001, 'Bu sefere müdahale yetkiniz yok.', 1;
        END

        -- Tarih Kontrolü
        IF EXISTS (SELECT 1 FROM dbo.Trips WHERE TripID = @SeferID AND DepartureDate < GETDATE())
            THROW 50002, 'Geçmiş sefer iptal edilemez.', 1;

        UPDATE dbo.Trips SET Status = 0 WHERE TripID = @SeferID;

        -- Manuel Log
        INSERT INTO dbo.TripLogs (TripID, Action, OldValue, NewValue, LogDate, Description)
        VALUES (@SeferID, 'CancelByCompany', 'Active', 'Passive', SYSUTCDATETIME(), 'İptal Nedeni: ' + @IptalNedeni + ' | Şirket: ' + CAST(@SirketID AS NVARCHAR));

        SELECT 'Sefer iptal edildi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

