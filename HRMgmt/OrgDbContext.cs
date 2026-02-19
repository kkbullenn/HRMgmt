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

        // Added DbSets for RBAC
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<SchedulingTemplate> SchedulingTemplates { get; set; }
        public DbSet<TemplateGenerationLog> TemplateGenerationLogs { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<HRMgmt.Models.Account> Account { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RBAC MAPPINGS (Role Permissions Junction)

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // EXISTING ENTITY MAPPINGS

            modelBuilder.Entity<User>()
                .HasOne(u => u.RoleNavigation)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

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
    }
}