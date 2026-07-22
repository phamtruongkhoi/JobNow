using Postgrest.Attributes;
using Postgrest.Models;
//using Supabase.Postgrest.Attributes;
//using Supabase.Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_industries")]
    public class Industry : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("icon_url")]
        public string IconUrl { get; set; } // Link ảnh icon (Ví dụ: icon IT, Marketing)

        [Column("job_count")]
        public int JobCount { get; set; }
    }
}