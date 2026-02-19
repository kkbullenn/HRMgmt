using HRMgmt;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // ADDED: Required for Cookie Auth

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<OrgDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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