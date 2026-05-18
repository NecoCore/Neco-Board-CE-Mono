namespace neco_board_ce.Models.Results
{
    public class RepositoryResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
