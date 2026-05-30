namespace neco_board_ce.Models.DTO.Response.Auth
{
    public class MeResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Avatar { get; set; } = string.Empty;
    }
}
