using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        // Navigation property for the junction table
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // Navigation property for users assigned to this role
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}