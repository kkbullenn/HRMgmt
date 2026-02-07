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
        public enum ShiftType
        {
            Day,
            Evening
        }

        [Key]
        public Guid ShiftID { get; set; }

        [Required]
        public DateOnly ShiftStart { get; set; }

        public DateOnly? ShiftEnd { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [Required]
        public ShiftType TypeOfShift { get; set; }

        public string RecurrenceDays { get; set; }

        public int? ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; }

        public Shift()
        {
            ShiftAssignments = new HashSet<ShiftAssignment>();
        }
    }
}
