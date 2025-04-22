using FluentValidation;
using MeetinRoomRezervation.Models;

public class MeetingRoomValidator : AbstractValidator<MeetingRoomDto>
{
	public MeetingRoomValidator()
	{
		RuleFor(room => room.Name)
			.NotEmpty().WithMessage("Oda adı boş olamaz")
			.MinimumLength(3).WithMessage("Oda adı en az 3 karakter olmalı");

		RuleFor(room => room.Capacity)
			.GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalı");
	}
}
