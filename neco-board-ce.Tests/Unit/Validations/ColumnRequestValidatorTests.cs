using FluentValidation.TestHelper;
using neco_board_ce.Models.DTO.Request.Columns;
using neco_board_ce.Models.DTO.Validations;
using Xunit;

namespace neco_board_ce.Tests.Unit.Validations
{
    public class ColumnRequestValidatorTests
    {
        private readonly ColumnRequestValidator _validator;

        public ColumnRequestValidatorTests()
        {
            _validator = new ColumnRequestValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new ColumnRequest { Name = string.Empty };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Column name is required.");
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Too_Long()
        {
            var model = new ColumnRequest { Name = new string('a', 101) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Column name cannot exceed 100 characters.");
        }

        [Theory]
        [InlineData("red")]
        [InlineData("123456")]
        [InlineData("#GG0000")]
        [InlineData("#1234567")]
        public void Should_Have_Error_When_Color_Is_Invalid(string invalidColor)
        {
            var model = new ColumnRequest { Name = "Valid", Color = invalidColor };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Color)
                  .WithErrorMessage("Invalid HEX color format.");
        }

        [Theory]
        [InlineData("#FFF")]
        [InlineData("#FFFFFF")]
        [InlineData("#000000")]
        [InlineData("#abc")]
        public void Should_Not_Have_Error_When_Color_Is_Valid(string validColor)
        {
            var model = new ColumnRequest { Name = "Valid", Color = validColor };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Color);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Model_Is_Valid()
        {
            var model = new ColumnRequest 
            { 
                Name = "Valid Column", 
                Color = "#FFFFFF" 
            };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
