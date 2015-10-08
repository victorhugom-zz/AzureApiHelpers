using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;
using System.Drawing;

namespace AzureApiHelpers
{
    public class CDN
    {
        private static string StorageAccountName;
        private static string StorageAccountAccessKey;
        private static string[] AllowedExtensions = new string[] { ".jpg", ".png", ".gif" };
        private static ISupportedImageFormat format = new JpegFormat { Quality = 100 };
        private static ResizeLayer ResizeLayer = new ResizeLayer(new Size(1920, 1080), ResizeMode.Min);

        private CloudStorageAccount storageAccount;
        public CloudStorageAccount StorageAccount
        {
            get
            {
                if (storageAccount == null)
                {
                    string account = StorageAccountName;
                    string key = StorageAccountAccessKey;
                    string connectionString = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", account, key);
                    storageAccount = CloudStorageAccount.Parse(connectionString);
                }
                return storageAccount;
            }
        }
        public CloudBlobClient BlobClient { get; private set; }
        public CloudBlobContainer Container { get; private set; }

        public CDN(CDNSettings appSettings)
        {
            try
            {
                StorageAccountName = appSettings.StorageAccountName;
                StorageAccountAccessKey = appSettings.StorageAccountAccessKey;

                // Create a blob client and retrieve reference to images container
                BlobClient = StorageAccount.CreateCloudBlobClient();
                Container = BlobClient.GetContainerReference("images");

                // Create the "images" container if it doesn't already exist.
                if (Container.CreateIfNotExists())
                {
                    // Enable public access on the newly created "images" container
                    Container.SetPermissions(
                        new BlobContainerPermissions
                        {
                            PublicAccess =
                                BlobContainerPublicAccessType.Blob
                        });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task<string> UploadPhotoAsync(IFormFile photoToUpload)
        {
            string fullPath = null;

            if (photoToUpload == null || photoToUpload.Length == 0)
                return null;

            var parsedContentDisposition = ContentDispositionHeaderValue.Parse(photoToUpload.ContentDisposition);
            string filename = parsedContentDisposition.FileName.Trim('"');

            //check extension
            if (!FileExtensionIsValid(filename))
                return null;

            //check file size and upload
            if (!FileSizeIsValid(photoToUpload))
                return null;

            try
            {
                // Create a unique name for the images we are about to upload
                string imageName = $"photo-{Guid.NewGuid().ToString()}.jpg";

                // Upload image to Blob Storage
                CloudBlockBlob blockBlob = Container.GetBlockBlobReference(imageName);
                blockBlob.Properties.ContentType = "image/jpeg";

                //resize image and up
                using (MemoryStream outStream = new MemoryStream())
                {
                    // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                    using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                    {
                        // Load, resize, set the format and quality and save an image.
                        imageFactory.Load(photoToUpload.OpenReadStream())
                                    .Resize(ResizeLayer)
                                    .Format(format)
                                    .AutoRotate()
                                    .Save(outStream);
                    }

                    await blockBlob.UploadFromStreamAsync(outStream);
                }

                // Convert to be HTTP based URI (default storage path is HTTPS)
                var uriBuilder = new UriBuilder(blockBlob.Uri);
                uriBuilder.Scheme = "https";
                fullPath = uriBuilder.ToString();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return fullPath;
        }

        private bool FileExtensionIsValid(string path)
        {
            return AllowedExtensions.Contains(Path.GetExtension(path));
        }

        private bool FileSizeIsValid(IFormFile file)
        {
            double fileSize = 0;
            using (var reader = file.OpenReadStream())
            {
                //get filesize in kb
                fileSize = (reader.Length / 1024);
            }

            //filesize less than 5MB => true, else => false
            return (fileSize < 1024 * 5) ? true : false;
        }
    }
}
