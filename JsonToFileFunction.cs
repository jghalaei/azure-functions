using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Newtonsoft.Json.Linq;

public static class JsonToFileFunction
{
    [Function("JsonToFileFunction")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        // Deserialize the JSON from the request body
        string? fileContent = await GetFileContent(req);
        ArgumentNullException.ThrowIfNull(fileContent);

        string blobName = await SaveToStorage(fileContent);

        // Return a success response
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync($"File created from JSON and uploaded successfully: {blobName}");
        return response;
    }

    private static async Task<string> SaveToStorage(string fileContent)
    {
        // Prepare the blob storage connection
        string connectionString = Environment.GetEnvironmentVariable("StorageConnectionString") ?? string.Empty; // Ensure this is set in your environment variables
        string containerName = Environment.GetEnvironmentVariable("StorageContainerName") ?? string.Empty; ; // Update with your container name
        string blobName = $"{Guid.NewGuid()}.txt";

        // Upload the file content to the blob
        var blobServiceClient = new BlobServiceClient(connectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        // Convert the file content to a byte array and upload it as a stream
        byte[] byteArray = Encoding.UTF8.GetBytes(fileContent);
        using (var stream = new MemoryStream(byteArray))
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        return blobName;
    }

    private static async Task<string?> GetFileContent(HttpRequestData req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        JObject json = JObject.Parse(requestBody);

        // Convert the JSON object to a string (or however you wish to format it)
        string? fileContent = json.GetValue("fileContent")?.ToString();
        return fileContent;
    }
}
