using FluentValidation.TestHelper;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.DTO.Validations;
using Xunit;

namespace neco_board_ce.Tests.Unit.Validations
{
    public class ProjectRequestValidatorTests
    {
        private readonly ProjectRequestValidator _validator;

        public ProjectRequestValidatorTests()
        {
            _validator = new ProjectRequestValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new ProjectRequest { Name = string.Empty };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Project name is required.");
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Too_Long()
        {
            var model = new ProjectRequest { Name = new string('a', 101) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Project name cannot exceed 100 characters.");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Too_Long()
        {
            var model = new ProjectRequest { Name = "Valid Name", Description = new string('a', 1001) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description cannot exceed 1000 characters.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_Model_Is_Valid()
        {
            var model = new ProjectRequest 
            { 
                Name = "Valid Project Name", 
                Description = "Valid Description" 
            };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
