using Microsoft.EntityFrameworkCore;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Models;
using VIGOR.Web.Data;
using VIGOR.Web.Services;

namespace VIGOR.UnitTests.Services
{
    public class AuditServiceTests
    {
        [Fact]
        public async Task LogCitizenStatusUpdatedAsync_AddsAuditEventWithUserAndDepartmentContext()
        {
            using var context = CreateContext();
            var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
            var service = new AuditService(context);

            await service.LogCitizenStatusUpdatedAsync(
                citizen.CitizenId,
                department.DepartmentId,
                "user-1",
                "Anna Andersen",
                CitizenStatus.Red,
                ShiftType.Day);

            await context.SaveChangesAsync();

            var auditEvent = await context.AuditEvents.SingleAsync();
            Assert.Equal("CitizenStatusUpdated", auditEvent.Action);
            Assert.Equal("Citizen", auditEvent.EntityType);
            Assert.Equal(citizen.CitizenId, auditEvent.EntityId);
            Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
            Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
            Assert.Equal("user-1", auditEvent.UserId);
            Assert.Equal("Anna Andersen", auditEvent.UserDisplayNameSnapshot);
            Assert.Equal(ShiftType.Day, auditEvent.ShiftType);
        }

        [Fact]
        public async Task FixedMedicationService_GiveAsync_WritesAuditEvent_WhenAuditServiceIsInjected()
        {
            using var context = CreateContext();
            var (department, citizen) = await CreateCitizenInDepartmentAsync(context);

            var medication = new FixedMedication
            {
                CitizenId = citizen.CitizenId,
                Name = "Panodil",
                PlannedAt = new DateTime(2000, 1, 1, 8, 0, 0),
                ScheduleDescription = "Fast tidspunkt",
                IsActive = true
            };

            context.FixedMedications.Add(medication);
            await context.SaveChangesAsync();

            var auditService = new AuditService(context);
            var medicationService = new FixedMedicationService(context, auditService);

            var result = await medicationService.GiveAsync(
                citizen.CitizenId,
                medication.FixedMedicationId,
                department.DepartmentId,
                "user-1",
                DateTime.UtcNow,
                userDisplayNameSnapshot: "Anna Andersen");

            Assert.NotNull(result);

            var auditEvent = await context.AuditEvents.SingleAsync();
            Assert.Equal("FixedMedicationGiven", auditEvent.Action);
            Assert.Equal("FixedMedication", auditEvent.EntityType);
            Assert.Equal(medication.FixedMedicationId, auditEvent.EntityId);
            Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
            Assert.Equal("user-1", auditEvent.UserId);
            Assert.Equal("Anna Andersen", auditEvent.UserDisplayNameSnapshot);
        }

        [Fact]
        public async Task PnMedicationService_RegisterAsync_WritesAuditEventWithShiftContext_WhenAuditServiceIsInjected()
        {
            using var context = CreateContext();
            var (department, citizen) = await CreateCitizenInDepartmentAsync(context);
            var auditService = new AuditService(context);
            var service = new PnMedicationService(context, auditService);

            var request = new RegisterPnMedicationRequest
            {
                MedicineName = "Panodil",
                Dose = "1 tablet",
                Reason = "Smerter",
                GivenAt = DateTime.UtcNow,
                ShiftType = ShiftType.Evening
            };

            var result = await service.RegisterAsync(
                citizen.CitizenId,
                department.DepartmentId,
                "user-2",
                request,
                userDisplayNameSnapshot: "Bent Jensen");

            Assert.NotNull(result);

            var auditEvent = await context.AuditEvents.SingleAsync();
            Assert.Equal("PnMedicationRegistered", auditEvent.Action);
            Assert.Equal("PnMedication", auditEvent.EntityType);
            Assert.Equal(result!.PnMedicationId, auditEvent.EntityId);
            Assert.Equal(citizen.CitizenId, auditEvent.CitizenId);
            Assert.Equal(department.DepartmentId, auditEvent.DepartmentId);
            Assert.Equal("user-2", auditEvent.UserId);
            Assert.Equal("Bent Jensen", auditEvent.UserDisplayNameSnapshot);
            Assert.Equal(ShiftType.Evening, auditEvent.ShiftType);
        }

        private static async Task<(Department Department, Citizen Citizen)> CreateCitizenInDepartmentAsync(AppDbContext context)
        {
            var department = new Department { Name = "Afdeling A" };
            context.Departments.Add(department);
            await context.SaveChangesAsync();

            var citizen = new Citizen
            {
                Name = "Borger A",
                DepartmentId = department.DepartmentId
            };

            context.Citizens.Add(citizen);
            await context.SaveChangesAsync();

            return (department, citizen);
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }
    }
}
