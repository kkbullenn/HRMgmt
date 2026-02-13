using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Payrolls")]
    public class Payroll
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [Required]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        public DateTime PayPeriodEnd { get; set; }

        [Required]
        public decimal RegularHours { get; set; }

        [Required]
        public decimal OvertimeHours { get; set; }

        [Required]
        public decimal SickHours { get; set; }

        [Required]
        public decimal TotalHours { get; set; }

        [Required]
        public decimal GrossPay { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }
    }
}
