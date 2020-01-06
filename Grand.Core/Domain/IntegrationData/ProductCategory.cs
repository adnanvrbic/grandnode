using System.ComponentModel.DataAnnotations.Schema;

namespace Grand.Core.Domain.IntegrationData
{
    [Table("_vHE_ARTIKAL_GRUPE")]
    public class ProductCategory
    {
        [Column("Naziv")]
        public string Id { get; set; }

        [Column("sifra")]
        public string ProductId { get; set; }
    }
}