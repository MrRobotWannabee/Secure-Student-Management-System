using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

public class ImageUploadService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly string _connectionString;
    private readonly ILogger<ImageUploadService> _logger;

    public ImageUploadService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<ImageUploadService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _containerName = configuration["AzureStorage:ContainerName"]
            ?? throw new ArgumentNullException("Container name configuration is missing");
        _connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new ArgumentNullException("Azure Storage connection string is missing");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadImageAsync(IFormFile file, int width = 200, int height = 200)
    {
        const long MaxFileSize = 2 * 1024 * 1024; // 2MB
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };

        // 1. Validate File Size
        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("File size exceeds limit: {FileName} ({FileSize} bytes)",
                file.FileName, file.Length);
            throw new ArgumentException("File size exceeds 2MB limit");
        }

        // 2. Validate File Extension
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("Invalid file extension: {FileName}", file.FileName);
            throw new ArgumentException("Only JPEG/PNG files are allowed");
        }

        try
        {
            // 3. Validate File Content
            using var uploadStream = new MemoryStream();
            IImageFormat format;

            // First pass: Validate actual image format
            using (var validationStream = file.OpenReadStream())
            {
                format = Image.DetectFormat(validationStream);
                if (format != JpegFormat.Instance && format != PngFormat.Instance)
                {
                    throw new ArgumentException("Invalid image content");
                }
                validationStream.Position = 0; // Reset stream for processing

                // Second pass: Process image
                using (var image = Image.Load(validationStream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Max
                    }));

                    // Save processed image
                    IImageEncoder encoder = format == PngFormat.Instance
                        ? new PngEncoder()
                        : new JpegEncoder { Quality = 80 };

                    image.Save(uploadStream, encoder);
                    uploadStream.Position = 0;
                }
            }

            // 4. Upload to Azure Blob Storage
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{Guid.NewGuid()}{fileExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(uploadStream, overwrite: true);
            _logger.LogInformation("Uploaded image: {BlobName}", blobName);

            // 5. Generate Secure Access URL
            return GenerateSasToken(blobClient).ToString();
        }
        catch (UnknownImageFormatException ex)
        {
            _logger.LogError(ex, "Invalid image content: {FileName}", file.FileName);
            throw new ArgumentException("File is not a valid image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed: {FileName}", file.FileName);
            throw new InvalidOperationException("Image upload failed");
        }
    }

    private Uri GenerateSasToken(BlobClient blobClient, int expiryMinutes = 10)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var storageCredential = new Azure.Storage.StorageSharedKeyCredential(
            _blobServiceClient.AccountName,
            GetAccountKeyFromConnectionString(_connectionString)
        );

        var sasToken = sasBuilder.ToSasQueryParameters(storageCredential).ToString();
        return new UriBuilder(blobClient.Uri) { Query = sasToken }.Uri;
    }

    private string GetAccountKeyFromConnectionString(string connectionString)
    {
        foreach (var segment in connectionString.Split(';'))
        {
            if (segment.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
            {
                return segment.Substring("AccountKey=".Length);
            }
        }
        throw new InvalidOperationException("AccountKey not found in connection string");
    }
}