using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Interfaces;

namespace Recruitov.Infrastructure.Services;

public class CloudStorageService(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<CloudStorageService> logger) : ICloudStorageService
{
    public bool HasConfiguredDropboxToken =>
        !string.IsNullOrWhiteSpace(config["Dropbox:AccessToken"]) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN"));

    public bool HasConfiguredOneDriveToken =>
        !string.IsNullOrWhiteSpace(config["OneDrive:AccessToken"]) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ONEDRIVE_ACCESS_TOKEN"));

    public Task<(bool Success, string? CloudPath, string Message)> UploadJsonFileAsync(
        string fileName,
        string jsonContent,
        string provider = "dropbox",
        string? userSuppliedToken = null)
    {
        var bytes = Encoding.UTF8.GetBytes(jsonContent);

        if (string.Equals(provider, "dropbox", StringComparison.OrdinalIgnoreCase))
            return UploadToDropboxAsync(fileName, bytes, userSuppliedToken);

        if (string.Equals(provider, "onedrive", StringComparison.OrdinalIgnoreCase))
            return UploadToOneDriveAsync(fileName, bytes, userSuppliedToken);

        return SaveToLocalFolderAsync(fileName, bytes);
    }

    private async Task<(bool Success, string? CloudPath, string Message)> UploadToDropboxAsync(
        string fileName,
        byte[] fileBytes,
        string? userSuppliedToken)
    {
        var token = !string.IsNullOrWhiteSpace(userSuppliedToken)
            ? userSuppliedToken.Trim()
            : config["Dropbox:AccessToken"]
              ?? Environment.GetEnvironmentVariable("DROPBOX_ACCESS_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            var (_, localPath, _) = await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, localPath, "Dropbox Access Token is not configured. Saved file locally for testing.");
        }

        try
        {
            var client = httpClientFactory.CreateClient();
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
                logger.LogInformation("Successfully uploaded {FileName} to Dropbox at {Path}", fileName, targetPath);
                return (true, targetPath, $"Successfully uploaded to Dropbox ({targetPath}). Power Automate flow triggered.");
            }

            logger.LogWarning("Dropbox upload failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"Dropbox upload failed ({response.StatusCode}): {responseBody}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while uploading {FileName} to Dropbox", fileName);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"Dropbox upload error: {ex.Message}");
        }
    }

    private async Task<(bool Success, string? CloudPath, string Message)> UploadToOneDriveAsync(
        string fileName,
        byte[] fileBytes,
        string? userSuppliedToken)
    {
        var token = !string.IsNullOrWhiteSpace(userSuppliedToken)
            ? userSuppliedToken.Trim()
            : config["OneDrive:AccessToken"]
              ?? Environment.GetEnvironmentVariable("ONEDRIVE_ACCESS_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            var (_, localPath, _) = await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, localPath, "OneDrive Access Token is not configured. Saved file locally for testing.");
        }

        try
        {
            var client = httpClientFactory.CreateClient();
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
                logger.LogInformation("Successfully uploaded {FileName} to OneDrive", fileName);
                return (true, $"/tickets/{fileName}", $"Successfully uploaded to OneDrive (/tickets/{fileName}). Power Automate flow triggered.");
            }

            logger.LogWarning("OneDrive upload failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"OneDrive upload failed ({response.StatusCode}): {responseBody}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while uploading {FileName} to OneDrive", fileName);
            await SaveToLocalFolderAsync(fileName, fileBytes);
            return (false, null, $"OneDrive upload error: {ex.Message}");
        }
    }

    private async Task<(bool Success, string? CloudPath, string Message)> SaveToLocalFolderAsync(string fileName, byte[] fileBytes)
    {
        try
        {
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "tickets");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(filePath, fileBytes);
            return (true, filePath, $"Saved to local backup directory ({filePath}).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save ticket locally");
            return (false, null, $"Local save failed: {ex.Message}");
        }
    }
}
