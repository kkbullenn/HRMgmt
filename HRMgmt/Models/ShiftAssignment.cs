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
        
    }
}
