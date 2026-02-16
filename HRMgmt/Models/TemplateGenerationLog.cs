using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("TemplateGenerationLogs")]
    public class TemplateGenerationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public int WeekType { get; set; } // 1=Weekly, 2=Biweekly

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        public DateTime GeneratedAt { get; set; }

        [Required]
        public int GeneratedCount { get; set; }
    }
}
