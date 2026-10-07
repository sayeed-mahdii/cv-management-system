using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace backend.Services;

/// <summary>
/// Service for uploading files to cloud storage providers (Dropbox, OneDrive)
/// to trigger automated Power Automate Cloud Flows.
/// </summary>
public class CloudStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<CloudStorageService> _logger;

    public CloudStorageService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<CloudStorageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a JSON string file to the specified cloud provider.
    /// </summary>
    public async Task<(bool success, string? cloudPath, string message)> UploadJsonFileAsync(
        string fileName,
        string jsonContent,
        string provider = "dropbox",
        string? userSuppliedToken = null)
    {
        var bytes = Encoding.UTF8.GetBytes(jsonContent);

        if (string.Equals(provider, "dropbox", StringComparison.OrdinalIgnoreCase))
        {
            return await UploadToDropboxAsync(fileName, bytes, userSuppliedToken);
        }
        else if (string.Equals(provider, "onedrive", StringComparison.OrdinalIgnoreCase))
        {
            return await UploadToOneDriveAsync(fileName, bytes, userSuppliedToken);
        }

        // Fallback to local file backup
        return await SaveToLocalFolderAsync(fileName, bytes);
    }

    private async Task<(bool success, string? cloudPath, string message)> UploadToDropboxAsync(
        string fileName,
        byte[] fileBytes,
        string? userSuppliedToken)
    {
        var token = !string.IsNullOrWhiteSpace(userSuppliedToken)
            ? userSuppliedToken.Trim()
            : _config["Dropbox:AccessToken"]
              ?? Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            // If no token is provided, save locally as fallback
            var (localSuccess, localPath, _) = await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, localPath, "Dropbox Access Token is not configured. Saved file locally for testing.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var targetPath = $"/tickets/{fileName}";

            var dropboxArg = JsonSerializer.Serialize(new
            {
                path = targetPath,
                mode = "add",
                autorename = true,
                mute = false
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
            {
                Content = new ByteArrayContent(fileBytes)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Dropbox-API-Arg", dropboxArg);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully uploaded {FileName} to Dropbox at {Path}", fileName, targetPath);
                return (true, targetPath, $"Successfully uploaded to Dropbox ({targetPath}). Power Automate flow triggered.");
            }
            else
            {
                _logger.LogWarning("Dropbox upload failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
                await SaveToLocalFolderAsync(fileName, fileBytes);
                return (false, null, $"Dropbox upload failed ({response.StatusCode}): {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while uploading {FileName} to Dropbox", fileName);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"Dropbox upload error: {ex.Message}");
        }
    }

    private async Task<(bool success, string? cloudPath, string message)> UploadToOneDriveAsync(
        string fileName,
        byte[] fileBytes,
        string? userSuppliedToken)
    {
        var token = !string.IsNullOrWhiteSpace(userSuppliedToken)
            ? userSuppliedToken.Trim()
            : _config["OneDrive:AccessToken"]
              ?? Environment.GetEnvironmentVariable("ONEDRIVE_ACCESS_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            var (localSuccess, localPath, _) = await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, localPath, "OneDrive Access Token is not configured. Saved file locally for testing.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var targetUrl = $"https://graph.microsoft.com/v1.0/me/drive/root:/tickets/{fileName}:/content";

            using var request = new HttpRequestMessage(HttpMethod.Put, targetUrl)
            {
                Content = new ByteArrayContent(fileBytes)
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully uploaded {FileName} to OneDrive", fileName);
                return (true, $"/tickets/{fileName}", $"Successfully uploaded to OneDrive (/tickets/{fileName}). Power Automate flow triggered.");
            }
            else
            {
                _logger.LogWarning("OneDrive upload failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
                await SaveToLocalFolderAsync(fileName, fileBytes);
                return (false, null, $"OneDrive upload failed ({response.StatusCode}): {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while uploading {FileName} to OneDrive", fileName);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"OneDrive upload error: {ex.Message}");
        }
    }

    private async Task<(bool success, string? cloudPath, string message)> SaveToLocalFolderAsync(string fileName, byte[] fileBytes)
    {
        try
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "tickets");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filePath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(filePath, fileBytes);
            return (true, filePath, $"Saved to local backup directory ({filePath}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ticket locally");
            return (false, null, $"Local save failed: {ex.Message}");
        }
    }
}
