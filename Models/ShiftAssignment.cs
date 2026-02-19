using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("ShiftAssignments")]
    public class ShiftAssignment
    {
        [Key]
        public int Id { get; set; }     
        
        [Required]
        public Guid ShiftId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateOnly ShiftDate { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ShiftId))]
        public Shift? Shift { get; set; }
    }
}
