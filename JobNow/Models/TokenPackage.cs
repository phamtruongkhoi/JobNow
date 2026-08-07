using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_token_packages")]
    public class TokenPackage : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("package_name")]
        public string PackageName { get; set; }

        [Column("token_amount")]
        public int TokenAmount { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}
