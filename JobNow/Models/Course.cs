using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_courses")]
    public class Course : BaseModel
    {
        [PrimaryKey("id", false)] public int Id { get; set; }
        [Column("title")] public string Title { get; set; }
        [Column("provider_name")] public string ProviderName { get; set; }
        [Column("provider_logo")] public string ProviderLogo { get; set; }
        [Column("price")] public string Price { get; set; } // Tớ lưu giá dạng số chuỗi để dễ lọc
        [Column("location")] public string Location { get; set; }
        [Column("posted_at")] public string PostedAt { get; set; }
        [Column("deadline")] public DateTime Deadline { get; set; }
        [Column("tags")] public string Tags { get; set; }
        [Column("is_hot")] public bool IsHot { get; set; }
        [Column("is_new")] public bool IsNew { get; set; }
        [Column("duration")] public string Duration { get; set; }
        [Column("format")] public string Format { get; set; }
    }
}