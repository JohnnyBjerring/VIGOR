using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.Models;

namespace VIGOR.Web.Data
{
    /// <summary>
    /// Application DbContext – Identity + domæne-entiteter.
    /// </summary>
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Domain DbSets needed for UC02 (Citizens on Department)
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Citizen> Citizens => Set<Citizen>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<FixedMedication> FixedMedications => Set<FixedMedication>();
        public DbSet<PnMedication> PnMedications => Set<PnMedication>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee 1:1 IdentityUser
            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.HasIndex(e => e.IdentityUserId).IsUnique();
            });

            builder.Entity<FixedMedication>(entity =>
            {
                entity.HasKey(m => m.FixedMedicationId);
                entity.Property(m => m.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(m => m.ScheduleDescription)
                    .IsRequired()
                    .HasMaxLength(80)
                    .HasDefaultValue("Dagligt");

                entity.Property(m => m.IsActive)
                    .HasDefaultValue(true);

                entity.Property(m => m.GivenByUserId).HasMaxLength(450);

                entity.HasIndex(m => new { m.CitizenId, m.PlannedAt });

                // FK uden navigationer (undgår at ændre Citizen response-shape)
                entity.HasOne<Citizen>()
                    .WithMany()
                    .HasForeignKey(m => m.CitizenId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            builder.Entity<AuditEvent>(entity =>
            {
                entity.HasKey(e => e.AuditEventId);

                entity.Property(e => e.EntityType)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.UserDisplayNameSnapshot)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(e => new { e.CitizenId, e.CreatedAtUtc });
                entity.HasIndex(e => new { e.DepartmentId, e.CreatedAtUtc });
                entity.HasIndex(e => new { e.Action, e.CreatedAtUtc });

                // Restrict bevarer audit-spor og forhindrer utilsigtet sletning af historik sammen med en borger.
                entity.HasOne<Citizen>()
                    .WithMany()
                    .HasForeignKey(e => e.CitizenId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PnMedication>(entity =>
            {
                entity.HasKey(m => m.PnMedicationId);

                entity.Property(m => m.MedicineName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(m => m.Dose)
                    .IsRequired()
                    .HasMaxLength(60);

                entity.Property(m => m.Reason)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(m => m.GivenByUserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasIndex(m => new { m.CitizenId, m.GivenAtUtc });
                entity.HasIndex(m => new { m.DepartmentId, m.GivenAtUtc });

                // FK uden navigationer (undgår at ændre Citizen response-shape)
                entity.HasOne<Citizen>()
                    .WithMany()
                    .HasForeignKey(m => m.CitizenId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
