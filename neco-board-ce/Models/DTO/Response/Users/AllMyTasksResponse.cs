using neco_board_ce.Models.DTO.Response.Task;

namespace neco_board_ce.Models.DTO.Response.Users
{
    public class AllMyTasksResponse
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public List<MyTaskResponse> Tasks = new List<MyTaskResponse>();
    }
}
