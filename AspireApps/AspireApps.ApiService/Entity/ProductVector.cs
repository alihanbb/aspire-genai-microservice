using Microsoft.Extensions.VectorData;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspireApps.CatalogService.Entity
{
    public class ProductVector
    {
        [VectorStoreKey]
        public ulong Id { get; set; }
        [VectorStoreData]
        public string Name { get; set; } = default!;
        [VectorStoreData]
        public string Description { get; set; } = default!;
        [VectorStoreData]
        public double Price { get; set; }
        [VectorStoreData]
        public string ImageUrl { get; set; } = default!;

        [NotMapped]
        [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> Vector { get; set; }
    }
}
