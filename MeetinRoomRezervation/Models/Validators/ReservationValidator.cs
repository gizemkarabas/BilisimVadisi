using FluentValidation;
using MeetinRoomRezervation.Models;

public class ReservationValidator : AbstractValidator<ReservationDto>
{
    public ReservationValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Toplantı odası seçimi zorunludur");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Başlangıç zamanı zorunludur")
            .Must(BeValidStartTime).WithMessage("Başlangıç zamanı geçmiş bir zaman olamaz");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Bitiş zamanı zorunludur")
            .GreaterThan(x => x.StartTime).WithMessage("Bitiş zamanı başlangıç zamanından sonra olmalıdır");

        RuleFor(x => x)
            .Must(HaveValidDuration).WithMessage("Rezervasyon süresi en az 1 saat, en fazla 8 saat olabilir");

    }

    private bool BeValidStartTime(DateTime startTime)
    {
        return startTime > DateTime.Now.AddMinutes(-5); // 5 dakika tolerans
    }

    private bool HaveValidDuration(ReservationDto reservation)
    {
        if (reservation.StartTime == default || reservation.EndTime == default)
            return true; // Diğer validasyonlar handle edecek

        var duration = reservation.EndTime - reservation.StartTime;
        return duration.TotalHours >= 1 && duration.TotalHours <= 8;
    }
}
