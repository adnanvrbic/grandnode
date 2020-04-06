using System.Linq;
using System.Threading.Tasks;
using Grand.Core.Caching;
using Grand.Core.Data;
using Grand.Core.Data.IntegrationData;
using Grand.Core.Domain.Catalog;
using Grand.Core.Domain.Shipping;
using Grand.Services.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Services.Tasks.IntegrationTasks
{
    public class SyncStockTask : IScheduleTask
    {
        private const string PRODUCTS_PATTERN_KEY = "Grand.product.";
        
        private readonly ILogger _logger;
        private readonly ISqlRepository<Product> _productRepository;
        private readonly ISqlRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<Grand.Core.Domain.Catalog.Product> _productMongoRepository;
        private readonly IRepository<Warehouse> _warehouseMongoRepository;
        private readonly ICacheManager _cacheManager;
        
        
        public async Task Execute()
        {
            var erpData = await _productRepository.GetAll();
            var shopData = await (from p in _productMongoRepository.Table select new { p.Sku, p.ProductWarehouseInventory }).ToListAsync();
//
//            foreach (var item in shopData)
//            {
//                
//            }
//            
//            
//            var insertData = (from e in erpData
//                join c in shopData on e.Id equals c.Sku into dc
//                from dcg in dc.DefaultIfEmpty()
//                where dcg == null && e.Id.Length > 0
//                select e).ToList();
//
//            var updateData = (from e in erpData
//                join c in shopData on e.Id equals c.Sku
//                where e.Name != c.Name
//                select c).ToList();
//
//            var updatebuilder = Builders<Product>.Update;
//            var update = updatebuilder.AddToSet(p => p.ProductWarehouseInventory, pwi);
//            await _productMongoRepository.Collection.UpdateOneAsync(new BsonDocument("_id", pwi.ProductId), update);

        }
    }
}