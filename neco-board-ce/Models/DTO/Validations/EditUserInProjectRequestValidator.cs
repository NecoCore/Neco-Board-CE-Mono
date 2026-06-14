using FluentValidation;
using neco_board_ce.Models.DTO.Request.Projects;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditUserInProjectRequestValidator : AbstractValidator<EditUserInProjectRequest>
    {
        public EditUserInProjectRequestValidator()
        {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid project role.");
        }
    }
}
