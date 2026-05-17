namespace neco_board_ce.Models.Results
{
    public record AuthResult
    (
        bool Success,
        string? AccessToken = null,
        string? RefreshToken = null,
        string? Error = null
    );
}
