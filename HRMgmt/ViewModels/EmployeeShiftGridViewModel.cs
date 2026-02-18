using System.Collections.Generic;
using HRMgmt.Models;

namespace HRMgmt.ViewModels
{
    public class EmployeeShiftGridViewModel
    {
        public List<string> Grid { get; set; } = new List<string>();
        public List<Shift> Shifts { get; set; } = new List<Shift>();
        public List<User> Users { get; set; } = new List<User>();
    }
}
