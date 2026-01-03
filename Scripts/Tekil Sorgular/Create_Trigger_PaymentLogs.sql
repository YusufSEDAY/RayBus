-- =============================================
-- PaymentLogs Trigger Oluşturma
-- =============================================
-- Bu script Payments tablosu için otomatik loglama trigger'ını oluşturur

USE [RayBusDB]
GO

-- Mevcut trigger'ı sil (varsa)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.trg_Odeme_Islemleri_Logla') AND type = 'TR')
BEGIN
    DROP TRIGGER dbo.trg_Odeme_Islemleri_Logla;
    PRINT 'Mevcut trigger silindi.';
END
GO

-- Yeni trigger'ı oluştur
CREATE TRIGGER trg_Odeme_Islemleri_Logla
ON dbo.Payments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. DURUM: Yeni Bir Ödeme Eklendi mi? (INSERT işlemi)
    -- Inserted tablosunda veri var, Deleted tablosunda yoksa bu bir EKLEME işlemidir.
    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO dbo.PaymentLogs (PaymentID, Action, NewStatus, Description)
        SELECT 
            i.PaymentID, 
            'Olusturuldu', -- Action tipi
            i.Status, 
            'Yeni ödeme kaydı oluşturuldu. Tutar: ' + CAST(i.Amount AS NVARCHAR(20))
        FROM inserted i;
    END

    -- 2. DURUM: Ödeme Güncellendi mi? (UPDATE işlemi)
    -- Hem Inserted hem Deleted tablosunda veri varsa bu bir GÜNCELLEME işlemidir.
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        -- Sadece 'Status' kolonu değiştiyse log tutalım
        IF UPDATE(Status)
        BEGIN
            INSERT INTO dbo.PaymentLogs (PaymentID, Action, OldStatus, NewStatus, Description)
            SELECT 
                i.PaymentID, 
                'DurumDegisikligi', -- Action tipi
                d.Status, -- Eski Durum
                i.Status, -- Yeni Durum
                'Ödeme durumu güncellendi.'
            FROM inserted i
            INNER JOIN deleted d ON i.PaymentID = d.PaymentID
            WHERE i.Status <> d.Status; -- Eski ve yeni durum gerçekten farklıysa
        END
    END
END;
GO

PRINT 'trg_Odeme_Islemleri_Logla trigger''ı başarıyla oluşturuldu.';
GO

