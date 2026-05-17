namespace BiometricoApp.Models
{
    public class ReporteEmpleado
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = null!;
        public int Mes { get; set; }
        public int Anio { get; set; }

        public int MinutosTardes { get; set; }
        public int MinutosExtraAntes { get; set; }
        public int MinutosExtraDespues { get; set; }
        public int MinutosDomingo { get; set; }


    }
}
