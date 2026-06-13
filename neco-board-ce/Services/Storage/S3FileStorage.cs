using Amazon;
using Amazon.S3;
using System.Net;
using neco_board_ce.Interfaces;

namespace neco_board_ce.Services.Storage
{
    public class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;

        public S3FileStorage(IConfiguration config)
        {
            var region = RegionEndpoint.GetBySystemName(config["Storage:S3:Region"]);

            _s3 = new AmazonS3Client(
                config["Storage:S3:AccessKey"],
                config["Storage:S3:SecretKey"],
                region
            );
            _bucket = config["Storage:S3:Bucket"] ?? throw new ArgumentNullException("S3 bucket name is required");
        }


        public async Task<Stream> GetAsync(string filePath)
        {
            var response = await _s3.GetObjectAsync(_bucket, filePath);
            return response.ResponseStream;
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder, string? overrideName = null)
        {
            var finalName = string.IsNullOrEmpty(overrideName)
                ? $"{Guid.NewGuid()}{Path.GetExtension(fileName)}"
                : $"{overrideName}{Path.GetExtension(fileName)}";

            var key = $"{folder}/{finalName}";

            await _s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = fileStream,
                AutoCloseStream = false
            });

            return key;
        }

        public async Task<bool> Exists(string filePath)
        {
            try
            {
                await _s3.GetObjectMetadataAsync(_bucket, filePath);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task DeleteAsync(string filePath)
        {
            await _s3.DeleteObjectAsync(_bucket, filePath);
        }
    }
}
