using FluentValidation;
using static MeetinRoomRezervation.Components.Pages.Login;


public class LoginModelValidator : AbstractValidator<LoginInputModel>
{
	public LoginModelValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("E-posta alanı zorunludur")
			.EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("Şifre alanı zorunludur");
	}
}