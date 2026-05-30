namespace neco_board_ce.Models.Results
{
    public class ChekResult
    {
        public bool Result { get; set; }
        public string? Message { get; set; } = string.Empty;
        public string? ProjectId { get; set; } = string.Empty;
    }
}
