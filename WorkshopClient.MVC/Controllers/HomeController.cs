using System.Linq;
using Microsoft.Azure;

namespace WorkshopClient.MVC.Controllers
{
    using System;
    using System.Web.Mvc;
    using System.Threading.Tasks;
    using System.IO;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <inheritdoc />
    public class HomeController : Controller
    {
        static CloudBlobClient _blobClient;
        private const string BlobContainerName = 
            "azureintroduction-imagecontainer";
        static CloudBlobContainer _blobContainer;

        public async Task<ActionResult> Index()
        {
            try
            {
                var storageAccount = 
                    CloudStorageAccount
                    .Parse
                    (
                        CloudConfigurationManager
                        .GetSetting("StorageConnectionString")
                    );

                _blobClient = storageAccount.CreateCloudBlobClient();
                _blobContainer = 
                    _blobClient
                    .GetContainerReference(BlobContainerName);

                await _blobContainer.CreateIfNotExistsAsync();

                await 
                    _blobContainer
                    .SetPermissionsAsync
                    (
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        }
                    );

                var allBlobs =
                    _blobContainer
                        .ListBlobs()
                        .Where(blob => blob.GetType() == typeof(CloudBlockBlob))
                        .Select(blob => blob.Uri)
                        .ToList();

                return View(allBlobs);
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> UploadAsync()
        {
            try
            {
                var files = Request.Files;
                var fileCount = files.Count;

                if (fileCount <= 0) return RedirectToAction("Index");

                for (var i = 0; i < fileCount; i++)
                {
                    var file = files[i];
                    if (file?.InputStream == null || file.FileName == null)
                        continue;

                    var blob =
                        _blobContainer
                        .GetBlockBlobReference(GetRandomBlobName(file.FileName));

                    await blob.UploadFromStreamAsync(file.InputStream);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteImage(string name)
        {
            try
            {
                var uri = new Uri(name);
                var filename = Path.GetFileName(uri.LocalPath);

                var blob = _blobContainer.GetBlockBlobReference(filename);
                await blob.DeleteIfExistsAsync();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                foreach (var blob in _blobContainer.ListBlobs())
                {
                    if (blob.GetType() == typeof(CloudBlockBlob))
                    {
                        await ((CloudBlockBlob)blob).DeleteIfExistsAsync();
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewData["message"] = ex.Message;
                ViewData["trace"] = ex.StackTrace;
                return View("Error");
            }
        }

        private static string GetRandomBlobName(string filename)
        {
            var ext = Path.GetExtension(filename);
            return $"{DateTime.Now.Ticks:10}_{Guid.NewGuid()}{ext}";
        }
    }
}