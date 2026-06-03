namespace neco_board_ce.Models.Results
{
    public class CheckResult
    {
        public bool Result { get; set; }
        public string? Message { get; set; } = string.Empty;
        public string? ProjectId { get; set; } = string.Empty;
    }
}
