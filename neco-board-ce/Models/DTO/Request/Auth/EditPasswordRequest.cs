namespace neco_board_ce.Models.DTO.Request.Auth
{
    public class EditPasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
