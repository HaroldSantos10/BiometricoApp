
using BiometricoApp.Models;
using Microsoft.EntityFrameworkCore;


namespace BiometricoApp.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Empleado> Empleados { get; set; } 
        public DbSet<RegistroBiometrico> RegistrosBiometricos { get; set; }
        public DbSet<ReporteEmpleado> ReportesEmpleados { get; set; }

    }
}
