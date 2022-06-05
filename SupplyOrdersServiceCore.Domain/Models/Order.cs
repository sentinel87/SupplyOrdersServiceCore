using SupplyOrdersServiceCore.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Domain.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("id_order")]
        public long Id { get; set; }

        [Column("order_symbol")]
        [MaxLength(20)]
        public string OrderSymbol { get; set; }

        [Column("client_company_id")]
        public int ClientCompanyId { get; set; }

        [Column("status")]
        public OrderStatus Status { get; set; }

        [Column("order_file")]
        [MaxLength(20)]
        public string OrderFile { get; set; }

        [Column("response_file")]
        [MaxLength(20)]
        public string ResponseFile { get; set; }

        [Column("creation_date")]
        public DateTime? CreationDate { get; set; }

        [Column("modification_date")]
        public DateTime? ModificationDate { get; set; }

        [Column("ftp_status")]
        public FtpStatus FtpStatus { get; set; }

        [Column("comment")]
        [MaxLength(200)]
        public string Comment { get; set; }

        [Column("wholesaler")]
        public int Wholesaler { get; set; }

        [Column("ftp_file")]
        [MaxLength(20)]
        public string FtpFile { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
