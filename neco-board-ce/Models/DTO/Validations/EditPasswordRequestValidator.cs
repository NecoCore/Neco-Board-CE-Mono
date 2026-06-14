using FluentValidation;
using neco_board_ce.Models.DTO.Request.Auth;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditPasswordRequestValidator : AbstractValidator<EditPasswordRequest>
    {
        public EditPasswordRequestValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Old password is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("New password is required.")
                .Length(6, 100).WithMessage("New password must be between 6 and 100 characters.")
                .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
                .Matches(@"\d").WithMessage("New password must contain at least one number.")
                .NotEqual(x => x.OldPassword).WithMessage("New password must be different from the old password.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }
    }
}
