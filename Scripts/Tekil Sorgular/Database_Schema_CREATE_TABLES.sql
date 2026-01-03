-- =============================================
-- RayBus - Tüm Tabloların CREATE Komutları
-- =============================================
-- Bu dosya tüm veritabanı tablolarının CREATE komutlarını içerir
-- Veritabanı: RayBusDB

USE [RayBusDB]
GO

-- =============================================
-- 1. Roles Tablosu
-- =============================================
CREATE TABLE dbo.Roles
(
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL
);
GO

-- =============================================
-- 2. Cities Tablosu
-- =============================================
CREATE TABLE dbo.Cities 
(
    CityID      INT IDENTITY(1,1) PRIMARY KEY,
    CityName    NVARCHAR(100) NOT NULL
);
GO

CREATE UNIQUE INDEX UX_Cities_CityName ON dbo.Cities(CityName); -- Aynı şehir adının alınmamasını sağlar
GO

-- =============================================
-- 3. Users Tablosu
-- =============================================
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    RoleID          INT NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    Email           NVARCHAR(150) NOT NULL,
    PasswordHash    NVARCHAR(300) NOT NULL,
    Phone           NVARCHAR(30) NULL,
    CreatedAt       DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    Status          TINYINT DEFAULT 1 NOT NULL, -- 1=Active,0=Inactive
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
);
GO

CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
GO

-- =============================================
-- 4. Terminals Tablosu
-- =============================================
CREATE TABLE dbo.Terminals (
    TerminalID      INT IDENTITY(1,1) PRIMARY KEY,
    CityID          INT NOT NULL,
    TerminalName    NVARCHAR(150) NOT NULL,
    Address         NVARCHAR(300) NULL,
    CONSTRAINT FK_Terminals_Cities FOREIGN KEY (CityID) REFERENCES dbo.Cities(CityID)
);
GO

CREATE INDEX IX_Terminals_CityID ON dbo.Terminals(CityID);
GO

-- =============================================
-- 5. Stations Tablosu
-- =============================================
CREATE TABLE dbo.Stations (
    StationID       INT IDENTITY(1,1) PRIMARY KEY,
    CityID          INT NOT NULL,
    StationName     NVARCHAR(150) NOT NULL,
    Address         NVARCHAR(300) NULL,
    CONSTRAINT FK_Stations_Cities FOREIGN KEY (CityID) REFERENCES dbo.Cities(CityID)
);
GO

CREATE INDEX IX_Stations_CityID ON dbo.Stations(CityID);
GO

-- =============================================
-- 6. Vehicles Tablosu
-- =============================================
CREATE TABLE dbo.Vehicles (
    VehicleID       INT IDENTITY(1,1) PRIMARY KEY,
    VehicleType     NVARCHAR(20) NOT NULL, 
    PlateOrCode     NVARCHAR(100) NULL,
    SeatCount       INT NOT NULL DEFAULT 0,
    Active          BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT CHK_Vehicles_Type CHECK (VehicleType IN ('Bus','Train')) -- Vehicles tablosundaki VehicleType kolonuna sadece 'Bus' veya 'Train' değerleri girilebilir.
);
GO

CREATE INDEX IX_Vehicles_Type ON dbo.Vehicles(VehicleType);
GO

-- =============================================
-- 7. Buses Tablosu
-- =============================================
CREATE TABLE dbo.Buses (
    BusID           INT PRIMARY KEY, 
    BusModel        NVARCHAR(100) NULL,
    LayoutType      NVARCHAR(20) NULL, 
    CONSTRAINT FK_Buses_Vehicles FOREIGN KEY (BusID) REFERENCES dbo.Vehicles(VehicleID) ON DELETE CASCADE -- İlişkili verileri silerken hata almayı önler. Vehicle tablosundan silindiğinde bağlı (ilişkili olduğu) otobüs de silinir.
);
GO

-- =============================================
-- 8. Trains Tablosu
-- =============================================
CREATE TABLE dbo.Trains (
    TrainID         INT PRIMARY KEY, 
    TrainModel      NVARCHAR(100) NULL,
    CONSTRAINT FK_Trains_Vehicles FOREIGN KEY (TrainID) REFERENCES dbo.Vehicles(VehicleID) ON DELETE CASCADE
);
GO

-- =============================================
-- 9. Wagons Tablosu
-- =============================================
CREATE TABLE dbo.Wagons (
    WagonID         INT IDENTITY(1,1) PRIMARY KEY,
    TrainID         INT NOT NULL,
    WagonNo         INT NOT NULL,
    SeatCount       INT NOT NULL,
    CONSTRAINT FK_Wagons_Trains FOREIGN KEY (TrainID) REFERENCES dbo.Trains(TrainID) ON DELETE CASCADE,
    CONSTRAINT UQ_Wagons_Train_WagonNo UNIQUE (TrainID, WagonNo)
);
GO

