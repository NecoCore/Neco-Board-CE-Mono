using FluentValidation;
using neco_board_ce.Models.DTO.Request.Tasks;

namespace neco_board_ce.Models.DTO.Validations
{
    public class AddUserInTaskRequestValidator : AbstractValidator<AddUserInTaskRequest>
    {
        public AddUserInTaskRequestValidator()
        {
            // Guid? validation is handled by type system, but we can add specific rules if needed.
            // For now, no specific rules required for UserId.
        }
    }
}
