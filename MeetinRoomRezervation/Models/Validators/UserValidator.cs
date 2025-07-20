using FluentValidation;
using MeetinRoomRezervation.Models;

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı zorunludur")
            .Length(2, 50).WithMessage("Ad 2-50 karakter arasında olmalıdır")
            .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Ad sadece harf içerebilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı zorunludur")
            .Length(2, 50).WithMessage("Soyad 2-50 karakter arasında olmalıdır")
            .Matches(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$").WithMessage("Soyad sadece harf içerebilir");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta alanı zorunludur")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
            .Length(5, 100).WithMessage("E-posta adresi 5-100 karakter arasında olmalıdır");

        RuleFor(x => x.Company)
            .NotEmpty().WithMessage("Şirket adı zorunludur")
            .Length(2, 100).WithMessage("Şirket adı 2-100 karakter arasında olmalıdır");

        RuleFor(x => x.CompanyOfficial)
            .NotEmpty().WithMessage("Yetkili kişi adı zorunludur")
            .Length(2, 100).WithMessage("Yetkili kişi adı 2-100 karakter arasında olmalıdır");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Telefon numarası zorunludur")
            .Matches(@"^(\+90|0)?[5][0-9]{9}$").WithMessage("Geçerli bir Türkiye telefon numarası giriniz (örn: 05xxxxxxxxx)")
            .Length(10,11).WithMessage("Geçerli bir telefon numarası giriniz");

        RuleFor(x => x.MonthlyUsageLimit)
            .GreaterThan(0).WithMessage("Aylık kullanım limiti 0'dan büyük olmalıdır")
            .LessThanOrEqualTo(200).WithMessage("Aylık kullanım limiti 200 saati geçemez");
    }
}
