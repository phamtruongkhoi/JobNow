using Postgrest.Attributes;
using Postgrest.Models;
//using Supabase.Postgrest.Attributes;
//using Supabase.Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_locations")]
    public class JobLocation : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; } // Link ảnh nền thành phố
    }
}