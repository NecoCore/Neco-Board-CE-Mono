using neco_board_ce.Interfaces;

namespace neco_board_ce.Services.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorage> _logger;

        public LocalFileStorage(IWebHostEnvironment env, IConfiguration config, ILogger<LocalFileStorage> logger)
        {
            _basePath = config["Storage:Local:BasePath"]
                ?? Path.Combine(env.ContentRootPath, "uploads");
            _logger = logger;
        }

        public Task<Stream> GetAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            _logger.LogInformation($"Looking for file: {fullPath}");
            if (!File.Exists(fullPath)) throw new FileNotFoundException($"File not found: {fullPath}");
            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder)
        {
            var dir = Path.Combine(_basePath, folder);
            Directory.CreateDirectory(dir);

            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var fullPath = Path.Combine(dir, uniqueName);

            await using var fs = File.Create(fullPath);
            await fileStream.CopyToAsync(fs);

            return Path.Combine(folder, uniqueName);
        }

        public async Task<bool> Exists(string filePath)
        {
            return await Task.FromResult(File.Exists(Path.Combine(_basePath, filePath)));
        }

        public Task DeleteAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}
