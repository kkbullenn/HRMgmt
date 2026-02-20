using HRMgmt;
using HRMgmt.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // ADDED: Required for Cookie Auth

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useLocalDb = builder.Configuration.GetValue<bool>("UseLocalDb");
var testModeValue = Environment.GetEnvironmentVariable("TEST_MODE");
if (!string.IsNullOrWhiteSpace(testModeValue) && bool.TryParse(testModeValue, out var testModeEnabled))
{
    useLocalDb = testModeEnabled;
}
var hostedConnection = builder.Configuration.GetConnectionString("HostedConnection");
var localConnection = builder.Configuration.GetConnectionString("LocalConnection");

builder.Services.AddDbContext<OrgDbContext>(options =>
{
    if (useLocalDb)
    {
        options.UseSqlite(localConnection);
    }
    else
    {
        options.UseMySql(hostedConnection, ServerVersion.AutoDetect(hostedConnection));
    }
});

// ADDED: Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Where to send users who aren't logged in
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/Error"; // Where to send users who lack permissions
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // How long the login lasts
    });

// ADDED: Configure Authorization Policies (Optional but powerful)
builder.Services.AddAuthorization(options =>
{
    // Example: If you want to use [Authorize(Policy = "ManagePayroll")] instead of Roles later
    options.AddPolicy("ManagePayroll", policy =>
        policy.RequireClaim("Permission", "Payroll.Edit", "Payroll.Create"));
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrgDbContext>();
    if (db.Database.IsSqlite())
    {
        db.Database.EnsureCreated();
        SeedQaTestAccount(db);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ADDED: Must be exactly here, before UseAuthorization
app.UseAuthentication();

app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static void SeedQaTestAccount(OrgDbContext db)
{
    const string username = "qa_test";
    const string password = "123456";
    const string roleName = "Admin";

    if (!db.Roles.Any(r => r.RoleName == roleName))
    {
        db.Roles.Add(new Role { RoleName = roleName });
        db.SaveChanges();
    }

    if (!db.Account.Any(a => a.Username == username))
    {
        db.Account.Add(new Account
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = roleName,
            DisplayName = "QA Test",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}