CREATE INDEX IX_Wagons_TrainID ON dbo.Wagons(TrainID);
GO

-- =============================================
-- 10. Seats Tablosu
-- =============================================
CREATE TABLE dbo.Seats (
    SeatID          INT IDENTITY(1,1) PRIMARY KEY,
    VehicleID       INT NOT NULL,
    WagonID         INT NULL,
    SeatNo          NVARCHAR(20) NOT NULL,
    SeatPosition    NVARCHAR(20) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Seats_Vehicles 
        FOREIGN KEY (VehicleID) 
        REFERENCES dbo.Vehicles(VehicleID) 
        ON DELETE CASCADE,
    CONSTRAINT FK_Seats_Wagons 
        FOREIGN KEY (WagonID) 
        REFERENCES dbo.Wagons(WagonID) 
        ON DELETE NO ACTION   -- ⬅ BURASI DEĞİŞTİ!
);
GO

CREATE UNIQUE INDEX UX_Seats_Vehicle_SeatNo 
ON dbo.Seats(VehicleID, SeatNo);
GO

CREATE INDEX IX_Seats_WagonID 
ON dbo.Seats(WagonID);
GO

-- =============================================
-- 11. Trips Tablosu
-- =============================================
CREATE TABLE dbo.Trips (
    TripID                  INT IDENTITY(1,1) PRIMARY KEY,
    VehicleID               INT NOT NULL,       -- hangi otobüs/tren ile
    FromCityID              INT NOT NULL,
    ToCityID                INT NOT NULL,
    DepartureTerminalID     INT NULL,           -- otobüs için terminal
    ArrivalTerminalID       INT NULL,
    DepartureStationID      INT NULL,           -- tren için istasyon
    ArrivalStationID        INT NULL,
    DepartureDate           DATE NOT NULL,
    DepartureTime           TIME(0) NOT NULL,
    ArrivalDate             DATE NULL,
    ArrivalTime             TIME(0) NULL,
    Price                   DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    Status                  TINYINT NOT NULL DEFAULT 1, -- 1=Active,0=Cancelled
    CreatedAt               DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT FK_Trips_Vehicles FOREIGN KEY (VehicleID) REFERENCES dbo.Vehicles(VehicleID),
    CONSTRAINT FK_Trips_FromCity FOREIGN KEY (FromCityID) REFERENCES dbo.Cities(CityID),
    CONSTRAINT FK_Trips_ToCity FOREIGN KEY (ToCityID) REFERENCES dbo.Cities(CityID),
    CONSTRAINT FK_Trips_DepTerminal FOREIGN KEY (DepartureTerminalID) REFERENCES dbo.Terminals(TerminalID),
    CONSTRAINT FK_Trips_ArrTerminal FOREIGN KEY (ArrivalTerminalID) REFERENCES dbo.Terminals(TerminalID),
    CONSTRAINT FK_Trips_DepStation FOREIGN KEY (DepartureStationID) REFERENCES dbo.Stations(StationID),
    CONSTRAINT FK_Trips_ArrStation FOREIGN KEY (ArrivalStationID) REFERENCES dbo.Stations(StationID),
    CONSTRAINT CHK_Trips_DifferentCities CHECK (FromCityID <> ToCityID)
);
GO

CREATE INDEX IX_Trips_FromToDate ON dbo.Trips(FromCityID, ToCityID, DepartureDate, DepartureTime);
GO

CREATE INDEX IX_Trips_VehicleDate ON dbo.Trips(VehicleID, DepartureDate);
GO

-- =============================================
-- 12. TripSeats Tablosu
-- =============================================
CREATE TABLE dbo.TripSeats (
    TripSeatID      INT IDENTITY(1,1) NOT NULL, -- opsiyonel surrogate
    TripID          INT NOT NULL,
    SeatID          INT NOT NULL,
    IsReserved      BIT NOT NULL DEFAULT 0,
    ReservedAt      DATETIME2 NULL,
    CONSTRAINT PK_TripSeats_Trip_Seat PRIMARY KEY (TripID, SeatID),
    CONSTRAINT UQ_TripSeats_TripSeatID UNIQUE (TripSeatID),
    CONSTRAINT FK_TripSeats_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID) ON DELETE CASCADE,
    CONSTRAINT FK_TripSeats_Seats FOREIGN KEY (SeatID) REFERENCES dbo.Seats(SeatID) ON DELETE CASCADE
);
GO

CREATE INDEX IX_TripSeats_TripID ON dbo.TripSeats(TripID);
GO

CREATE INDEX IX_TripSeats_IsReserved ON dbo.TripSeats(IsReserved);
GO

-- =============================================
-- 13. CancellationReasons Tablosu
-- =============================================
CREATE TABLE dbo.CancellationReasons (
    ReasonID        INT IDENTITY(1,1) PRIMARY KEY,
    ReasonText      NVARCHAR(300) NOT NULL
);
GO

