using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditTaskStatusRequestValidator : AbstractValidator<EditTaskStatusRequest>
    {
        public EditTaskStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid task status.");
        }
    }
}
