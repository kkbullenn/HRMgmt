using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Required]
        public int RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        [Required]
        public int PermissionId { get; set; }
        [ForeignKey(nameof(PermissionId))]
        public Permission? Permission { get; set; }
    }
}