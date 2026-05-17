using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Tasks
{
    public class EditTaskStatusRequest
    {
        [Required]
        public ColumnTaskStatus Status { get; set; }
    }
}
