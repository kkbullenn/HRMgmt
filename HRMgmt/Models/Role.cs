using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Roles")]
    public class Role
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
    }
}