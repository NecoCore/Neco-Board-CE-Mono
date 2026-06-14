using FluentValidation;
using neco_board_ce.Models.DTO.Request.Projects;

namespace neco_board_ce.Models.DTO.Validations
{
    public class UserProjectRequestValidator : AbstractValidator<UserProjectRequest>
    {
        public UserProjectRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User ID is required.")
                .NotEqual(Guid.Empty).WithMessage("User ID cannot be empty.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid project role.");
        }
    }
}
