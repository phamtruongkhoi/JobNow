using Postgrest.Attributes;
using Postgrest.Models;
//using Supabase.Postgrest.Attributes;
//using Supabase.Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_articles")]
    public class Article : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("category")]
        public string Category { get; set; } // Ví dụ: Kinh nghiệm phỏng vấn

        [Column("title")]
        public string Title { get; set; }

        [Column("summary")]
        public string Summary { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; }
    }
}