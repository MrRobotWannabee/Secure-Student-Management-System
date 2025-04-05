
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Secure_Student_Management_System.Models;

namespace Secure_Student_Management_System.Controllers
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet for Student model
        public DbSet<Student> Students { get; set; }
        


        // DbSet for other models (add more DbSets as needed)
        // For example, DbSet for other entities like Course, Teacher, etc.
        // public DbSet<Course> Courses { get; set; }

        // Override OnModelCreating if you need to configure relationships, keys, etc.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Example: Custom configuration for Student if needed
            builder.Entity<Student>()
                .Property(s => s.Email)
                .IsRequired();

            // Add any other custom configurations for other models
        }
    }
}
