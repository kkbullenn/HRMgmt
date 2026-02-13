using System.Collections.Generic;
using HRMgmt.Models;

namespace HRMgmt.ViewModels
{
    public class EmployeeShiftGridViewModel
    {
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<string> Grid { get; set; } = new List<string>();
        public List<Shift> Shifts { get; set; } = new List<Shift>();
    }
}
