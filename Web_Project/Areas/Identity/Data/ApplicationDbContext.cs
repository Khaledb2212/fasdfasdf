using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web_Project.Models;

namespace Web_Project.Data;

public class ApplicationDbContext : IdentityDbContext<UserDetails>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.UseSqlServer("Server=.;Database=WebProjectDB;User Id=sa;Password=123456;TrustServerCertificate=True;");
        optionsBuilder.UseSqlServer("Server=.;Database=WebProjectDB;User Id=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;");

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
