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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee 1:1 IdentityUser
            builder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.HasIndex(e => e.IdentityUserId).IsUnique();
            });
        }
    }
}
