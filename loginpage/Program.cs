using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using loginpage.Data;
using loginpage.Middlewares;
using loginpage.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();


var cs = builder.Configuration.GetConnectionString("Connection");

if (string.IsNullOrWhiteSpace(cs))
{
    throw new InvalidOperationException(
        "Connection string topilmadi. appsettings.json/appsettings.Development.json ichida " +
        "ConnectionStrings:Connection (yoki DefaultConnection) ni kiriting.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(cs));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login.html";
        options.LogoutPath = "/auth/logout";
        options.Cookie.Name = "loginpage.auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// ✅ migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/auth/register.html"));

// Agar middleware fayli bo'lmasa, vaqtincha comment qiling
app.UseMiddleware<EnsureUserActiveMiddleware>();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
