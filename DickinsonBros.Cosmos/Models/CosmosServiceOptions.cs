using System.Diagnostics.CodeAnalysis;

namespace DickinsonBros.Cosmos.Models
{
    [ExcludeFromCodeCoverage]
    public class CosmosServiceOptions
    {
        public string EndpointUri { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public string ConnectionString { get; set; }
    }
}
