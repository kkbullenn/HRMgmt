using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("SchedulingTemplates")]
    public class SchedulingTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public int WeekType { get; set; }

        [Required]
        public int WeekIndex { get; set; }

        [Required]
        [MaxLength(20)]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ShiftType { get; set; } = string.Empty;
    }
}
