using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [RegularExpression(@"^[a-zA-Z''-'\s]{1,20}$",
            ErrorMessage = "Numbers are not allowed.")]
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public int Role { get; set; }

        public byte[]? Photo { get; set; }

        public decimal? HourlyWage { get; set; }

        public ICollection<ShiftAssignment> ShiftAssignments { get; set; } = new List<ShiftAssignment>();

    }
}
