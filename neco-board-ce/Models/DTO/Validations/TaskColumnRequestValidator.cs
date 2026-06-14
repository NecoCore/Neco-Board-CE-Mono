using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class TaskColumnRequestValidator : AbstractValidator<TaskColumnRequest>
    {
        public TaskColumnRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Task name is required.")
                .MaximumLength(250).WithMessage("Task name cannot exceed 250 characters.");

            RuleFor(x => x.ColumnId)
                .NotEmpty().WithMessage("Column ID is required.")
                .NotEqual(Guid.Empty).WithMessage("Column ID cannot be empty.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid task priority.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid task status.");
        }
    }
}
