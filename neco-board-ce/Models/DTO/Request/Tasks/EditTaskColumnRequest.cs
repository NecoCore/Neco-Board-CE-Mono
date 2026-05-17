using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Tasks
{
    public class EditTaskColumnRequest
    {
        [Required]
        public string ColumnId { get; set; } = string.Empty;
    }
}
