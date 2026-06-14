using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditTaskPriorityRequestValidator : AbstractValidator<EditTaskPriorityRequest>
    {
        public EditTaskPriorityRequestValidator()
        {
            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid task priority.");
        }
    }
}
