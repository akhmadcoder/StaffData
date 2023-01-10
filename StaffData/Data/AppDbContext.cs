using Microsoft.EntityFrameworkCore;
using StaffData.Models;

namespace StaffData.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<StaffData.Models.Employee> Employees { get; set; }
        //public DbSet<Employee> Employees { get; set; }
    }
}
