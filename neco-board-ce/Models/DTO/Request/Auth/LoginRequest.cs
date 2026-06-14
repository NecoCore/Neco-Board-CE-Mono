namespace neco_board_ce.Models.DTO.Request.Auth
{
    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    };
}
