using FluentValidation;
using neco_board_ce.Models.DTO.Request.Auth;

namespace neco_board_ce.Models.DTO.Validations
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(3, 100).WithMessage("Name must be between 3 and 100 characters.");

            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Login is required.")
                .Length(3, 50).WithMessage("Login must be between 3 and 50 characters.")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Login can only contain letters, numbers, and underscores.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Length(6, 100).WithMessage("Password must be between 6 and 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("Password  must contain at least one number");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");
        }
    }
}
