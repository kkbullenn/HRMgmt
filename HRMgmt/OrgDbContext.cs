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
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<SchedulingTemplate> SchedulingTemplates { get; set; }
        public DbSet<TemplateGenerationLog> TemplateGenerationLogs { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne<Role>()    
                .WithMany()               
                .HasForeignKey(u => u.Role);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.User)
                .WithMany(u => u.ShiftAssignments)
                .HasForeignKey(sa => sa.UserId);

            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Shift)
                .WithMany(s => s.ShiftAssignment)
                .HasForeignKey(sa => sa.ShiftId);

            modelBuilder.Entity<Payroll>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<TemplateGenerationLog>()
                .HasIndex(x => new { x.TemplateName, x.StartDate, x.EndDate });

        }
        public DbSet<HRMgmt.Models.Account> Account { get; set; } = default!;
    }

}
