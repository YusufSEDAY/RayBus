-- =============================================
-- Trigger: trg_Sefer_Guncellendiginde_Log_Tut
-- Açıklama: Sefer güncellendiğinde otomatik log tutar
-- Tetiklenme: Trips tablosunda AFTER UPDATE
-- =============================================

ALTER TRIGGER trg_Sefer_Guncellendiginde_Log_Tut
ON dbo.Trips
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Sadece Fiyat veya Tarih/Saat değiştiyse log tutalım
    IF UPDATE(Price) OR UPDATE(DepartureDate) OR UPDATE(DepartureTime) OR UPDATE(Status)
    BEGIN
        DECLARE @TripID INT;
        SELECT @TripID = TripID FROM inserted;

        -- Fiyat Değişimi Logu
        IF UPDATE(Price)
        BEGIN
            INSERT INTO dbo.TripLogs (TripID, ColumnName, Action, OldValue, NewValue, LogDate, Description)
            SELECT 
                i.TripID, 
                'Price',
                'FiyatDegisimi', 
                CAST(d.Price AS NVARCHAR), 
                CAST(i.Price AS NVARCHAR), 
                SYSUTCDATETIME(), 
                'Otomatik Trigger Logu: Fiyat Güncellendi'
            FROM inserted i
            INNER JOIN deleted d ON i.TripID = d.TripID
            WHERE i.Price <> d.Price;
        END

        -- Tarih Değişimi Logu
        IF UPDATE(DepartureDate)
        BEGIN
            INSERT INTO dbo.TripLogs (TripID, ColumnName, Action, OldValue, NewValue, LogDate, Description)
            SELECT 
                i.TripID, 
                'DepartureDate',
                'TarihDegisimi', 
                CAST(d.DepartureDate AS NVARCHAR), 
                CAST(i.DepartureDate AS NVARCHAR), 
                SYSUTCDATETIME(), 
                'Otomatik Trigger Logu: Tarih Güncellendi'
            FROM inserted i
            INNER JOIN deleted d ON i.TripID = d.TripID
            WHERE i.DepartureDate <> d.DepartureDate;
        END

        -- Durum Değişimi Logu (İptal vs.)
        IF UPDATE(Status)
        BEGIN
            INSERT INTO dbo.TripLogs (TripID, ColumnName, Action, OldValue, NewValue, LogDate, Description)
            SELECT 
                i.TripID, 
                'Status',
                'DurumDegisikligi', 
                CAST(d.Status AS NVARCHAR), 
                CAST(i.Status AS NVARCHAR), 
                SYSUTCDATETIME(), 
                'Otomatik Trigger Logu: Sefer Durumu Değişti'
            FROM inserted i
            INNER JOIN deleted d ON i.TripID = d.TripID
            WHERE i.Status <> d.Status;
        END
    END
END;
GO

