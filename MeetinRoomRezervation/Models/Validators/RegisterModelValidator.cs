using FluentValidation;
using static MeetinRoomRezervation.Components.Pages.Register;

public class RegisterInputModelValidator : AbstractValidator<RegisterInputModel>
{
	public RegisterInputModelValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Ad alanı zorunludur");

		RuleFor(x => x.Surname)
			.NotEmpty().WithMessage("Soyad alanı zorunludur");

		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("E-posta alanı zorunludur")
			.EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("Şifre alanı zorunludur")
			.MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır");

		RuleFor(x => x.ConfirmPassword)
			.NotEmpty().WithMessage("Şifre tekrar alanı zorunludur")
			.Equal(x => x.Password).WithMessage("Şifreler uyuşmuyor");
	}
}
