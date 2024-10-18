using Azure.Identity;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace ComputerName
{
    public class RenameComputerByCSV
    {
        private readonly ILogger<RenameComputerByCSV> _logger;
        
        public struct ComputerName
        {
            [Index(0)]
            public string SerialNumber { get; set; }
            [Index(1)]
            public string DesiredName { get; set; }
        }

        public RenameComputerByCSV(ILogger<RenameComputerByCSV> logger)
        {
            _logger = logger;
        }

        [Function("RenameComputerByCSV")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Request body is empty.");
            }

            try
            {
                // Attempt To deserialize the request
                ComputerNameRequest? request = JsonSerializer.Deserialize<ComputerNameRequest>(requestBody);
                if (request == null) { return new BadRequestObjectResult("The request body is not correctly formed."); }

                // Initialize the Managed Identity to get access to the Azure Blob Storage
                DefaultAzureCredential managedIdentity = new DefaultAzureCredential();

                // Client to work with to request storage blobs in the Blob URL
                BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(request.BlobURL), managedIdentity);

                // Get the container that stores the blob we want to gather
                BlobContainerClient blobContainer = blobServiceClient.GetBlobContainerClient(request.BlobContainerName);

                // Get the Content of the CSV
                BlobClient blobClient = blobContainer.GetBlobClient(request.CSV);

                using (var blobStream = await blobClient.OpenReadAsync())
                {
                    using (var reader = new StreamReader(blobStream))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        // Find the record in the CSV where the serial number matches the request
                        IEnumerable<ComputerName> record = csv.GetRecords<ComputerName>().ToList().Where(record => record.SerialNumber == request.SerialNumber);

                        if(record == null)
                        {
                            _logger.LogWarning($"Request Serial Number ({request.SerialNumber}) was not found in the CSV ({request.CSV})");
                            return new OkObjectResult(string.Empty);
                        }

                        if(record.Count() > 1)
                        {
                            _logger.LogWarning($"There is {record.Count()} instances of {request.SerialNumber}. Sending back the first one.");
                        }

                        _logger.LogInformation($"New Computer Name: {record.First().DesiredName}");
                        return new OkObjectResult(record.First().DesiredName);

                    }
                }
            }
            catch(Exception exception)
            {
                _logger.LogError(exception.ToString());
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }
    }
}
