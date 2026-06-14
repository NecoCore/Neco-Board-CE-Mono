using neco_board_ce.Models.DTO.Response.Task;

namespace neco_board_ce.Models.DTO.Response.Users
{
    public class AllMyTasksResponse
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public List<MyTaskResponse> Tasks = new List<MyTaskResponse>();
    }
}
