using Microsoft.AspNetCore.DataProtection;
using System.IO;


using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web_Project.Data;
using Web_Project.Models;
using Web_Project.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));



var sharedKeysPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "shared-keys")
);

//Also: you set RequireConfirmedAccount = true.
//For a university project, this usually blocks login unless you implement email confirmation.
//If you don’t plan to implement it, set it to false. 
builder.Services.AddDefaultIdentity<UserDetails>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    // Allow simple password because your project requirement is: "sau"
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
    options.Password.RequiredUniqueChars = 1;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(sharedKeysPath))
    .SetApplicationName("WebProject.SharedAuth");

// Add services to the container.
builder.Services.AddControllersWithViews();





builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CookieForwardingHandler>();

builder.Services.AddHttpClient("WebApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7085/"); // your Web_API base URL
})
.AddHttpMessageHandler<CookieForwardingHandler>();
builder.Services.AddRazorPages();

var app = builder.Build();

static async Task SeedRolesAndAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserDetails>>();

    // Ensure roles exist
    string[] roles = { "Admin", "Member", "Trainer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Ensure required Admin exists
    var adminEmail = "studentnumber@sakarya.edu.tr";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new UserDetails
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, "sau");
        if (!createResult.Succeeded)
            throw new Exception(string.Join(" | ", createResult.Errors.Select(e => e.Description)));
    }

    // Ensure Admin role assignment
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseDeveloperExceptionPage();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

await SeedRolesAndAdminAsync(app);
app.Run();
