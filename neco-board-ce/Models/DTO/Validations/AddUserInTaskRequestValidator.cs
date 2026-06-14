using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class AddUserInTaskRequestValidator : AbstractValidator<AddUserInTaskRequest>
    {
        public AddUserInTaskRequestValidator()
        {
            RuleFor(x => x.UserId)
                .Must(id => id == null || Guid.TryParse(id, out _))
                .WithMessage("User ID must be a valid GUID.");
        }
    }
}
