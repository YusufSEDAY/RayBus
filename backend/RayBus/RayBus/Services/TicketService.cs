using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RayBus.Data;
using RayBus.Models.DTOs;

namespace RayBus.Services
{
    public class TicketService : ITicketService
    {
        private readonly RayBusDbContext _context;
        private readonly ILogger<TicketService> _logger;
        private readonly string _connectionString;

        public TicketService(
            RayBusDbContext context,
            ILogger<TicketService> logger)
        {
            _context = context;
            _logger = logger;
            _connectionString = context.Database.GetConnectionString() 
                ?? throw new InvalidOperationException("Connection string not found");
        }

        public async Task<ApiResponse<TicketDetailDTO>> GetTicketDetailsAsync(int reservationId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Bilet_Bilgileri", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@ReservationID", reservationId);
                command.Parameters.AddWithValue("@TicketNumber", DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var ticket = MapToTicketDetailDTO(reader);
                    return ApiResponse<TicketDetailDTO>.SuccessResponse(
                        ticket,
                        "Bilet bilgileri başarıyla getirildi"
                    );
                }

                return ApiResponse<TicketDetailDTO>.ErrorResponse("Bilet bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bilet bilgileri getirilirken hata. ReservationID: {ReservationID}", reservationId);
                return ApiResponse<TicketDetailDTO>.ErrorResponse(
                    "Bilet bilgileri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<TicketDetailDTO>> GetTicketDetailsByTicketNumberAsync(string ticketNumber)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("[proc].sp_Bilet_Bilgileri", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@ReservationID", DBNull.Value);
                command.Parameters.AddWithValue("@TicketNumber", ticketNumber);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var ticket = MapToTicketDetailDTO(reader);
                    return ApiResponse<TicketDetailDTO>.SuccessResponse(
                        ticket,
                        "Bilet bilgileri başarıyla getirildi"
                    );
                }

