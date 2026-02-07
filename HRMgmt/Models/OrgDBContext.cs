using HRMgmt.Models;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace HRMgmt
{

    public class OrgDbContext : DbContext
    {
        public OrgDbContext(DbContextOptions<OrgDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShiftAssignment>()
                .HasKey(sa => new { sa.ShiftID, sa.EmployeeId });

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Shift)
                .WithMany(s => s.ShiftAssignments)
                .HasForeignKey(sa => sa.ShiftID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Employee)
                .WithMany(e => e.ShiftAssignments)
                .HasForeignKey(sa => sa.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Client> Clients { get; set; }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }

        public DbSet<OrgMgmt.Models.Shift> Shifts { get; set; } = default!;
        public DbSet<OrgMgmt.Models.ShiftAssignment> ShiftsAssignments { get; set; } = default!;


    }
}