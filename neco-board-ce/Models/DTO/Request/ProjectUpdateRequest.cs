using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request
{
    public class ProjectUpdateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? OwnerId { get; set; }
    }
}
