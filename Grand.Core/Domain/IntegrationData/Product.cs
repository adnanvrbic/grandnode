using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Grand.Core.Domain.IntegrationData
{
    [Table("_vHE_ARTIKLI")]
    public class Product
    {
        [Column("sifra")]
        public string Id { get; set; }
        [Column("Naziv")]
        public string Name { get; set; }
        [Column("BarKod")]
        public string Barcode { get; set; }
        [Column("Opis")]
        public string Description { get; set; }
        [Column("VPC")]
        public double VPC { get; set; }
        [Column("MPC")]
        public decimal MPC { get; set; }
        [Column("Zaliha")]
        public decimal Stock { get; set; }
        public static IEnumerable<Grand.Core.Domain.Catalog.Product> ConvertToMongoEntity(IEnumerable<Product> products)
        {
            return products.Select(item => new Grand.Core.Domain.Catalog.Product(item));
        }
    }
}