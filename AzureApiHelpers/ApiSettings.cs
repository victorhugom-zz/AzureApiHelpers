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
    }

    public class AzureData
    {
        public DbSettings DbSettings { get; set; }
        public CDNSettings CDNSettings { get; set; }
    }
}
