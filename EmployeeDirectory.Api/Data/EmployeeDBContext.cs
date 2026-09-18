using EmployeeDirectory.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeDirectory.Api.Data
{
    public class EmployeeDBContext(DbContextOptions<EmployeeDBContext> options) : DbContext(options)
    {
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.FirstName).IsUnique();

                //property/database table field lenght validation
                entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();

                entity.HasData(
                new Employee { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Department = "HR", Gender = Gender.Male },
                new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Department = "IT", Gender = Gender.Female },
                new Employee { Id = 3, FirstName = "Michael", LastName = "Johnson", Email = "michael.johnson@example.com", Department = "Finance", Gender = Gender.Male }
                );
            });
        }
    }

}