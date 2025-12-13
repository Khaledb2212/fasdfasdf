using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Web_API.Models;

namespace Web_API.Models
{
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options)
        {
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<TrainerSkill> TrainerSkills { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    //optionsBuilder.UseSqlServer("Server=.;Database=WebProjectDB;User Id=sa;Password=123456;TrustServerCertificate=True;");
        //    optionsBuilder.UseSqlServer("Server=.;Database=WebProjectDB;User Id=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;");

        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // This line is required
            base.OnModelCreating(modelBuilder);

            // --- FIX FOR APPOINTMENTS CASCADE CYCLE ---

            // 1. If a Member is deleted, do NOT delete their appointments automatically.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Member)
                .WithMany()
                .HasForeignKey(a => a.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. If a Trainer is deleted, do NOT delete their appointments automatically.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Trainer)
                .WithMany()
                .HasForeignKey(a => a.TrainerID)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. If a Service is deleted, do NOT delete the appointments automatically.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceID)
                .OnDelete(DeleteBehavior.Restrict);
        }
        //public DbSet<Web_API.Models.Person> Person { get; set; } = default!;
    }
}
