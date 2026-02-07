using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HRMgmt.Models
{
    public class Service
    {
        public Service()
        {
            Employees = new HashSet<Employee>();
        }

        public int Id { get; set; }
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,20}$", ErrorMessage = "Numbers are not allowed.")]
        public string Name { get; set; }
        [Range(0.01, 999999.99, ErrorMessage = "Rate cannot be negative")]
        public decimal Rate { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
    }

}
