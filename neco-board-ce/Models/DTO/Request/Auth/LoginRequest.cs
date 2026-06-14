using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Auth
{
    public record LoginRequest
    (
        [Required] string Login,
        [Required] string Password
    );
}
