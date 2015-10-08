using System;
using Newtonsoft.Json;

namespace AzureApiHelpers.Repositories
{
    public interface IDocumentBase
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }
        string Type { get; }
    }
}