-- =============================================
-- 14. Reservations Tablosu
-- =============================================
CREATE TABLE dbo.Reservations (
    ReservationID       INT IDENTITY(1,1) PRIMARY KEY,
    TripID              INT NOT NULL,
    SeatID              INT NOT NULL,
    UserID              INT NOT NULL,
    ReservationDate     DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    Status              NVARCHAR(30) NOT NULL DEFAULT 'Reserved', -- 'Reserved','Cancelled','Completed'
    PaymentStatus       NVARCHAR(30) NOT NULL DEFAULT 'Pending',  -- 'Pending','Paid','Refunded'
    CancelReasonID      INT NULL,
    CONSTRAINT FK_Reservations_TripSeat FOREIGN KEY (TripID, SeatID) REFERENCES dbo.TripSeats(TripID, SeatID),
    CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Reservations_CancelReason FOREIGN KEY (CancelReasonID) REFERENCES dbo.CancellationReasons(ReasonID)
);
GO

CREATE INDEX IX_Reservations_UserID ON dbo.Reservations(UserID);
GO

CREATE INDEX IX_Reservations_TripID ON dbo.Reservations(TripID);
GO

CREATE INDEX IX_Reservations_Status ON dbo.Reservations(Status);
GO

-- =============================================
-- 15. Payments Tablosu
-- =============================================
CREATE TABLE dbo.Payments (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    ReservationID   INT NOT NULL,
    Amount          DECIMAL(10,2) NOT NULL,
    PaymentDate     DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    PaymentMethod   NVARCHAR(50) NULL, -- 'Card', 'VirtualPOS', etc.
    Status          NVARCHAR(30) NOT NULL DEFAULT 'Completed', -- 'Completed','Failed','Refunded'
    TransactionRef  NVARCHAR(200) NULL,
    CONSTRAINT FK_Payments_Reservations FOREIGN KEY (ReservationID) REFERENCES dbo.Reservations(ReservationID) ON DELETE CASCADE
);
GO

CREATE INDEX IX_Payments_ReservationID ON dbo.Payments(ReservationID);
GO

-- =============================================
-- 16. TripLogs Tablosu
-- =============================================
CREATE TABLE dbo.TripLogs (
    LogID           INT IDENTITY(1,1) PRIMARY KEY,
    TripID          INT NOT NULL,
    ColumnName      NVARCHAR(100) NOT NULL,
    OldValue        NVARCHAR(400) NULL,
    NewValue        NVARCHAR(400) NULL,
    ChangedAt       DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    ChangedByUserID INT NULL,
    CONSTRAINT FK_TripLogs_Trips FOREIGN KEY (TripID) REFERENCES dbo.Trips(TripID),
    CONSTRAINT FK_TripLogs_Users FOREIGN KEY (ChangedByUserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE INDEX IX_TripLogs_TripID ON dbo.TripLogs(TripID);
GO

-- =============================================
-- 17. ReservationLogs Tablosu
-- =============================================
CREATE TABLE dbo.ReservationLogs (
    LogID           INT IDENTITY(1,1) PRIMARY KEY,
    ReservationID   INT NOT NULL,
    Action          NVARCHAR(50) NOT NULL, -- 'Created','Cancelled','Updated'
    Details         NVARCHAR(400) NULL,
    LogDate         DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    PerformedBy     INT NULL,
    CONSTRAINT FK_ReservationLogs_Reservation FOREIGN KEY (ReservationID) REFERENCES dbo.Reservations(ReservationID),
    CONSTRAINT FK_ReservationLogs_User FOREIGN KEY (PerformedBy) REFERENCES dbo.Users(UserID)
);
GO

CREATE INDEX IX_ReservationLogs_ReservationID ON dbo.ReservationLogs(ReservationID);
GO

-- =============================================
-- 18. Settings Tablosu
-- =============================================
CREATE TABLE dbo.Settings (
    SettingKey      NVARCHAR(100) PRIMARY KEY,
    SettingValue    NVARCHAR(500) NULL,
    UpdatedAt       DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL
);
GO

PRINT '=============================================';
PRINT 'Tüm tablolar başarıyla oluşturuldu!';
PRINT 'Toplam 18 tablo:';
PRINT '  1. Roles';
PRINT '  2. Cities';
PRINT '  3. Users';
PRINT '  4. Terminals';
PRINT '  5. Stations';
PRINT '  6. Vehicles';
PRINT '  7. Buses';
PRINT '  8. Trains';
PRINT '  9. Wagons';
PRINT '  10. Seats';
PRINT '  11. Trips';
PRINT '  12. TripSeats';
PRINT '  13. CancellationReasons';
PRINT '  14. Reservations';
PRINT '  15. Payments';
PRINT '  16. TripLogs';
PRINT '  17. ReservationLogs';
PRINT '  18. Settings';
PRINT '=============================================';
GO
