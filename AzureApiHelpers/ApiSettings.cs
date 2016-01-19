using System.Drawing;

namespace AzureApiHelpers
{
    public class DbSettings
    {
        public string EndPointUrl { get; set; }
        public string AuthorizationKey { get; set; }
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
        public string OfferType { get; set; }
    }

    public class CDNSettings
    {
        public string StorageAccountName { get; set; }
        public string StorageAccountAccessKey { get; set; }
        /// <summary>
        /// Used to resize the photo, if null the image will be note risized
        /// </summary>
        public Size ImageSize { get; set; }

        /// <summary>
        /// If true will upload a 100x100 thumbail in the container 'thumb'
        /// </summary>
        public bool UploadThumbnail { get; set; }
    }

    public class AzureData
    {
        public DbSettings DbSettings { get; set; }
        public CDNSettings CDNSettings { get; set; }
    }
}