                return ApiResponse<TicketDetailDTO>.ErrorResponse("Bilet bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Bilet bilgileri getirilirken hata. TicketNumber: {TicketNumber}", ticketNumber);
                return ApiResponse<TicketDetailDTO>.ErrorResponse(
                    "Bilet bilgileri getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<byte[]>> GenerateTicketPDFAsync(int reservationId)
        {
            try
            {
                // Önce bilet bilgilerini al
                var ticketResponse = await GetTicketDetailsAsync(reservationId);
                if (!ticketResponse.Success || ticketResponse.Data == null)
                {
                    return ApiResponse<byte[]>.ErrorResponse("Bilet bilgileri alınamadı");
                }

                var ticket = ticketResponse.Data;

                // PDF oluşturma (şimdilik basit bir HTML/PDF dönüşümü yapılabilir)
                // İleride QuestPDF veya iTextSharp kullanılabilir
                var pdfContent = GenerateSimplePDF(ticket);

                return ApiResponse<byte[]>.SuccessResponse(
                    pdfContent,
                    "PDF başarıyla oluşturuldu"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ PDF oluşturulurken hata. ReservationID: {ReservationID}", reservationId);
                return ApiResponse<byte[]>.ErrorResponse(
                    "PDF oluşturulurken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        private TimeSpan? GetTimeSpanSafe(SqlDataReader reader, string columnName, bool nullable = false)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                {
                    return nullable ? (TimeSpan?)null : TimeSpan.Zero;
                }

                // Önce TimeSpan olarak okumayı dene
                try
                {
                    return reader.GetTimeSpan(ordinal);
                }
                catch
                {
                    // TimeSpan olarak okunamazsa string olarak oku ve parse et
                    var timeString = reader.GetString(ordinal);
                    if (TimeSpan.TryParse(timeString, out var timeSpan))
                    {
                        return timeSpan;
                    }
                    return nullable ? (TimeSpan?)null : TimeSpan.Zero;
                }
            }
            catch
            {
                return nullable ? (TimeSpan?)null : TimeSpan.Zero;
            }
        }

        private TicketDetailDTO MapToTicketDetailDTO(SqlDataReader reader)
        {
            return new TicketDetailDTO
            {
                ReservationID = reader.GetInt32(reader.GetOrdinal("ReservationID")),
                BiletNumarasi = reader.IsDBNull(reader.GetOrdinal("BiletNumarasi"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("BiletNumarasi")),
                RezervasyonTarihi = reader.GetDateTime(reader.GetOrdinal("RezervasyonTarihi")),
                RezervasyonDurumu = reader.IsDBNull(reader.GetOrdinal("RezervasyonDurumu"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("RezervasyonDurumu")),
                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("KullaniciAdi")),
                KullaniciEmail = reader.IsDBNull(reader.GetOrdinal("KullaniciEmail"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("KullaniciEmail")),
                KullaniciTelefon = reader.IsDBNull(reader.GetOrdinal("KullaniciTelefon"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("KullaniciTelefon")),
                TripID = reader.GetInt32(reader.GetOrdinal("TripID")),
                KalkisTarihi = reader.GetDateTime(reader.GetOrdinal("KalkisTarihi")),
                KalkisSaati = GetTimeSpanSafe(reader, "KalkisSaati") ?? TimeSpan.Zero,
                VarisTarihi = reader.IsDBNull(reader.GetOrdinal("VarisTarihi"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("VarisTarihi")),
                VarisSaati = GetTimeSpanSafe(reader, "VarisSaati", nullable: true),
                SeferFiyati = reader.IsDBNull(reader.GetOrdinal("SeferFiyati"))
                    ? 0
                    : reader.GetDecimal(reader.GetOrdinal("SeferFiyati")),
                KalkisSehri = reader.IsDBNull(reader.GetOrdinal("KalkisSehri"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("KalkisSehri")),
                VarisSehri = reader.IsDBNull(reader.GetOrdinal("VarisSehri"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("VarisSehri")),
                KalkisTerminali = reader.IsDBNull(reader.GetOrdinal("KalkisTerminali"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("KalkisTerminali")),
                VarisTerminali = reader.IsDBNull(reader.GetOrdinal("VarisTerminali"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("VarisTerminali")),
                KalkisIstasyonu = reader.IsDBNull(reader.GetOrdinal("KalkisIstasyonu"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("KalkisIstasyonu")),
                VarisIstasyonu = reader.IsDBNull(reader.GetOrdinal("VarisIstasyonu"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("VarisIstasyonu")),
                VehicleID = reader.GetInt32(reader.GetOrdinal("VehicleID")),
                AracPlakasi = reader.IsDBNull(reader.GetOrdinal("AracPlakasi"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("AracPlakasi")),
                AracTipi = reader.IsDBNull(reader.GetOrdinal("AracTipi"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("AracTipi")),
                AracTipiTurkce = reader.IsDBNull(reader.GetOrdinal("AracTipiTurkce"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("AracTipiTurkce")),
                SeatID = reader.GetInt32(reader.GetOrdinal("SeatID")),
                KoltukNumarasi = reader.IsDBNull(reader.GetOrdinal("KoltukNumarasi"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("KoltukNumarasi")),
                KoltukDurumu = reader.IsDBNull(reader.GetOrdinal("KoltukDurumu"))
                    ? false
                    : reader.GetBoolean(reader.GetOrdinal("KoltukDurumu")),
                PaymentID = reader.IsDBNull(reader.GetOrdinal("PaymentID"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("PaymentID")),
                OdenenTutar = reader.IsDBNull(reader.GetOrdinal("OdenenTutar"))
                    ? 0
                    : reader.GetDecimal(reader.GetOrdinal("OdenenTutar")),
                OdemeTarihi = reader.IsDBNull(reader.GetOrdinal("OdemeTarihi"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("OdemeTarihi")),
                OdemeYontemi = reader.IsDBNull(reader.GetOrdinal("OdemeYontemi"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OdemeYontemi")),
                OdemeDurumu = reader.IsDBNull(reader.GetOrdinal("OdemeDurumu"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OdemeDurumu")),
                VagonNumarasi = reader.IsDBNull(reader.GetOrdinal("VagonNumarasi"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("VagonNumarasi")),
                OtobusModeli = reader.IsDBNull(reader.GetOrdinal("OtobusModeli"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OtobusModeli")),
                KoltukDuzeni = reader.IsDBNull(reader.GetOrdinal("KoltukDuzeni"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("KoltukDuzeni")),
                TrenModeli = reader.IsDBNull(reader.GetOrdinal("TrenModeli"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("TrenModeli")),
                SeyahatSuresiSaat = reader.IsDBNull(reader.GetOrdinal("SeyahatSuresiSaat"))
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("SeyahatSuresiSaat"))
            };
        }

        private byte[] GenerateSimplePDF(TicketDetailDTO ticket)
        {
            // TimeSpan formatlaması için güvenli metod
            string FormatTimeSpan(TimeSpan? timeSpan)
            {
                if (timeSpan == null || timeSpan == TimeSpan.Zero)
                    return "00:00";
                try
                {
                    return timeSpan.Value.ToString(@"hh\:mm");
                }
                catch
                {
                    return $"{timeSpan.Value.Hours:D2}:{timeSpan.Value.Minutes:D2}";
                }
            }

            // TimeSpan değerlerini önceden formatla
            var kalkisSaatiStr = FormatTimeSpan(ticket.KalkisSaati);
            var varisSaatiStr = FormatTimeSpan(ticket.VarisSaati);
            
            // Bilet numarası yoksa oluştur
            var biletNumarasi = ticket.BiletNumarasi;
            if (string.IsNullOrWhiteSpace(biletNumarasi))
            {
                // ReservationID'den bilet numarası oluştur
                biletNumarasi = $"RB-{ticket.ReservationID:D8}";
            }
            
            // Ödenen tutar yoksa sefer fiyatını kullan
            var odenenTutar = ticket.OdenenTutar > 0 ? ticket.OdenenTutar : ticket.SeferFiyati;
            
            // Site renkleri (indigo, pink, amber, green) - QuestPDF önceden tanımlı renkler ve yakın tonlar
            var primaryColor = Colors.Indigo.Medium; // Indigo
            var primaryDark = Colors.Indigo.Darken2;
            var primaryLight = Colors.Indigo.Lighten2;
            var secondaryColor = Colors.Pink.Medium; // Pink
            var accentColor = Colors.Orange.Medium; // Amber yerine Orange
            var successColor = Colors.Green.Medium; // Green
            var darkBg = Colors.Grey.Darken4;
            var darkSurface = Colors.Grey.Darken3;
            var lightBg = Colors.Grey.Lighten4;

            // QuestPDF ile gerçek PDF oluştur
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Logo dosyası yolu (varsa) - birden fazla yerde arama yap
            var currentDir = Directory.GetCurrentDirectory();
            var logoPaths = new[]
            {
                Path.Combine(currentDir, "wwwroot", "logo.png"), // Backend wwwroot klasörü
                Path.Combine(currentDir, "..", "..", "..", "frontend", "public", "logo.png"), // Frontend public klasörü (geliştirme)
                Path.Combine(currentDir, "..", "..", "frontend", "public", "logo.png"), // Alternatif yol
                Path.Combine(currentDir, "logo.png") // Mevcut dizin
            };
            
            string? logoPath = null;
            foreach (var path in logoPaths)
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    logoPath = normalizedPath;
                    break;
                }
            }
            var hasLogo = !string.IsNullOrEmpty(logoPath);

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(lightBg);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Grey.Darken3));

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(16);

                            // Soft Header - Site renklerine uygun
                            column.Item().Background(primaryColor).Padding(30).Column(headerColumn =>
                            {
                                headerColumn.Spacing(14);
                                
                                // Logo veya Text Logo
                                if (hasLogo && !string.IsNullOrEmpty(logoPath))
                                {
                                    try
                                    {
                                        headerColumn.Item().AlignCenter().Height(55, Unit.Point).Image(logoPath).FitArea();
                                    }
                                    catch
                                    {
                                        headerColumn.Item().AlignCenter().Text("RAYBUS").FontSize(36).Bold().FontColor(Colors.White).LetterSpacing(3);
                                    }
                                }
                                else
                                {
                                    headerColumn.Item().AlignCenter().Text("RAYBUS").FontSize(36).Bold().FontColor(Colors.White).LetterSpacing(3);
                                }
                                
                                // Bilet Numarası - Soft ve Modern
                                headerColumn.Item().AlignCenter().PaddingTop(8)
                                    .Background(primaryDark).PaddingVertical(10).PaddingHorizontal(20)
                                    .Text($"Bilet No: {biletNumarasi}")
                                    .FontSize(13).Bold().FontColor(Colors.White);
                            });

                            column.Item().PaddingVertical(10);

                            // Ana Bilgiler - Soft ve Modern
                            column.Item().Row(row =>
                            {
                                // Sol - Yolcu Bilgileri (Soft Pink)
                                row.RelativeItem(1).Padding(22).Background(Colors.White).Border(1).BorderColor(secondaryColor).Column(leftColumn =>
                                {
                                    leftColumn.Spacing(12);
                                    leftColumn.Item().Text("YOLCU BİLGİLERİ").FontSize(11).Bold().FontColor(secondaryColor).LetterSpacing(0.5f);
                                    leftColumn.Item().PaddingBottom(10).LineHorizontal(1.5f).LineColor(secondaryColor);
                                    
                                    leftColumn.Item().Text("Ad Soyad").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    leftColumn.Item().PaddingTop(4).Text(ticket.KullaniciAdi).FontSize(13).Bold().FontColor(Colors.Black);
                                    
                                    if (!string.IsNullOrEmpty(ticket.KullaniciEmail))
                                    {
                                        leftColumn.Item().PaddingTop(12);
                                        leftColumn.Item().Text("E-posta").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        leftColumn.Item().PaddingTop(4).Text(ticket.KullaniciEmail).FontSize(11);
                                    }
                                    
                                    if (!string.IsNullOrEmpty(ticket.KullaniciTelefon))
                                    {
                                        leftColumn.Item().PaddingTop(12);
                                        leftColumn.Item().Text("Telefon").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        leftColumn.Item().PaddingTop(4).Text(ticket.KullaniciTelefon).FontSize(11);
                                    }
                                });

                                row.ConstantItem(14);

                                // Sağ - Ödeme (Soft Green)
                                row.RelativeItem(1).Padding(22).Background(Colors.White).Border(1).BorderColor(successColor).Column(rightColumn =>
                                {
                                    rightColumn.Spacing(12);
                                    rightColumn.Item().Text("ÖDEME BİLGİLERİ").FontSize(11).Bold().FontColor(successColor).LetterSpacing(0.5f);
                                    rightColumn.Item().PaddingBottom(10).LineHorizontal(1.5f).LineColor(successColor);
                                    
                                    rightColumn.Item().Text("Ödenen Tutar").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    rightColumn.Item().PaddingTop(4).Text($"{odenenTutar:N2} ₺").FontSize(22).Bold().FontColor(successColor);
                                    
                                    if (ticket.OdemeTarihi.HasValue)
                                    {
                                        rightColumn.Item().PaddingTop(12);
                                        rightColumn.Item().Text("Ödeme Tarihi").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        rightColumn.Item().PaddingTop(4).Text(ticket.OdemeTarihi.Value.ToString("dd.MM.yyyy HH:mm")).FontSize(11);
                                    }
                                    
                                    if (!string.IsNullOrEmpty(ticket.OdemeYontemi))
                                    {
                                        rightColumn.Item().PaddingTop(12);
                                        rightColumn.Item().Text("Ödeme Yöntemi").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        rightColumn.Item().PaddingTop(4).Text(ticket.OdemeYontemi).FontSize(11);
                                    }
                                });
                            });

                            column.Item().PaddingVertical(10);

                            // Sefer Bilgileri - Soft ve Modern (Primary Indigo)
                            column.Item().Padding(24).Background(Colors.White).Border(1.5f).BorderColor(primaryColor).Column(tripColumn =>
                            {
                                tripColumn.Spacing(14);
                                tripColumn.Item().Text("SEFER BİLGİLERİ").FontSize(12).Bold().FontColor(primaryColor).LetterSpacing(0.5f);
                                tripColumn.Item().PaddingBottom(12).LineHorizontal(2).LineColor(primaryColor);
                                
                                // Güzergah - Soft ve Vurgulu
                                tripColumn.Item().PaddingVertical(10).Row(routeRow =>
                                {
                                    routeRow.RelativeItem(1).AlignCenter().Column(kalkisCol =>
                                    {
                                        kalkisCol.Item().Text(ticket.KalkisSehri).FontSize(20).Bold().FontColor(primaryDark);
                                        kalkisCol.Item().PaddingTop(4).Text("Kalkış").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    });
                                    routeRow.ConstantItem(45).AlignCenter().Text("→").FontSize(28).Bold().FontColor(primaryColor);
                                    routeRow.RelativeItem(1).AlignCenter().Column(varisCol =>
                                    {
                                        varisCol.Item().Text(ticket.VarisSehri).FontSize(20).Bold().FontColor(primaryDark);
                                        varisCol.Item().PaddingTop(4).Text("Varış").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    });
                                });

                                tripColumn.Item().PaddingVertical(14);

                                // Tarih ve Saat - Soft Grid
                                tripColumn.Item().Row(dateRow =>
                                {
                                    dateRow.RelativeItem(1).Padding(14).Background(primaryLight).Column(dateCol =>
                                    {
                                        dateCol.Item().Text("KALKIŞ").FontSize(10).Bold().FontColor(primaryDark);
                                        dateCol.Item().PaddingTop(6);
                                        dateCol.Item().Text(ticket.KalkisTarihi.ToString("dd.MM.yyyy")).FontSize(14).Bold();
                                        dateCol.Item().PaddingTop(4).Text(kalkisSaatiStr).FontSize(18).Bold().FontColor(primaryDark);
                                    });
                                    
                                    if (ticket.VarisTarihi.HasValue || ticket.VarisSaati.HasValue)
                                    {
                                        dateRow.ConstantItem(12);
                                        dateRow.RelativeItem(1).Padding(14).Background(successColor).Column(arrivalCol =>
                                        {
                                            arrivalCol.Item().Text("VARIŞ").FontSize(10).Bold().FontColor(Colors.White);
                                            arrivalCol.Item().PaddingTop(6);
                                            if (ticket.VarisTarihi.HasValue)
                                                arrivalCol.Item().Text(ticket.VarisTarihi.Value.ToString("dd.MM.yyyy")).FontSize(14).Bold().FontColor(Colors.White);
                                            if (ticket.VarisSaati.HasValue)
                                                arrivalCol.Item().PaddingTop(4).Text(varisSaatiStr).FontSize(18).Bold().FontColor(Colors.White);
                                        });
                                    }
                                });

                                // Terminal/İstasyon bilgileri - Soft
                                if (!string.IsNullOrEmpty(ticket.KalkisTerminali) || !string.IsNullOrEmpty(ticket.KalkisIstasyonu) || 
                                    !string.IsNullOrEmpty(ticket.VarisTerminali) || !string.IsNullOrEmpty(ticket.VarisIstasyonu))
                                {
                                    tripColumn.Item().PaddingTop(14).PaddingBottom(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                                    
                                    if (!string.IsNullOrEmpty(ticket.KalkisTerminali))
                                        tripColumn.Item().PaddingTop(6).Text($"Terminal: {ticket.KalkisTerminali}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrEmpty(ticket.KalkisIstasyonu))
                                        tripColumn.Item().PaddingTop(4).Text($"İstasyon: {ticket.KalkisIstasyonu}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrEmpty(ticket.VarisTerminali))
                                        tripColumn.Item().PaddingTop(4).Text($"Varış Terminali: {ticket.VarisTerminali}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    if (!string.IsNullOrEmpty(ticket.VarisIstasyonu))
                                        tripColumn.Item().PaddingTop(4).Text($"Varış İstasyonu: {ticket.VarisIstasyonu}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                }
                            });

                            column.Item().PaddingVertical(10);

                            // Araç ve Koltuk Bilgileri - Soft ve Modern
                            column.Item().Row(vehicleRow =>
                            {
                                vehicleRow.RelativeItem(1).Padding(22).Background(Colors.White).Border(1).BorderColor(accentColor).Column(vCol =>
                                {
                                    vCol.Spacing(12);
                                    vCol.Item().Text("ARAÇ BİLGİLERİ").FontSize(11).Bold().FontColor(accentColor).LetterSpacing(0.5f);
                                    vCol.Item().PaddingBottom(10).LineHorizontal(1.5f).LineColor(accentColor);
                                    
                                    vCol.Item().Text("Araç Tipi").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    vCol.Item().PaddingTop(4).Text(ticket.AracTipiTurkce ?? ticket.AracTipi).FontSize(13).Bold();
                                    
                                    vCol.Item().PaddingTop(12);
                                    vCol.Item().Text("Plaka/Kod").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    vCol.Item().PaddingTop(4).Text(ticket.AracPlakasi).FontSize(12).Bold();
                                });
                                
                                vehicleRow.ConstantItem(14);
                                
                                vehicleRow.RelativeItem(1).Padding(22).Background(Colors.White).Border(1).BorderColor(primaryColor).Column(seatCol =>
                                {
                                    seatCol.Spacing(12);
                                    seatCol.Item().Text("KOLTUK BİLGİLERİ").FontSize(11).Bold().FontColor(primaryColor).LetterSpacing(0.5f);
                                    seatCol.Item().PaddingBottom(10).LineHorizontal(1.5f).LineColor(primaryColor);
                                    
                                    seatCol.Item().Text("Koltuk No").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    seatCol.Item().PaddingTop(4).Text(ticket.KoltukNumarasi).FontSize(24).Bold().FontColor(primaryColor);
                                    
                                    if (ticket.VagonNumarasi.HasValue)
                                    {
                                        seatCol.Item().PaddingTop(12);
                                        seatCol.Item().Text("Vagon No").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        seatCol.Item().PaddingTop(4).Text(ticket.VagonNumarasi.Value.ToString()).FontSize(13).Bold();
                                    }
                                });
                            });

                            column.Item().PaddingVertical(14);

                            // Footer - Soft ve Modern
                            column.Item().Background(Colors.White).Padding(20).Border(1).BorderColor(Colors.Grey.Lighten2).AlignCenter().Column(footerColumn =>
                            {
                                footerColumn.Item().Text("✓ Bu bilet geçerlidir").FontSize(12).Bold().FontColor(successColor);
                                footerColumn.Item().PaddingTop(6).Text("İyi yolculuklar dileriz!").FontSize(11).FontColor(Colors.Grey.Darken2);
                                footerColumn.Item().PaddingTop(10).Text($"Bilet No: {biletNumarasi}").FontSize(10).Bold().FontColor(Colors.Grey.Darken1);
                            });
                        });
                });
            })
            .GeneratePdf();

            return pdfBytes;
        }
    }
}

