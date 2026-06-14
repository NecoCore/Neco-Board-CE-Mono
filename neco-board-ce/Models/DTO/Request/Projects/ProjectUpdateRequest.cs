namespace neco_board_ce.Models.DTO.Request.Projects
{
    public class ProjectUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OwnerId { get; set; }
    }
}
