namespace HRMgmt.ViewModels
{
    public class PayrollReviewViewModel
    {
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public List<PayrollReviewRow> Rows { get; set; } = new();
        public decimal TotalGrossPay => Rows.Sum(r => r.GrossPay);
        public int TotalShifts => Rows.Sum(r => r.ShiftCount);
        public double TotalHours => Rows.Sum(r => r.TotalHours);
    }

    public class PayrollReviewRow
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string Role { get; set; } = "";
        public int ShiftCount { get; set; }
        public double TotalHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal GrossPay => (decimal)TotalHours * HourlyRate;
    }
}
