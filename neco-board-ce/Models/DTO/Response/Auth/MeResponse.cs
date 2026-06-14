namespace neco_board_ce.Models.DTO.Response.Auth
{
    public class MeResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Avatar { get; set; } = string.Empty;
    }
}
