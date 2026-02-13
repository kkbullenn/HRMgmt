using HRMgmt.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Shifts")]
    public class Shift
    {
        [Key]
        public Guid ID { get; set; }

        [Required]
        public string Name { get; set; }

        public int RequiredCount { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; } // 08:00

        [Required]
        public TimeSpan EndTime { get; set; } // 16:00

        [Required]
        public DateTime StartDate { get; set; } 

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public RecurrenceType RecurrenceType { get; set; }

        public string RecurrenceDays { get; set; }
        public int Interval { get; set; }

        public ICollection<EmployeeShift>? EmployeeShifts { get; set; }
        
    }

    public enum RecurrenceType
    {
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Yearly
    }

    public class EmployeeShift
    {
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public Guid ShiftId { get; set; }
        public Shift Shift { get; set; }

        [Required]
        public DateTime Date { get; set; }
    }
    
}
