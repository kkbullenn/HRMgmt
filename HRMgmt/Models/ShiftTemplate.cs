using System.ComponentModel.DataAnnotations;

namespace HRMgmt.Models
{
    public class ShiftTemplate
    {
        [Key]
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public Guid EmployeeId { get; set; }
        public int WeekType { get; set; }
        public int WeekIndex { get; set; }
        public string DayOfWeek { get; set; }
        public string ShiftType { get; set; }
    }
}
