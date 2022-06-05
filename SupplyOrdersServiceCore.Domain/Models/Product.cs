using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Domain.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id_product")]
        public long Id { get; set; }

        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("central_ident_number")]
        [MaxLength(6)]
        public string CentralIdentNumber { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("processed_quantity")]
        public int ProcessedQuantity { get; set; }

        [ForeignKey("order_fk")]
        public Order Order { get; set; }
    }
}
