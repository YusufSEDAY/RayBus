using Microsoft.EntityFrameworkCore;
using RayBus.Models.Entities;

namespace RayBus.Data
{
    public class RayBusDbContext : DbContext
    {
        public RayBusDbContext(DbContextOptions<RayBusDbContext> options) : base(options)
        {
        }

        // DbSet'ler
        public DbSet<Role> Roles { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Terminal> Terminals { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Train> Trains { get; set; }
        public DbSet<Wagon> Wagons { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripSeat> TripSeats { get; set; }
        public DbSet<CancellationReason> CancellationReasons { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get; set; }
        public DbSet<TripLog> TripLogs { get; set; }
        public DbSet<ReservationLog> ReservationLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<AutoCancellationLog> AutoCancellationLogs { get; set; }
        public DbSet<NotificationQueue> NotificationQueues { get; set; }
        public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles", "app");
                entity.HasKey(e => e.RoleID);
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
            });

            // City
            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("Cities", "app");
                entity.HasKey(e => e.CityID);
                entity.Property(e => e.CityName).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.CityName).IsUnique().HasDatabaseName("UX_Cities_CityName");
            });

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", "app");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(300);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(30);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.Status).HasDefaultValue((byte)1);
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Users_Email");
                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Terminal
            modelBuilder.Entity<Terminal>(entity =>
            {
                entity.ToTable("Terminals", "app");
                entity.HasKey(e => e.TerminalID);
                entity.Property(e => e.CityID).IsRequired();
                entity.Property(e => e.TerminalName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Address).HasMaxLength(300).IsRequired(false);
                entity.HasOne(e => e.City)
                      .WithMany(c => c.Terminals)
                      .HasForeignKey(e => e.CityID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.CityID).HasDatabaseName("IX_Terminals_CityID");
            });

            // Station
            modelBuilder.Entity<Station>(entity =>
            {
                entity.ToTable("Stations", "app");
                entity.HasKey(e => e.StationID);
                entity.Property(e => e.CityID).IsRequired();
                entity.Property(e => e.StationName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Address).HasMaxLength(300).IsRequired(false);
                entity.HasOne(e => e.City)
                      .WithMany(c => c.Stations)
                      .HasForeignKey(e => e.CityID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.CityID).HasDatabaseName("IX_Stations_CityID");
            });

            // Vehicle
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("Vehicles", "app", t => t.HasCheckConstraint("CHK_Vehicles_Type", "VehicleType IN ('Bus','Train')"));
                entity.HasKey(e => e.VehicleID);
                entity.Property(e => e.VehicleType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PlateOrCode).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.SeatCount).HasDefaultValue(0).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.CompanyID).IsRequired(false);
                entity.HasIndex(e => e.VehicleType).HasDatabaseName("IX_Vehicles_Type");
                entity.HasIndex(e => e.CompanyID).HasDatabaseName("IX_Vehicles_CompanyID");
                entity.HasOne(e => e.Company)
                      .WithMany(u => u.Vehicles)
                      .HasForeignKey(e => e.CompanyID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Bus
            modelBuilder.Entity<Bus>(entity =>
            {
                entity.ToTable("Buses", "app");
                entity.HasKey(e => e.BusID);
                entity.Property(e => e.BusModel).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.LayoutType).HasMaxLength(20).IsRequired(false);
                entity.HasOne(e => e.Vehicle)
                      .WithOne(v => v.Bus)
                      .HasForeignKey<Bus>(e => e.BusID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Train
            modelBuilder.Entity<Train>(entity =>
            {
                entity.ToTable("Trains", "app");
                entity.HasKey(e => e.TrainID);
                entity.Property(e => e.TrainModel).HasMaxLength(100).IsRequired(false);
                entity.HasOne(e => e.Vehicle)
                      .WithOne(v => v.Train)
                      .HasForeignKey<Train>(e => e.TrainID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Wagon
            modelBuilder.Entity<Wagon>(entity =>
            {
                entity.ToTable("Wagons", "app");
                entity.HasKey(e => e.WagonID);
                entity.Property(e => e.WagonNo).IsRequired();
                entity.Property(e => e.SeatCount).IsRequired();
                entity.HasOne(e => e.Train)
                      .WithMany(t => t.Wagons)
                      .HasForeignKey(e => e.TrainID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.TrainID, e.WagonNo }).IsUnique().HasDatabaseName("UQ_Wagons_Train_WagonNo");
                entity.HasIndex(e => e.TrainID).HasDatabaseName("IX_Wagons_TrainID");
            });

            // Seat
            modelBuilder.Entity<Seat>(entity =>
            {
                entity.ToTable("Seats", "app");
                entity.HasKey(e => e.SeatID);
                entity.Property(e => e.VehicleID).IsRequired();
                entity.Property(e => e.WagonID).IsRequired(false);
                entity.Property(e => e.SeatNo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.SeatPosition).HasMaxLength(20).IsRequired(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true).IsRequired();
                entity.HasOne(e => e.Vehicle)
                      .WithMany(v => v.Seats)
                      .HasForeignKey(e => e.VehicleID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Wagon)
                      .WithMany(w => w.Seats)
                      .HasForeignKey(e => e.WagonID)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.VehicleID, e.SeatNo }).IsUnique().HasDatabaseName("UX_Seats_Vehicle_SeatNo");
                entity.HasIndex(e => e.WagonID).HasDatabaseName("IX_Seats_WagonID");
            });

            // Trip
            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("Trips", "app", t => t.HasCheckConstraint("CHK_Trips_DifferentCities", "FromCityID <> ToCityID"));
                entity.HasKey(e => e.TripID);
                entity.Property(e => e.DepartureDate).HasColumnType("date").IsRequired();
                entity.Property(e => e.DepartureTime).HasColumnType("time(0)").IsRequired();
                entity.Property(e => e.ArrivalDate).HasColumnType("date").IsRequired(false);
                entity.Property(e => e.ArrivalTime).HasColumnType("time(0)").IsRequired(false);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)").HasDefaultValue(0.00m).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue((byte)1).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.HasOne(e => e.Vehicle)
                      .WithMany(v => v.Trips)
                      .HasForeignKey(e => e.VehicleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.FromCity)
                      .WithMany(c => c.DepartureTrips)
                      .HasForeignKey(e => e.FromCityID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ToCity)
                      .WithMany(c => c.ArrivalTrips)
                      .HasForeignKey(e => e.ToCityID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.DepartureTerminal)
                      .WithMany(t => t.DepartureTrips)
                      .HasForeignKey(e => e.DepartureTerminalID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ArrivalTerminal)
                      .WithMany(t => t.ArrivalTrips)
                      .HasForeignKey(e => e.ArrivalTerminalID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.DepartureStation)
                      .WithMany(s => s.DepartureTrips)
                      .HasForeignKey(e => e.DepartureStationID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ArrivalStation)
                      .WithMany(s => s.ArrivalTrips)
                      .HasForeignKey(e => e.ArrivalStationID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.FromCityID, e.ToCityID, e.DepartureDate, e.DepartureTime })
                      .HasDatabaseName("IX_Trips_FromToDate");
                entity.HasIndex(e => new { e.VehicleID, e.DepartureDate })
                      .HasDatabaseName("IX_Trips_VehicleDate");
            });

            // TripSeat
            modelBuilder.Entity<TripSeat>(entity =>
            {
                entity.ToTable("TripSeats", "app");
                entity.HasKey(e => new { e.TripID, e.SeatID });
                entity.HasAlternateKey(e => e.TripSeatID);
                entity.Property(e => e.IsReserved).HasDefaultValue(false);
                entity.Property(e => e.ReservedAt).HasColumnType("datetime2").IsRequired(false);
                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.TripSeats)
                      .HasForeignKey(e => e.TripID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Seat)
                      .WithMany(s => s.TripSeats)
                      .HasForeignKey(e => e.SeatID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.TripID).HasDatabaseName("IX_TripSeats_TripID");
                entity.HasIndex(e => e.IsReserved).HasDatabaseName("IX_TripSeats_IsReserved");
            });

            // CancellationReason
            modelBuilder.Entity<CancellationReason>(entity =>
            {
                entity.ToTable("CancellationReasons", "app");
                entity.HasKey(e => e.ReasonID);
                entity.Property(e => e.ReasonText).IsRequired().HasMaxLength(300);
            });

            // Reservation
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.ToTable("Reservations", "app");
                entity.HasKey(e => e.ReservationID);
                entity.Property(e => e.TripID).IsRequired();
                entity.Property(e => e.SeatID).IsRequired();
                entity.Property(e => e.UserID).IsRequired();
                entity.Property(e => e.ReservationDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Reserved");
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
                entity.Property(e => e.CancelReasonID).IsRequired(false);
                entity.HasOne(e => e.TripSeat)
                      .WithMany(ts => ts.Reservations)
                      .HasForeignKey(e => new { e.TripID, e.SeatID })
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Reservations)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CancelReason)
                      .WithMany(cr => cr.Reservations)
                      .HasForeignKey(e => e.CancelReasonID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.UserID).HasDatabaseName("IX_Reservations_UserID");
                entity.HasIndex(e => e.TripID).HasDatabaseName("IX_Reservations_TripID");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Reservations_Status");
            });

            // Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments", "app");
                entity.HasKey(e => e.PaymentID);
                entity.Property(e => e.Amount).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(e => e.PaymentDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.PaymentMethod).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Completed");
                entity.Property(e => e.TransactionRef).HasMaxLength(200).IsRequired(false);
                entity.HasOne(e => e.Reservation)
                      .WithMany(r => r.Payments)
                      .HasForeignKey(e => e.ReservationID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.ReservationID).HasDatabaseName("IX_Payments_ReservationID");
            });

            // PaymentLog
            modelBuilder.Entity<PaymentLog>(entity =>
            {
                entity.ToTable("PaymentLogs", "log");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.PaymentID).IsRequired();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OldStatus).HasMaxLength(30).IsRequired(false);
                entity.Property(e => e.NewStatus).HasMaxLength(30).IsRequired(false);
                entity.Property(e => e.LogDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.Description).HasMaxLength(300).IsRequired(false);
                entity.HasOne(e => e.Payment)
                      .WithMany(p => p.PaymentLogs)
                      .HasForeignKey(e => e.PaymentID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.PaymentID).HasDatabaseName("IX_PaymentLogs_PaymentID");
            });

            // TripLog
            modelBuilder.Entity<TripLog>(entity =>
            {
                entity.ToTable("TripLogs", "log");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.TripID).IsRequired();
                entity.Property(e => e.ColumnName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(e => e.ChangedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.ChangedByUserID).IsRequired(false);
                entity.Property(e => e.Action).HasMaxLength(50).IsRequired(false);
                entity.Property(e => e.LogDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired(false);
                entity.Property(e => e.Description).HasMaxLength(500).IsRequired(false);
                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.TripLogs)
                      .HasForeignKey(e => e.TripID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ChangedByUser)
                      .WithMany(u => u.TripLogs)
                      .HasForeignKey(e => e.ChangedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.TripID).HasDatabaseName("IX_TripLogs_TripID");
            });

            // ReservationLog
            modelBuilder.Entity<ReservationLog>(entity =>
            {
                entity.ToTable("ReservationLogs", "log");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.ReservationID).IsRequired();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Details).HasMaxLength(400).IsRequired(false);
                entity.Property(e => e.LogDate).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.PerformedBy).IsRequired(false);
                entity.HasOne(e => e.Reservation)
                      .WithMany(r => r.ReservationLogs)
                      .HasForeignKey(e => e.ReservationID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.PerformedByUser)
                      .WithMany(u => u.ReservationLogs)
                      .HasForeignKey(e => e.PerformedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ReservationID).HasDatabaseName("IX_ReservationLogs_ReservationID");
            });

            // Setting
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("Settings", "app");
                entity.HasKey(e => e.SettingKey);
                entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SettingValue).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            });

            // AutoCancellationLog
            modelBuilder.Entity<AutoCancellationLog>(entity =>
            {
                entity.ToTable("AutoCancellationLog", "log");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.ReservationID).IsRequired();
                entity.Property(e => e.UserID).IsRequired();
                entity.Property(e => e.CancelledAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OriginalReservationDate).IsRequired();
                entity.Property(e => e.TimeoutMinutes).HasDefaultValue(15).IsRequired();
                entity.HasOne(e => e.Reservation)
                      .WithMany()
                      .HasForeignKey(e => e.ReservationID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.ReservationID).HasDatabaseName("IX_AutoCancellationLog_ReservationID");
                entity.HasIndex(e => e.CancelledAt).HasDatabaseName("IX_AutoCancellationLog_CancelledAt");
            });

            // NotificationQueue
            modelBuilder.Entity<NotificationQueue>(entity =>
            {
                entity.ToTable("NotificationQueue", "log");
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.UserID).IsRequired();
                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NotificationMethod).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Subject).HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.Property(e => e.RetryCount).HasDefaultValue(0).IsRequired();
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.RelatedReservation)
                      .WithMany()
                      .HasForeignKey(e => e.RelatedReservationID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_NotificationQueue_Status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_NotificationQueue_CreatedAt");
                entity.HasIndex(e => e.UserID).HasDatabaseName("IX_NotificationQueue_UserID");
            });

            // UserNotificationPreferences
            modelBuilder.Entity<UserNotificationPreferences>(entity =>
            {
                entity.ToTable("UserNotificationPreferences", "log");
                entity.HasKey(e => e.PreferenceID);
                entity.Property(e => e.UserID).IsRequired();
                entity.Property(e => e.EmailNotifications).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.SMSNotifications).HasDefaultValue(false).IsRequired();
                entity.Property(e => e.ReservationNotifications).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.PaymentNotifications).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.CancellationNotifications).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.ReminderNotifications).HasDefaultValue(true).IsRequired();
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.UserID).IsUnique().HasDatabaseName("IX_UserNotificationPreferences_UserID");
            });
        }
    }
}
