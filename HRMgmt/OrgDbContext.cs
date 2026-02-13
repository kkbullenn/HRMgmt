using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
namespace HRMgmt
{
    public class OrgDbContext : DbContext
    {
        public OrgDbContext(DbContextOptions<OrgDbContext> options) : base(options) { }
        public OrgDbContext() { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<EmployeeShift> EmployeeShifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne<Role>()    
                .WithMany()               
                .HasForeignKey(u => u.Role);

            modelBuilder.Entity<EmployeeShift>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(es => es.UserId);

            modelBuilder.Entity<EmployeeShift>()
                .HasOne<Shift>()
                .WithMany(s => s.EmployeeShifts)
                .HasForeignKey(es => es.ShiftId);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(sa => sa.UserId);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne<Shift>()
                .WithMany()
                .HasForeignKey(sa => sa.ShiftId);

            modelBuilder.Entity<Payroll>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.UserId);
        }
    }

}