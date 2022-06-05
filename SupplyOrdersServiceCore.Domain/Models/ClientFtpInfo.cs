using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyOrdersServiceCore.Domain.Models
{
    [Table("client_ftp_info")]
    public class ClientFtpInfo
    {
        [Key]
        [Column("id_client_ftp_info")]
        public int Id { get; set; }

        [Column("client_company_id")]
        public int ClientCompanyId { get; set; }

        [Column("ftp_directory")]
        [MaxLength(100)]
        public string FtpDirectory { get; set; }
    }
}
