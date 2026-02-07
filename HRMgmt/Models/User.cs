using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("User")]
    public class User
    {
        public Guid ID { get; set; }
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,20}$",
        ErrorMessage = "Numbers are not allowed.")]
        public string Name { get; set; }
        public string Address { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public byte[]? Photo { get; set; }

    }
}
