namespace neco_board_ce.Interfaces
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream fileStream, string fileName, string folder, string? overrideName = null);
        Task<Stream> GetAsync(string filePath);
        Task DeleteAsync(string filePath);
        Task<bool> Exists(string filePath);
    }
}