using neco_board_ce.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace neco_board_ce.Models.DTO.Request.Tasks
{
    public class EditTaskPriorityRequest
    {
        [Required]
        public TaskPriority Priority { get; set; }
    }
}
