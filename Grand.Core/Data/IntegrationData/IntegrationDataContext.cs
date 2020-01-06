using Grand.Core.Domain.IntegrationData;
using Microsoft.EntityFrameworkCore;

namespace Grand.Core.Data.IntegrationData
{
    public class IntegrationDataContext : DbContext
    {
        public IntegrationDataContext()
        {
        }
        
        public IntegrationDataContext(DbContextOptions options) : base(options) {}
        
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
    }
}