using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using backend.Models.DTOs;

namespace backend.Services;

public class CloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly string _cloudName = string.Empty;
    private readonly string _apiKey = string.Empty;
    private readonly IConfiguration _config;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _config = config;
        _logger = logger;

        var cloudName = _config["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? string.Empty;
        var apiKey = _config["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? string.Empty;
        var apiSecret = _config["Cloudinary:ApiSecret"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? string.Empty;

        var cloudinaryUrl = _config.GetConnectionString("Cloudinary") ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL");

        if (!string.IsNullOrEmpty(cloudinaryUrl))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
            _cloudinary.Api.Secure = true;
            _cloudName = _cloudinary.Api.Account.Cloud;
            _apiKey = _cloudinary.Api.Account.ApiKey;
        }
        else if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
            _cloudName = cloudName;
            _apiKey = apiKey;
        }
    }

    public bool IsConfigured => _cloudinary != null;

    public CloudinarySignatureDto GenerateSignedUploadParams(string folder = "recruitov/avatars")
    {
        if (_cloudinary == null)
        {
            return new CloudinarySignatureDto { IsConfigured = false };
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parameters = new Dictionary<string, object>
        {
            { "folder", folder },
            { "timestamp", timestamp }
        };

        var signature = _cloudinary.Api.SignParameters(parameters);

        return new CloudinarySignatureDto
        {
            IsConfigured = true,
            Signature = signature,
            Timestamp = timestamp,
            ApiKey = _apiKey,
            CloudName = _cloudName,
            Folder = folder
        };
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (_cloudinary == null || string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Cloudinary asset {PublicId}", publicId);
            return false;
        }
    }
}
