using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace JobNow.Models
{
    [Table("jn_saved_courses")]
    public class SavedCourse : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("profile_id")]
        public string ProfileId { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // [Reference(typeof(Course), ReferenceAttribute.JoinType.Inner)]
        // public Course Course { get; set; }
    }
}
