using neco_board_ce.Models.Enums;

namespace neco_board_ce.Models.DTO.Request.Tasks
{
    public class EditTaskStatusRequest
    {
        public ColumnTaskStatus Status { get; set; }
    }
}
