using System.ComponentModel.DataAnnotations.Schema;

namespace HRMgmt.Models
{
    [Table("Employees")]
    public class Employee : User
    {
        public Service? MyService { get; set; }
        public decimal Salary { get; set; }
        public int? ServiceId { get; set; }
        public string Role { get; set; }
        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; }

        public Employee()
        {
            ShiftAssignments = new HashSet<ShiftAssignment>();
        }

        public ICollection<EmployeeShift>? EmployeeShifts { get; set; }
    }
}
