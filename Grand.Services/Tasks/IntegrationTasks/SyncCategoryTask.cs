using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Core.Caching;
using Grand.Core.Data;
using Grand.Core.Data.IntegrationData;
using Grand.Core.Domain.Catalog;
using Grand.Core.Domain.Localization;
using Grand.Core.Domain.Logging;
using Grand.Core.Domain.Stores;
using Grand.Services.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Services.Tasks.IntegrationTasks
{
    public class SyncCategoryTask : IScheduleTask
    {
        private const string CATEGORIES_PATTERN_KEY = "Grand.category.";
        private const string PRODUCTCATEGORIES_PATTERN_KEY = "Grand.productcategory.";
        private const string PRODUCTS_PATTERN_KEY = "Grand.product.";
        private const string PRODUCTS_BY_ID_KEY = "Grand.product.id-{0}";
        
        private readonly ILogger _logger;
        private readonly ISqlRepository<Grand.Core.Domain.IntegrationData.ProductCategory> _productCategoryRepository;
        private readonly IRepository<Grand.Core.Domain.Catalog.Category> _categoryMongoRepository;
        private readonly IRepository<Grand.Core.Domain.Catalog.Product> _productMongoRepository;
        private readonly IRepository<Grand.Core.Domain.Stores.Store> _storeMongoRepository;
        private readonly ICacheManager _cacheManager;

        public SyncCategoryTask(ILogger logger, ISqlRepository<Core.Domain.IntegrationData.ProductCategory> productCategoryRepository, IRepository<Category> categoryMongoRepository, IRepository<Product> productMongoRepository, IRepository<Store> storeMongoRepository, ICacheManager cacheManager)
        {
            _logger = logger;
            _productCategoryRepository = productCategoryRepository;
            _categoryMongoRepository = categoryMongoRepository;
            _productMongoRepository = productMongoRepository;
            _storeMongoRepository = storeMongoRepository;
            _cacheManager = cacheManager;
        }

        public async Task Execute()
        {
            try
            {
                var erpData = await _productCategoryRepository.GetAll();
                await SyncCategories(erpData);
                await SyncProductCategories(erpData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task SyncCategories(IEnumerable<Grand.Core.Domain.IntegrationData.ProductCategory> erpData)
        {
            try
            {
                var storeId = await (from s in _storeMongoRepository.Table select new {s.Id}).ToListAsync();
                var erpCategories = erpData.Select(c => new {c.Id}).Distinct();
                var shopData = await (from p in _categoryMongoRepository.Table select new {p.SeName, p.Name}).ToListAsync();

                var insertData = (from e in erpCategories
                    join c in shopData on e.Id equals c.SeName into dc
                    from dcg in dc.DefaultIfEmpty()
                    where dcg == null && e.Id.Length > 0
                    select new Category {
                        CustomerRoles = new List<string>(),
                        Locales = new List<LocalizedProperty>(),
                        Stores = storeId.Select(d => d.Id).ToList(),
                        Name = e.Id,
                        SeName = e.Id,
                        Published = true,
                        ShowOnHomePage = false
                    }).ToList();
                
                await _categoryMongoRepository.InsertAsync(insertData);
                await _cacheManager.RemoveByPattern(CATEGORIES_PATTERN_KEY);
                await _cacheManager.RemoveByPattern(PRODUCTCATEGORIES_PATTERN_KEY);
                await _cacheManager.RemoveByPattern(PRODUCTS_PATTERN_KEY);

                await _logger.InsertLog(LogLevel.Information,
                    $"Number of categories inserted: {insertData.Count}");
            }
            catch (Exception ex)
            {
                await _logger.InsertLog(LogLevel.Error, "Error syncing categories from Erp.", ex.ToString());
            }
        }

        private async Task SyncProductCategories(IEnumerable<Grand.Core.Domain.IntegrationData.ProductCategory> erpData)
        {
            try
            {
                var shopProductData = await (from p in _productMongoRepository.Table select new {p.Id, p.Sku}).ToListAsync();
                var shopCategoryData = await (from p in _categoryMongoRepository.Table select new {p.SeName, p.Id}).ToListAsync();

                var insertData = (from e in erpData
                    join p in shopProductData on e.ProductId equals p.Sku
                    join c in shopCategoryData on e.Id equals c.SeName
                    select new Grand.Core.Domain.Catalog.ProductCategory {
                        ProductId = p.Id,
                        CategoryId = c.Id,
                        IsFeaturedProduct = false,
                        DisplayOrder = 1
                    }).ToList();

                foreach (var item in insertData)
                {
                    var updatebuilder = Builders<Grand.Core.Domain.Catalog.Product>.Update;
                    var update = updatebuilder.AddToSet(p => p.ProductCategories, item);
                    await _productMongoRepository.Collection.UpdateOneAsync(new BsonDocument("_id", item.ProductId), update);
                
                    await _cacheManager.RemoveByPattern(CATEGORIES_PATTERN_KEY);
                    await _cacheManager.RemoveByPattern(PRODUCTCATEGORIES_PATTERN_KEY);
                    await _cacheManager.RemoveByPattern(string.Format(PRODUCTS_BY_ID_KEY, item.ProductId));
                }
                
                await _logger.InsertLog(LogLevel.Information,
                    $"Number of product category associations inserted: {insertData.Count}");
            }
            catch (Exception ex)
            {
                await _logger.InsertLog(LogLevel.Error, "Error syncing product category association from Erp.", ex.ToString());
            }
        }
    }
}