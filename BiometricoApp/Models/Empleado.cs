namespace BiometricoApp.Models
{
    public class Empleado
    {
        public int Id { get; set; }
        public string CodigoEmpleado { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string DUI { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public decimal Salario { get; set; }
        public DateOnly FechaIngreso { get; set; }
        public DateOnly FechaNacimiento { get; set; }
        public string TipoContrato { get; set; } = string.Empty;
        public string CargoAcademico { get; set; } = string.Empty;
        public TimeOnly HoraEntrada { get; set; } = new TimeOnly(8, 0);
        public TimeOnly HoraSalida { get; set; } = new TimeOnly(17, 0);
        public TimeOnly HoraEntradaSabado { get; set; } = new TimeOnly(7, 0);
        public TimeOnly HoraSalidaSabado { get; set; } = new TimeOnly(11, 0);
        public bool TrabajaSabado { get; set; } = false;
        public bool Activo { get; set; } = true;
        public bool TurnoNocturno { get; set; } = false;
    }
}
