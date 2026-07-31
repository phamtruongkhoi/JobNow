using Postgrest.Attributes;
using Postgrest.Models;
//using Supabase.Postgrest.Attributes;
//using Supabase.Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_employers")]
    public class Employer : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("logo_url")]
        public string LogoUrl { get; set; }

        [Column("is_global")]
        public bool IsGlobal { get; set; } // true: Quốc tế, false: Việt Nam

        [Column("website")]
        public string Website { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("company_size")]
        public string CompanySize { get; set; }
    }
}