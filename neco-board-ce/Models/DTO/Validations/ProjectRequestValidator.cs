using FluentValidation;
using neco_board_ce.Models.DTO.Request.Projects;

namespace neco_board_ce.Models.DTO.Validations
{
    public class ProjectRequestValidator : AbstractValidator<ProjectRequest>
    {
        public ProjectRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(100).WithMessage("Project name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        }
    }
}
