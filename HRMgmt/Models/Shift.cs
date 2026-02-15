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
        public Guid ShiftId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int RequiredCount { get; set; }

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

        public ICollection<ShiftAssignment>? ShiftAssignment { get; set; }
        
    }

    public enum RecurrenceType
    {
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Yearly
    }

    // public class EmployeeShift
    // {
    //     [Key]
    //     public Guid EmployeeShiftId { get; set; }


    //     [Required]
    //     public Guid ShiftId { get; set; }

    //     [Required]
    //     public Guid UserId { get; set; }

    //     [Required]
    //     public DateTime Date { get; set; }
    // }
    
}