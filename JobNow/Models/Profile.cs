using Postgrest.Attributes;
using Postgrest.Models;

namespace JobNow.Models
{
    [Table("jn_profiles")]
    public class Profile : BaseModel
    {
        // Đổi chữ false thành true để cho phép C# gửi ID này lên Supabase
        [PrimaryKey("id", true)]
        public string Id { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("role")]
        public string Role { get; set; }
    }
}