using System;

namespace HRMgmt.ViewModels
{
    public class EmployeeShiftListItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string ShiftName { get; set; } = string.Empty;
        public DateOnly ShiftDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
