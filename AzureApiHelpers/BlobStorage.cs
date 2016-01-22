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
    public class BlobStorage
    {
        private static string StorageAccountName;
        private static string StorageAccountAccessKey;
        private static string[] AllowedExtensions = new string[] { ".jpg", ".png", ".gif", ".jpeg" };
        private static ISupportedImageFormat format = new JpegFormat { Quality = 100 };
        private ResizeLayer ResizeLayer = null;
        private static int ImageSizeInMb = 10;
        private bool UploadThumbnail = false;

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

        public BlobStorage(BlobSettings appSettings)
        {
            try
            {
                if (appSettings.ImageSize != null)
                    ResizeLayer = new ResizeLayer(appSettings.ImageSize, ResizeMode.Min);

                UploadThumbnail = appSettings.UploadThumbnail;

                StorageAccountName = appSettings.StorageAccountName;
                StorageAccountAccessKey = appSettings.StorageAccountAccessKey;

                // Create a blob client and retrieve reference to images container
                BlobClient = StorageAccount.CreateCloudBlobClient();
                Container = BlobClient.GetContainerReference(appSettings.ContainerName);

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

        /// <summary>
        /// Upload a photo to the Blob azure storage service
        /// </summary>
        /// <param name="photoToUpload"></param>
        /// <param name="fileName">Optional. Will use a random GUID if no filename defined</param>
        /// <returns></returns>
        public async Task<string> UploadPhotoAsync(Stream fileStream, string fileName = "")
        {
            if (fileStream == null || fileStream.Length == 0)
                return null;

            fileName = string.IsNullOrEmpty(fileName) ? Guid.NewGuid().ToString() : fileName;
            string fullPath = null;

            try
            {
                // Create a unique name for the images we are about to upload
                string imageName = $"photo-{fileName}.jpg";

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
                        var imageToPrepare = imageFactory.Load(fileStream)
                                        .Format(format)
                                        .AutoRotate();

                        // Resize image
                        if (ResizeLayer != null)
                            imageToPrepare.Resize(ResizeLayer);

                        imageToPrepare.Save(outStream);

                    }

                    await blockBlob.UploadFromStreamAsync(outStream);
                }

                // Convert to be HTTP based URI (default storage path is HTTPS)
                var uriBuilder = new UriBuilder(blockBlob.Uri);
                uriBuilder.Scheme = "https";
                fullPath = uriBuilder.ToString();

                await UploadThumbnailToBlob(fileStream, imageName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return fullPath;
        }

        private async Task UploadThumbnailToBlob(Stream fileStream, string imageName)
        {
            //set imageNameFolder
            imageName = $"thumb/{imageName}";

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
                    var imageToPrepare = imageFactory.Load(fileStream)
                                    .Format(format)
                                    .Resize(new ResizeLayer(resizeMode: ResizeMode.Min, size: new Size(100, 100)))
                                    .AutoRotate();

                    // Resize image
                    if (ResizeLayer != null)
                        imageToPrepare.Resize(ResizeLayer);

                    imageToPrepare.Save(outStream);
                }

                await blockBlob.UploadFromStreamAsync(outStream);
            }
        }

        /// <summary>
        /// Upload a photo to the Azure Blob Storage Service
        /// </summary>
        /// <param name="photoToUpload"></param>
        /// <param name="fileName">Optional. Will use a random GUID if no filename defined</param>
        /// <returns></returns>
        public async Task<string> UploadPhotoAsync(IFormFile photoToUpload, string fileName = "")
        {
            if (photoToUpload == null || photoToUpload.Length == 0)
                return null;

            var parsedContentDisposition = ContentDispositionHeaderValue.Parse(photoToUpload.ContentDisposition);

            //check file size and upload
            if (!FileSizeIsValid(photoToUpload))
                return null;

            using (var photoStream = photoToUpload.OpenReadStream())
            {
                return await UploadPhotoAsync(photoStream, fileName);
            }
        }

        /// <summary>
        /// Upload a photo to the Azure Blob Storage Service
        /// </summary>
        /// <param name="photoToUpload"></param>
        /// <param name="fileName">Optional. Will use a random GUID if no filename defined</param>
        /// <returns></returns>
        public async Task<string> UploadPhotoAsync(string base64File, string fileName = "")
        {
            if (string.IsNullOrEmpty(base64File)) return null;

            byte[] imageBytes = Convert.FromBase64String(base64File);
            using (Stream fileStream = new MemoryStream(imageBytes))
            {
                return await UploadPhotoAsync(fileStream);
            }
        }

        private bool FileExtensionIsValid(string path)
        {
            return AllowedExtensions.Contains(Path.GetExtension(path));
        }

        private bool FileSizeIsValid(IFormFile file)
        {
            double fileSize = 0;
            //get filesize in kb
            fileSize = (file.Length / 1024);

            //filesize less than 5MB => true, else => false
            return (fileSize < 1024 * ImageSizeInMb) ? true : false;
        }

        private bool FileSizeIsValid(Stream streamFile)
        {
            double fileSize = 0;
            //get filesize in kb
            fileSize = (streamFile.Length / 1024);

            //filesize less than 5MB => true, else => false
            return (fileSize < 1024 * ImageSizeInMb) ? true : false;
        }
    }
}
