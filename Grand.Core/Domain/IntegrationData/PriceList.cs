using System.ComponentModel.DataAnnotations.Schema;

namespace Grand.Core.Domain.IntegrationData
{
    [Table("_vHE_ARTIKLI")]
    public class PriceList
    {
        [Column("sifra")]
        public string Id { get; set; }
        [Column("VPC")]
        public double VPC { get; set; }
        [Column("MPC")]
        public decimal MPC { get; set; }
    }
}