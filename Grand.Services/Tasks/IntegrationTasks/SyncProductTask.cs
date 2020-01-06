using System;
using System.Linq;
using System.Threading.Tasks;
using Grand.Core.Caching;
using Grand.Core.Data;
using Grand.Core.Data.IntegrationData;
using Grand.Core.Domain.IntegrationData;
using Grand.Core.Domain.Logging;
using Grand.Core.Domain.Stores;
using Grand.Services.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Services.Tasks.IntegrationTasks
{
    public class SyncProductTask : IScheduleTask
    {
        private const string PRODUCTS_PATTERN_KEY = "Grand.product.";
        
        private readonly ILogger _logger;
        private readonly ISqlRepository<Product> _productRepository;
        private readonly ISqlRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<Grand.Core.Domain.Catalog.Product> _productMongoRepository;
        private readonly IRepository<Grand.Core.Domain.Stores.Store> _storeMongoRepository;
        private readonly ICacheManager _cacheManager;

        public SyncProductTask(ILogger logger, ISqlRepository<Product> productRepository, ISqlRepository<ProductCategory> productCategoryRepository, IRepository<Core.Domain.Catalog.Product> productMongoRepository, IRepository<Store> storeMongoRepository, ICacheManager cacheManager)
        {
            _logger = logger;
            _productRepository = productRepository;
            _productCategoryRepository = productCategoryRepository;
            _productMongoRepository = productMongoRepository;
            _storeMongoRepository = storeMongoRepository;
            _cacheManager = cacheManager;
        }

        public async Task Execute()
        {
            try
            {
                var storeId = await (from s in _storeMongoRepository.Table select new {s.Id}).ToListAsync();
                
                var erpData = await _productRepository.GetAll();
                var productQuery = from p in _productMongoRepository.Table select new {p.Sku, p.Name};
                var shopData = await productQuery.ToListAsync();
                
                var insertData = (from e in erpData
                    join c in shopData on e.Id equals c.Sku into dc
                    from dcg in dc.DefaultIfEmpty()
                    where dcg == null && e.Id.Length > 0
                    select e).ToList();

                var updateData = (from e in erpData
                    join c in shopData on e.Id equals c.Sku
                    where e.Name != c.Name
                    select c).ToList();

                await _productMongoRepository.InsertAsync(Product.ConvertToMongoEntity(insertData));
                //await _productMongoRepository.UpdateAsync(Product.ConvertToMongoEntity(updateData));
                await _cacheManager.RemoveByPattern(PRODUCTS_PATTERN_KEY);

                await _logger.InsertLog(LogLevel.Information,
                    $"Number of product inserted: {insertData.Count}, updated: {updateData.Count}");
            }
            catch (Exception ex)
            {
                await _logger.InsertLog(LogLevel.Error, "Error syncing product from Erp.", ex.ToString());
            }
        }
    }
}