-- =============================================
-- PaymentLogs Tablosu Oluşturma
-- =============================================
-- Bu script PaymentLogs tablosunu oluşturur

USE [RayBusDB]
GO

-- PaymentLogs tablosunu oluştur
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.PaymentLogs') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.PaymentLogs (
        LogID INT IDENTITY(1,1) PRIMARY KEY,
        PaymentID INT NOT NULL,
        Action NVARCHAR(50) NOT NULL, -- Örn: 'PaymentCreated', 'StatusChanged'
        OldStatus NVARCHAR(30) NULL,  -- Eski Durum
        NewStatus NVARCHAR(30) NULL,  -- Yeni Durum
        LogDate DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
        Description NVARCHAR(300) NULL, -- Ek açıklama
        
        CONSTRAINT FK_PaymentLogs_Payments FOREIGN KEY (PaymentID) REFERENCES dbo.Payments(PaymentID)
    );

    -- Index oluştur
    CREATE INDEX IX_PaymentLogs_PaymentID ON dbo.PaymentLogs(PaymentID);

    PRINT 'PaymentLogs tablosu başarıyla oluşturuldu.';
END
ELSE
BEGIN
    PRINT 'PaymentLogs tablosu zaten mevcut.';
END
GO

