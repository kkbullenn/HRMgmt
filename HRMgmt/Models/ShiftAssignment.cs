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
        public Guid ShiftID { get; set; }
        [Required]
        public Guid EmployeeId { get; set; }
        
        public int? ServiceId { get; set; }
        
        [Required]
        public DateOnly AssignmentDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [ForeignKey("ShiftID")]
        public virtual Shift? Shift { get; set; }
        
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; } 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
