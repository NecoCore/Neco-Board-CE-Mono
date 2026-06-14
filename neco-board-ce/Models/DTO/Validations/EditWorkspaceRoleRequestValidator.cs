using FluentValidation;
using neco_board_ce.Models.DTO.Request.Users;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditWorkspaceRoleRequestValidator : AbstractValidator<EditWorkspaceRoleRequest>
    {
        public EditWorkspaceRoleRequestValidator()
        {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid workspace role specified.");
        }
    }
}
