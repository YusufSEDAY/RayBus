using RayBus.Models.DTOs;
using RayBus.Models.Entities;
using RayBus.Repositories;
using RayBus.Data;
using Microsoft.EntityFrameworkCore;
using static RayBus.Repositories.IReservationRepository;

namespace RayBus.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly RayBusDbContext _context;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            RayBusDbContext context,
            ILogger<ReservationService> logger)
        {
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<ReservationDTO>>> GetAllReservationsAsync()
        {
            try
            {
                var reservations = await _reservationRepository.GetAllAsync();
                var reservationDtos = reservations.Select(MapToDTO).ToList();

                return ApiResponse<IEnumerable<ReservationDTO>>.SuccessResponse(
                    reservationDtos,
                    "Rezervasyonlar başarıyla getirildi"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyonlar getirilirken hata oluştu");
                return ApiResponse<IEnumerable<ReservationDTO>>.ErrorResponse(
                    "Rezervasyonlar getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<IEnumerable<ReservationDTO>>> GetReservationsByUserIdAsync(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return ApiResponse<IEnumerable<ReservationDTO>>.ErrorResponse("Geçersiz kullanıcı ID'si");
                }

                // Stored procedure kullanarak kullanıcı biletlerini getir (daha optimize)
                // Artık TripFiyati stored procedure'den geliyor, ekstra sorgu gereksiz
                var userTickets = await _reservationRepository.GetUserTicketsUsingStoredProcedureAsync(userId);
                
                // Mapping yaparken OdenenTutar 0 ise TripFiyati'ni kullan
                var reservationDtos = userTickets.Select(ticket => 
                {
                    var dto = MapFromUserTicket(ticket);
                    // Eğer ödenen tutar 0 ise ve ödeme bekleniyorsa, TripFiyati'ni kullan
                    if (dto.Price == 0 && ticket.PaymentStatus == "Pending" && ticket.TripFiyati > 0)
                    {
                        dto.Price = ticket.TripFiyati;
                    }
                    return dto;
                }).ToList();

                return ApiResponse<IEnumerable<ReservationDTO>>.SuccessResponse(
                    reservationDtos,
                    $"{reservationDtos.Count} rezervasyon bulundu"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı rezervasyonları getirilirken hata oluştu. UserId: {UserId}", userId);
                return ApiResponse<IEnumerable<ReservationDTO>>.ErrorResponse(
                    "Rezervasyonlar getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ReservationDTO>> GetReservationByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Geçersiz rezervasyon ID'si");
                }

                var reservation = await _reservationRepository.GetByIdAsync(id);

                if (reservation == null)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Rezervasyon bulunamadı");
                }

                var reservationDto = MapToDTO(reservation);
                return ApiResponse<ReservationDTO>.SuccessResponse(reservationDto, "Rezervasyon başarıyla getirildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon getirilirken hata oluştu. ID: {Id}", id);
                return ApiResponse<ReservationDTO>.ErrorResponse(
                    "Rezervasyon getirilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<ReservationDTO>> CreateReservationAsync(CreateReservationDTO createDto)
        {
            try
            {
                if (createDto.UserID <= 0)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Geçersiz kullanıcı ID'si");
                }

                // Kullanıcı rolünü kontrol et - Admin ve Şirket kullanıcıları bilet alamaz
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == createDto.UserID);

                if (user == null)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Kullanıcı bulunamadı");
                }

                if (user.Role != null)
                {
                    var roleName = user.Role.RoleName;
                    if (roleName == "Admin" || roleName == "Şirket")
                    {
                        return ApiResponse<ReservationDTO>.ErrorResponse(
                            $"Bu işlem için yetkiniz yok. {roleName} kullanıcıları bilet alamaz."
                        );
                    }
                }

                if (createDto.TripID <= 0)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Geçersiz sefer ID'si");
                }

                if (createDto.SeatID <= 0)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Geçersiz koltuk ID'si");
                }

                if (createDto.Price <= 0)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Geçersiz fiyat");
                }

                // Sefer kontrolü - fiyat doğrulama için
                var trip = await _tripRepository.GetByIdAsync(createDto.TripID);
                if (trip == null || trip.Status != 1)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Sefer bulunamadı veya aktif değil");
                }

                // Stored procedure kullanarak rezervasyon yap (transaction güvenli, race condition kontrolü var)
                var (success, reservationId, errorMessage, paymentStatus) = await _reservationRepository
                    .CreateReservationUsingStoredProcedureAsync(
                        createDto.TripID,
                        createDto.SeatID,
                        createDto.UserID,
                        createDto.Price,
                        createDto.PaymentMethod ?? "Kredi Kartı",
                        createDto.IslemTipi
                    );

                if (!success)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse(errorMessage);
                }

                // Rezervasyonu tekrar yükle (tüm ilişkilerle birlikte)
                var reservation = await _reservationRepository.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    return ApiResponse<ReservationDTO>.ErrorResponse("Rezervasyon oluşturuldu ancak yüklenemedi");
                }

                // Rezervasyon oluşturuldu log kaydı ekle
                var islemTipiText = createDto.IslemTipi == 1 ? "Satın Alma" : "Rezervasyon";
                await _reservationRepository.AddReservationLogAsync(
                    reservationId, 
                    "Created", 
                    $"{islemTipiText} oluşturuldu. Sefer: {createDto.TripID}, Koltuk: {createDto.SeatID}, Fiyat: {createDto.Price}₺, Ödeme Durumu: {paymentStatus}",
                    createDto.UserID
                );

                // PaymentLogs'a kart bilgilerini kaydet (eğer varsa ve satın alma ise)
                if (createDto.CardInfo != null && createDto.IslemTipi == 1)
                {
                    // Ödeme kaydını bul (stored procedure ile oluşturulmuş olmalı)
                    var payment = await _context.Payments
                        .Where(p => p.ReservationID == reservationId)
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefaultAsync();

                    if (payment != null)
                    {
                        // PaymentLogs'a kart bilgilerini ekle
                        var cardInfoDescription = $"Kart: {createDto.CardInfo.MaskedCardNumber}, " +
                            $"Sahibi: {createDto.CardInfo.CardHolder}, " +
                            $"Son Kullanma: {createDto.CardInfo.ExpiryMonth}/{createDto.CardInfo.ExpiryYear}";

                        var paymentLog = new Models.Entities.PaymentLog
                        {
                            PaymentID = payment.PaymentID,
                            Action = "KartBilgileri",
                            NewStatus = payment.Status,
                            Description = cardInfoDescription,
                            LogDate = DateTime.UtcNow
                        };

                        _context.PaymentLogs.Add(paymentLog);
                        await _context.SaveChangesAsync();
                    }
                }

                var reservationDto = MapToDTO(reservation);
                var successMessage = createDto.IslemTipi == 1 
                    ? "Rezervasyon başarıyla oluşturuldu ve ödeme tamamlandı" 
                    : "Rezervasyon başarıyla oluşturuldu. Ödeme bekleniyor.";

                return ApiResponse<ReservationDTO>.SuccessResponse(
                    reservationDto,
                    successMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon oluşturulurken hata oluştu");
                return ApiResponse<ReservationDTO>.ErrorResponse(
                    "Rezervasyon oluşturulurken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> CompletePaymentAsync(CompletePaymentDTO completePaymentDto)
        {
            try
            {
                if (completePaymentDto.ReservationID <= 0)
                {
                    return ApiResponse<bool>.ErrorResponse("Geçersiz rezervasyon ID'si");
                }

                if (completePaymentDto.Price <= 0)
                {
                    return ApiResponse<bool>.ErrorResponse("Geçersiz fiyat");
                }

                // Rezervasyonu kontrol et
                var reservation = await _reservationRepository.GetByIdAsync(completePaymentDto.ReservationID);
                if (reservation == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Rezervasyon bulunamadı");
                }

                // Rezervasyon sahibinin rolünü kontrol et - Admin ve Şirket kullanıcıları ödeme yapamaz
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == reservation.UserID);

                if (user?.Role != null)
                {
                    var roleName = user.Role.RoleName;
                    if (roleName == "Admin" || roleName == "Şirket")
                    {
                        return ApiResponse<bool>.ErrorResponse(
                            $"Bu işlem için yetkiniz yok. {roleName} kullanıcıları ödeme yapamaz."
                        );
                    }
                }

                // Zaten ödenmiş mi?
                if (reservation.PaymentStatus == "Paid")
                {
                    return ApiResponse<bool>.ErrorResponse("Bu rezervasyonun ödemesi zaten yapılmış");
                }

                // İptal edilmiş mi?
                if (reservation.Status == "Cancelled")
                {
                    return ApiResponse<bool>.ErrorResponse("İptal edilmiş bir rezervasyon için ödeme yapılamaz");
                }

                // Stored procedure kullanarak ödeme tamamla
                var (success, errorMessage) = await _reservationRepository
                    .CompletePaymentUsingStoredProcedureAsync(
                        completePaymentDto.ReservationID,
                        completePaymentDto.Price,
                        completePaymentDto.PaymentMethod ?? "Kredi Kartı"
                    );

                if (!success)
                {
                    return ApiResponse<bool>.ErrorResponse(errorMessage);
                }

                // Ödeme tamamlandı log kaydı ekle
                await _reservationRepository.AddReservationLogAsync(
                    completePaymentDto.ReservationID,
                    "PaymentCompleted",
                    $"Ödeme tamamlandı. Fiyat: {completePaymentDto.Price}₺, Yöntem: {completePaymentDto.PaymentMethod}",
                    reservation.UserID
                );

                // PaymentLogs'a kart bilgilerini kaydet (eğer varsa)
                if (completePaymentDto.CardInfo != null)
                {
                    // Ödeme kaydını bul (stored procedure ile oluşturulmuş olmalı)
                    var payment = await _context.Payments
                        .Where(p => p.ReservationID == completePaymentDto.ReservationID)
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefaultAsync();

                    if (payment != null)
                    {
                        // PaymentLogs'a kart bilgilerini ekle
                        var cardInfoDescription = $"Kart: {completePaymentDto.CardInfo.MaskedCardNumber}, " +
                            $"Sahibi: {completePaymentDto.CardInfo.CardHolder}, " +
                            $"Son Kullanma: {completePaymentDto.CardInfo.ExpiryMonth}/{completePaymentDto.CardInfo.ExpiryYear}";

                        var paymentLog = new Models.Entities.PaymentLog
                        {
                            PaymentID = payment.PaymentID,
                            Action = "KartBilgileri",
                            NewStatus = payment.Status,
                            Description = cardInfoDescription,
                            LogDate = DateTime.UtcNow
                        };

                        _context.PaymentLogs.Add(paymentLog);
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Ödeme tamamlandı. ReservationID: {ReservationID}, Price: {Price}", 
                    completePaymentDto.ReservationID, completePaymentDto.Price);

                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Ödeme başarıyla tamamlandı"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme tamamlanırken hata oluştu");
                return ApiResponse<bool>.ErrorResponse(
                    "Ödeme tamamlanırken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<ApiResponse<bool>> CancelReservationAsync(int id, CancelReservationDTO? cancelDto = null)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<bool>.ErrorResponse("Geçersiz rezervasyon ID'si");
                }

                // Rezervasyonu getir ve kontrol et
                var reservation = await _reservationRepository.GetByIdAsync(id);
                if (reservation == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Rezervasyon bulunamadı");
                }

                // Zaten iptal edilmiş mi?
                if (reservation.Status == "Cancelled")
                {
                    return ApiResponse<bool>.ErrorResponse("Bu rezervasyon zaten iptal edilmiş");
                }

                // Sefer başlamış mı kontrol et
                var trip = reservation.TripSeat?.Trip;
                if (trip != null)
                {
                    var departureDateTime = trip.DepartureDate.Date.Add(trip.DepartureTime);
                    if (departureDateTime <= DateTime.Now)
                    {
                        return ApiResponse<bool>.ErrorResponse(
                            "Sefer başladığı için rezervasyon iptal edilemez. Lütfen müşteri hizmetleri ile iletişime geçin."
                        );
                    }
                }

                // İptal işlemini gerçekleştir (CancelReasonID ve kullanıcı ID'sini log için geç)
                var cancelReasonID = cancelDto?.CancelReasonID;
                var result = await _reservationRepository.CancelAsync(id, cancelReasonID, reservation.UserID);

                if (!result)
                {
                    return ApiResponse<bool>.ErrorResponse("Rezervasyon iptal edilemedi");
                }

                _logger.LogInformation("Rezervasyon iptal edildi. ReservationID: {ReservationID}, TripID: {TripID}, UserID: {UserID}", 
                    reservation.ReservationID, reservation.TripID, reservation.UserID);

                return ApiResponse<bool>.SuccessResponse(true, "Rezervasyon başarıyla iptal edildi. İade işlemi otomatik olarak başlatılacaktır.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rezervasyon iptal edilirken hata oluştu. ID: {Id}", id);
                return ApiResponse<bool>.ErrorResponse(
                    "Rezervasyon iptal edilirken bir hata oluştu",
                    new List<string> { ex.Message }
                );
            }
        }

        private ReservationDTO MapToDTO(Reservation reservation)
        {
            var trip = reservation.TripSeat?.Trip;
            var seat = reservation.TripSeat?.Seat;
            var vehicle = trip?.Vehicle;

            return new ReservationDTO
            {
                ReservationID = reservation.ReservationID,
                UserID = reservation.UserID,
                UserName = reservation.User?.FullName ?? string.Empty,
                TripID = reservation.TripID,
                SeatID = reservation.SeatID,
                SeatNumber = seat?.SeatNo ?? string.Empty,
                VehicleType = vehicle?.VehicleType ?? string.Empty,
                FromCity = trip?.FromCity?.CityName ?? string.Empty,
                ToCity = trip?.ToCity?.CityName ?? string.Empty,
                DepartureDate = trip?.DepartureDate ?? DateTime.MinValue,
                DepartureTime = trip?.DepartureTime ?? TimeSpan.Zero,
                ArrivalDate = trip?.ArrivalDate,
                ArrivalTime = trip?.ArrivalTime,
                Price = trip?.Price ?? 0,
                ReservationDate = reservation.ReservationDate,
                Status = reservation.Status,
                PaymentStatus = reservation.PaymentStatus,
                CancelReason = reservation.CancelReason?.ReasonText
            };
        }

        private ReservationDTO MapFromUserTicket(UserTicketDTO ticket)
        {
            // Guzergah formatı: "İstanbul > Ankara"
            var cities = ticket.Guzergah.Split(" > ");
            var fromCity = cities.Length > 0 ? cities[0] : string.Empty;
            var toCity = cities.Length > 1 ? cities[1] : string.Empty;

            // KalkisSaati string formatından TimeSpan'e çevir (HH:mm formatında)
            TimeSpan departureTime = TimeSpan.Zero;
            if (TimeSpan.TryParse(ticket.KalkisSaati, out var parsedTime))
            {
                departureTime = parsedTime;
            }

            return new ReservationDTO
            {
                ReservationID = ticket.ReservationID,
                UserID = 0, // SP'de UserID yok, gerekirse eklenebilir
                UserName = string.Empty, // SP'de UserName yok
                TripID = ticket.TripID, // SP'den TripID alınıyor
                SeatID = 0, // SP'de SeatID yok, gerekirse eklenebilir
                SeatNumber = ticket.SeatNo,
                VehicleType = ticket.VehicleType,
                FromCity = fromCity,
                ToCity = toCity,
                DepartureDate = ticket.DepartureDate,
                DepartureTime = departureTime,
                ArrivalDate = null, // SP'de varış tarihi yok
                ArrivalTime = null, // SP'de varış saati yok
                Price = ticket.OdenenTutar,
                ReservationDate = ticket.IslemTarihi,
                Status = ticket.RezervasyonDurumu,
                PaymentStatus = ticket.PaymentStatus,
                CancelReason = null // SP'de iptal nedeni yok
            };
        }
    }
}
