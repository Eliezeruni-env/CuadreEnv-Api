using FluentValidation;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Validators
{
    public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            // Enforce stronger password rules for production readiness
            // Minimum 8 chars, at least one uppercase, one lowercase, one digit and one special char
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }

    public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class RefreshRequestDtoValidator : AbstractValidator<RefreshRequestDto>
    {
        public RefreshRequestDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class RevokeRequestDtoValidator : AbstractValidator<RevokeRequestDto>
    {
        public RevokeRequestDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
