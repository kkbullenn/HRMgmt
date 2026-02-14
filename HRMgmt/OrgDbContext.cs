using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
namespace HRMgmt
{
    public class OrgDbContext : DbContext
    {
        public OrgDbContext(DbContextOptions<OrgDbContext> options) : base(options) { }
        public OrgDbContext() { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<EmployeeShift> EmployeeShifts { get; set; }
        public DbSet<ShiftTemplate> ShiftTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmployeeShift>()
                .HasKey(es => new { es.EmployeeId, es.ShiftId, es.Date });

            modelBuilder.Entity<EmployeeShift>()
                .HasOne(es => es.Employee)
                .WithMany(e => e.EmployeeShifts)
                .HasForeignKey(es => es.EmployeeId);

            modelBuilder.Entity<EmployeeShift>()
                .HasOne(es => es.Shift)
                .WithMany(s => s.EmployeeShifts)
                .HasForeignKey(es => es.ShiftId);
        }
    }

}