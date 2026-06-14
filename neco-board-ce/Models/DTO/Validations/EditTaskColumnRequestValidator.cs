using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class EditTaskColumnRequestValidator : AbstractValidator<EditTaskColumnRequest>
    {
        public EditTaskColumnRequestValidator()
        {
            RuleFor(x => x.ColumnId)
                .NotEmpty().WithMessage("Column ID is required.")
                .Must(id => Guid.TryParse(id, out _)).WithMessage("Column ID must be a valid GUID.");
        }
    }
}
