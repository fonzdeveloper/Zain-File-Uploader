using Microsoft.AspNetCore.Mvc;
using ZBlobStorage.Models;
using System.Diagnostics;
using AESCTR_Standard;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using Azure.Storage.Blobs.Models;
using System.Web;

namespace ZBlobStorage.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly string _storageConnectionString;
        private readonly string _containerName;
        private readonly IConfiguration _configuration;

        #region Ctor
        public HomeController(ILogger<HomeController> logger,
             IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Retrieve encrypted values
            string encryptedConnectionString = _configuration.GetValue<string>("AzureBlobStorage:ConnectionString");
            string encryptedContainerName = _configuration.GetValue<string>("AzureBlobStorage:ContainerName");
            string encryptionKey = _configuration.GetValue<string>("puller");

            // Decrypt values
            _storageConnectionString = AESCTR.Decrypt(encryptedConnectionString, encryptionKey).Content;
            _containerName = AESCTR.Decrypt(encryptedContainerName, encryptionKey).Content;
        }

        #endregion
        #region Method

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // Fetch the list of blobs from Azure Blob Storage
                BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                if (containerClient != null)
                {
                    var blobs = containerClient.GetBlobs();

                    // Filter blobs to get only folders
                    var folders = blobs
                        .Where(blob => blob.Name.Contains("/") && !blob.Name.EndsWith("/"))
                        .Select(blob => blob.Name.Split('/')[0])
                        .Distinct()
                        .ToList();

                    ViewBag.Folders = folders;
                    return View();
                }
                else
                {
                    _logger.LogError("Failed to retrieve container client. No details found!!");
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while fetching the list of blobs.");
                return View();
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string uploadedFileDate, IFormFile uploadedFile)
        {
            try
            {
                if (string.IsNullOrEmpty(uploadedFileDate))
                {
                    // Log error
                    _logger.LogError("No Date selected.");

                    // Handle case when no date is selected
                    ModelState.AddModelError(string.Empty, "Please select a Date to continue.");
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return StatusCode(500, string.Join(" ", errors));
                }

                if (uploadedFile == null || uploadedFile.Length == 0)
                {
                    // Log error
                    _logger.LogError("No file uploaded.");

                    // Handle case when no file is uploaded
                    ModelState.AddModelError(string.Empty, "Please select a file to upload.");
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return StatusCode(500, string.Join(" ", errors));
                }

                // Create a BlobServiceClient object which will be used to create a container client
                BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);

                // Get a reference to a container
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                // Create the container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync();

                // Create folder structure based on the uploaded file date
                DateTime date = DateTime.Parse(uploadedFileDate);
                string year = date.Year.ToString();
                string month = date.ToString("MMMM", CultureInfo.InvariantCulture);
                string folderPath = Path.Combine(year, month); // Azure Blob Storage doesn't have folders
                string fileName = Path.GetFileName(uploadedFile.FileName);
                string blobName = folderPath + "/" + fileName;

                // Get a reference to a blob
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Upload file to blob storage
                using (Stream stream = uploadedFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Log success
                _logger.LogInformation("File uploaded successfully: {FileName}", fileName);
                return Json(new { success = true, fileName = fileName });
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError(ex, "An error occurred while uploading the file.");

                // Handle exception
                ModelState.AddModelError(string.Empty, "An error occurred while uploading the file.");
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return StatusCode(500, string.Join(" ", errors));
            }
        }



        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is required.");
            }

            try
            {
                string decodedFileName = HttpUtility.UrlDecode(fileName);
                // Create a BlobServiceClient
                var blobServiceClient = new BlobServiceClient(_storageConnectionString);
                // Get a reference to the container
                var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                // Get a reference to the blob
                var blobClient = containerClient.GetBlobClient(decodedFileName);
                // Check if the blob exists
                if (!await blobClient.ExistsAsync())
                {
                    return NotFound("File not found.");
                }

                // Download the blob content to a memory stream
                var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);

                // Set the position of the memory stream to the beginning
                memoryStream.Position = 0;

                // Determine the content type based on the file extension
                var contentType = GetContentType();

                // Return the file content as a file
                return File(memoryStream.ToArray(), contentType, decodedFileName);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine(ex.Message);

                // Return an error response
                return Json(new { success = false, error = "An error occurred while downloading the file." });
            }
        }








        //[Authorize(Roles = "Fonz")]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Download(string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName))
        //    {
        //        return BadRequest("File name is required.");
        //    }

        //    try
        //    {
        //        string decodedFileName = HttpUtility.UrlDecode(fileName);
        //        // Create a BlobServiceClient
        //        var blobServiceClient = new BlobServiceClient(_storageConnectionString);
        //        // Get a reference to the container
        //        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        //        // Get a reference to the blob
        //        var blobClient = containerClient.GetBlobClient(decodedFileName);
        //        // Check if the blob exists
        //        if (!await blobClient.ExistsAsync())
        //        {
        //            return NotFound("File not found.");
        //        }

        //        // Download the blob content to a memory stream
        //        var memoryStream = new MemoryStream();
        //        await blobClient.DownloadToAsync(memoryStream);

        //        // Set the position of the memory stream to the beginning
        //        memoryStream.Position = 0;

        //        // Determine the content type based on the file extension
        //        var contentType = GetContentType();

        //        // Return the file content as JSON data
        //        return Json(new { success = true, data = Convert.ToBase64String(memoryStream.ToArray()), contentType = contentType });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        Console.WriteLine(ex.Message);

        //        // Return an error response
        //        return Json(new { success = false, error = "An error occurred while downloading the file." });
        //    }
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult OpenFolder(string folderName, string prevfolder)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                if (containerClient != null)
                {
                    var blobs = containerClient.GetBlobs();
                    var foldersInFolder = blobs
                        .Where(blob => blob.Name.Contains(folderName + "/"))
                        .Select(blob =>
                        {
                            var folderNames = blob.Name.Split('/');
                            var folder = folderNames.Length > 1 ? folderNames[1] : "";
                            return new BlobItemViewModel
                            {
                                Name = folder,
                                IsFolder = true
                            };
                        })
                        .DistinctBy(folder => folder.Name) // Ensure distinct folder names
                        .ToList();

                    // Filter out the current folder from the list of folders
                    foldersInFolder = foldersInFolder.Where(folder => !folder.Name.Equals(folderName)).ToList();

                    ViewBag.Folders = foldersInFolder;
                    if (prevfolder == null)
                    {
                      
                            prevfolder=folderName;
                        
                    }
                    else
                    {
                        prevfolder=prevfolder + "/"+folderName;
                    }
                    // Concatenate the previous folder path and the current folder name
                    ViewBag.FolderName = prevfolder;

                    if (foldersInFolder.Count == 0)
                    {
                        var filesInFolder = blobs
                            .Where(blob => blob.Name.Contains(folderName + "/"))
                            .Select(blob =>
                            {
                                var folderNames = blob.Name.Split('/');
                                var fileName = folderNames.Length > 1 ? folderNames[2] : "";
                                var filepath=Path.GetExtension(fileName);
                                return new BlobItemViewModel
                                {
                                    Name = fileName,
                                    Properties = blob.Properties,
                                    IsFolder = false,
                                    Fileextension=filepath
                                };
                            })
                            .Distinct() // Ensure uniqueness
                            .ToList();

                        ViewBag.Files = filesInFolder;
                    }

                    return View();
                }
                else
                {
                    _logger.LogError("Failed to retrieve container client. No details found!!");
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while fetching the list of blobs.");
                return View();
            }
        }



        #endregion

        #region Old code
        //[HttpGet]
        //public IActionResult Index()
        //{
        //    try
        //    {
        //        // Fetch the list of blobs from Azure Blob Storage
        //        BlobServiceClient blobServiceClient = new BlobServiceClient(_storageConnectionString);
        //        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

        //        if (containerClient != null)
        //        {
        //            var blobs = containerClient.GetBlobs();
        //            // Pass the list of blobs to the view
        //            return View(blobs);
        //        }
        //        else
        //        {
        //            _logger.LogError("Failed to retrieve container client.No details Found!!");
        //            return View();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception
        //        _logger.LogError(ex, "An error occurred while fetching the list of blobs.");
        //        return View();
        //    }
        //}



        //public async Task<IActionResult> Index(string uploadedFileDate, IFormFile uploadedFile)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(uploadedFileDate)||uploadedFileDate == null)
        //        {
        //            // Log error
        //            _logger.LogError("No Date selected.");

        //            // Handle case when no file is uploaded
        //            ModelState.AddModelError(string.Empty, "Please select a Date to continue.");
        //            // Return validation errors along with the response
        //            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //            return StatusCode(500, string.Join(" ", errors));
        //        }

        //        if (uploadedFile == null || uploadedFile.Length == 0)
        //        {
        //            // Log error
        //            _logger.LogError("No file uploaded.");

        //            // Handle case when no file is uploaded
        //            ModelState.AddModelError(string.Empty, "Please select a file to upload.");
        //            // Return validation errors along with the response
        //            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //            return StatusCode(500, string.Join(" ", errors));
        //        }

        //        // Check if the uploaded file is a ZIP file
        //        if (!IsZipFile(uploadedFile))
        //        {
        //            // Log error
        //            _logger.LogError("Uploaded file is not a ZIP file.");

        //            // Handle case when file is not a ZIP file
        //            ModelState.AddModelError(string.Empty, "Only ZIP files are allowed.");

        //            // Return validation errors along with the response
        //            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //            return StatusCode(500, string.Join(" ", errors));
        //        }

        //        // Upload the file to Azure Blob Storage
        //        BlobServiceClient blobServiceClient = new(_storageConnectionString);
        //        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        //        var fileName = Path.GetFileName(uploadedFile.FileName);
        //        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        //        using Stream fileStream = uploadedFile.OpenReadStream();
        //        var fileUploadResponse = await blobClient.UploadAsync(fileStream, true);
        //        if (fileUploadResponse != null && fileUploadResponse.GetRawResponse().Status == 201)
        //        {
        //            _logger.LogInformation("File uploaded successfully: {FileName}", fileName);
        //            return Json(new { success = true, fileName = fileName });
        //        }
        //        else
        //        {
        //            _logger.LogWarning("File failed to upload : {FileName}", fileName);
        //            return Json(new { success = false });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error
        //        _logger.LogError(ex, "An error occurred while uploading the file.");

        //        // Handle exception
        //        ModelState.AddModelError(string.Empty, "An error occurred while uploading the file.");

        //        // Return validation errors along with the response
        //        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //        return StatusCode(500, string.Join(" ", errors));
        //    }
        //}        

        [Authorize(Roles = "Fonz")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                BlobServiceClient blobServiceClient = new(_storageConnectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.DeleteIfExistsAsync();

                // Return a JSON response indicating success
                return Json(new { success = true, message = $"{fileName} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                // Handle the error 
                return StatusCode(500, ex.Message);
            }
        }
       

        #endregion
        #region PRIVATE
        // content type based on the file extension
        private static string GetContentType() => "application/octet-stream";

        // Check if the file is a ZIP file
        private static bool IsZipFile(IFormFile file)
        {
            // ZIP file MIME types
            var zipMimeTypes = new[] { "application/zip", "application/x-zip-compressed", "application/octet-stream" };

            // Check if the file's MIME type matches any of the ZIP file MIME types
            return zipMimeTypes.Contains(file.ContentType.ToLower());
        }
        #endregion
    }
}