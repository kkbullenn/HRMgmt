using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMgmt;
using HRMgmt.ViewModels;

namespace HRMgmt.Controllers
{
    public class PayrollController : Controller
    {
        private readonly OrgDbContext _context;

        public PayrollController(OrgDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HttpPost]
        public IActionResult Review(DateTime? payPeriodStart, DateTime? payPeriodEnd)
        {
            var end = payPeriodEnd ?? DateTime.Today;
            var start = payPeriodStart ?? end.AddDays(-13);

            if (end < start)
            {
                (start, end) = (end, start);
            }

            var shiftsInPeriod = _context.EmployeeShifts
                .Include(es => es.Employee)
                .Include(es => es.Shift)
                .Where(es => es.Date >= start.Date && es.Date <= end.Date)
                .ToList();

            var grouped = shiftsInPeriod
                .GroupBy(es => es.EmployeeId)
                .Select(g =>
                {
                    var emp = g.First().Employee;
                    var totalHours = g.Sum(es =>
                    {
                        var shift = es.Shift;
                        var duration = shift.EndTime - shift.StartTime;
                        return duration.TotalHours;
                    });
                    return new PayrollReviewRow
                    {
                        EmployeeId = emp.ID,
                        EmployeeName = emp.Name ?? "",
                        Role = emp.Role ?? "",
                        ShiftCount = g.Count(),
                        TotalHours = Math.Round(totalHours, 2),
                        HourlyRate = emp.Salary
                    };
                })
                .OrderBy(r => r.EmployeeName)
                .ToList();

            var vm = new PayrollReviewViewModel
            {
                PayPeriodStart = start,
                PayPeriodEnd = end,
                Rows = grouped
            };

            return View(vm);
        }
    }
}
