using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VIGOR.Web.Data;
using VIGOR.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add VIGOR Web services (DI – Identity, Auth, Controllers, etc.)
builder.Services.AddVigorWebServices(builder.Configuration);

// Swagger / OpenAPI (Development only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed roller og testbruger
await SeedDataAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health-check endpoint
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

/// <summary>
/// Seeder roller (Leder, Vagtansvarlig, Personale) og en testbruger ved opstart.
/// </summary>
static async Task SeedDataAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    // Opret roller
    string[] roleNames = ["Leder", "Vagtansvarlig", "Personale"];
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Opret testbruger (Leder)
    const string testEmail = "admin@vigor.dk";
    const string testPassword = "Admin1234";
    if (await userManager.FindByEmailAsync(testEmail) == null)
    {
        var user = new IdentityUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, testPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Leder");
        }
    }

    // Opret testbruger (Vagtansvarlig)
    const string leadEmail = "vagtansvarlig@vigor.dk";
    const string leadPassword = "Test1234";
    if (await userManager.FindByEmailAsync(leadEmail) == null)
    {
        var user = new IdentityUser
        {
            UserName = leadEmail,
            Email = leadEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, leadPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Vagtansvarlig");
        }
    }

    // Opret testbruger (Personale)
    const string staffEmail = "personale@vigor.dk";
    const string staffPassword = "Test1234";
    if (await userManager.FindByEmailAsync(staffEmail) == null)
    {
        var user = new IdentityUser
        {
            UserName = staffEmail,
            Email = staffEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, staffPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Personale");
        }
    }

    // Opret testbruger UDEN rolle (til Denied-flow test)
    const string noRoleEmail = "norole@vigor.dk";
    const string noRolePassword = "Test1234";
    if (await userManager.FindByEmailAsync(noRoleEmail) == null)
    {
        var user = new IdentityUser
        {
            UserName = noRoleEmail,
            Email = noRoleEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, noRolePassword);
    }

    // Opret test data (Afdelinger og Borgere)
    if (!await context.Departments.AnyAsync())
    {
        var department1 = new VIGOR.Shared.Models.Department { Name = "Afdeling A" };
        var department2 = new VIGOR.Shared.Models.Department { Name = "Afdeling B" };

        context.Departments.AddRange(department1, department2);
        await context.SaveChangesAsync();

        context.Citizens.AddRange(
            new VIGOR.Shared.Models.Citizen { Name = "Jens Hansen", Status = "Aktiv", DepartmentId = department1.DepartmentId },
            new VIGOR.Shared.Models.Citizen { Name = "Anna Nielsen", Status = "Aktiv", DepartmentId = department1.DepartmentId },
            new VIGOR.Shared.Models.Citizen { Name = "Ole Jensen", Status = "Inaktiv", DepartmentId = department1.DepartmentId },
            new VIGOR.Shared.Models.Citizen { Name = "Peter Møller", Status = "Aktiv", DepartmentId = department2.DepartmentId },
            new VIGOR.Shared.Models.Citizen { Name = "Mette Poulsen", Status = "Inaktiv", DepartmentId = department2.DepartmentId }
        );
        await context.SaveChangesAsync();
    }
}
