using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMgmt.ViewModels
{
	public class AdminPayrollCalcViewModel
	{
		[Required]
		[DataType(DataType.Date)]
		public DateTime StartDate { get; set; }

		[Required]
		[DataType(DataType.Date)]
		public DateTime EndDate { get; set; }

		public List<AdminPayrollRow> Rows { get; set; } = new();

		public decimal TotalHoursAllEmployees { get; set; }
		public decimal TotalGrossAllEmployees { get; set; }
		public decimal TotalPensionAllEmployees { get; set; }
		public decimal TotalTaxAllEmployees { get; set; }
		public decimal TotalNetAllEmployees { get; set; }
	}

	public class AdminPayrollRow
	{
		public Guid UserId { get; set; }
		public string EmployeeName { get; set; } = "";
		public decimal HourlyWage { get; set; }

		public int ShiftCount { get; set; }
		public decimal TotalHours { get; set; }
		public decimal GrossPay { get; set; }

		public decimal PensionDeduction { get; set; }
		public decimal TaxDeduction { get; set; }
		public decimal NetPay { get; set; }
	}
}
