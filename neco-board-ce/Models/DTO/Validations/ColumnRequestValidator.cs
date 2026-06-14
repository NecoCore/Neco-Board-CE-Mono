using FluentValidation;
using neco_board_ce.Models.DTO.Request.Columns;

namespace neco_board_ce.Models.DTO.Validations
{
    public class ColumnRequestValidator : AbstractValidator<ColumnRequest>
    {
        public ColumnRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Column name is required.")
                .MaximumLength(100).WithMessage("Column name cannot exceed 100 characters.");

            RuleFor(x => x.Color)
                .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$").WithMessage("Invalid HEX color format.");
        }
    }
}
