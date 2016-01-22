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

    public class BlobSettings
    {
        public BlobSettings()
        {
            ContainerName = "images";
        }

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

        /// <summary>
        /// Default container name: images
        /// </summary>
        public string ContainerName { get; set; }
    }

    public class BusQueueSettings
    {
        public BusQueueSettings()
        {
            QueueName = "default";
        }

        public string ConnectionString { get;  set; }
        /// <summary>
        ///Default queue name: default
        /// </summary>
        public string QueueName { get; set; }
    }

    public class AzureData
    {
        public DbSettings DbSettings { get; set; }
        public BlobSettings BlobSettings { get; set; }
        public BusQueueSettings BusQueueSettings { get; set; }
    }
}
