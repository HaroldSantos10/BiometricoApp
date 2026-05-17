namespace BiometricoApp.Models;

public class RegistroBiometrico
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public TimeOnly? HoraEntrada { get; set; }
    public TimeOnly? HoraSalida { get; set; }
    public string TipoLinea { get; set; } = "ST";
    public string ArchivoOrigen { get; set; } = string.Empty;
    public bool EsTurnoNocturno { get; set; } = false;
}
