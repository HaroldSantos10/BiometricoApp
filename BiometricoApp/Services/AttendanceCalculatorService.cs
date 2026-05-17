using BiometricoApp.Models;

namespace BiometricoApp.Services;

public class ResultadoDia
{
    public DateOnly Fecha { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public TimeOnly? EntradaReal { get; set; }
    public TimeOnly? SalidaReal { get; set; }
    public int MinutosTarde { get; set; }
    public int MinutosExtraAntes { get; set; }
    public int MinutosExtraDespues { get; set; }
    public bool EsDomingo { get; set; }
    public bool EsSabado { get; set; }
    public string Observacion { get; set; } = string.Empty;
}

public class ResultadoEmpleado
{
    public Empleado Empleado { get; set; } = null!;
    public List<ResultadoDia> Dias { get; set; } = new();
    public int TotalMinutosTarde => Dias.Sum(d => d.MinutosTarde);
    public int TotalMinutosExtraAntes => Dias.Sum(d => d.MinutosExtraAntes);
    public int TotalMinutosExtraDespues => Dias.Sum(d => d.MinutosExtraDespues);
    public int TotalMinutosDomingo => Dias.Where(d => d.EsDomingo).Sum(d => d.MinutosExtraAntes + d.MinutosExtraDespues);
    public int TotalMinutosExtra => TotalMinutosExtraAntes + TotalMinutosExtraDespues;
    public bool TieneTardanzasCriticas => TotalMinutosTarde > 15;
}

public class AttendanceCalculatorService
{
    private const int MinutosUmbralExtraEntrada = 20;
    private const int MinutosUmbralExtraSalida = 30;

    public ResultadoEmpleado Calcular(Empleado empleado, List<RegistroBiometrico> registros)
    {
        var resultado = new ResultadoEmpleado { Empleado = empleado };

        // Agrupar por fecha y eliminar duplicados
        var porFecha = registros
            .GroupBy(r => r.Fecha)
            .OrderBy(g => g.Key);

        foreach (var grupo in porFecha)
        {
            var fecha = grupo.Key;
            var diaSemana = fecha.DayOfWeek;
            var regs = grupo.ToList();

            // Determinar entrada y salida del día
            var entrada = regs
                .Where(r => r.HoraEntrada.HasValue)
                .OrderBy(r => r.HoraEntrada)
                .FirstOrDefault()?.HoraEntrada;

            var salida = regs
                .Where(r => r.HoraSalida.HasValue)
                .OrderByDescending(r => r.HoraSalida)
                .FirstOrDefault()?.HoraSalida;

            var dia = new ResultadoDia
            {
                Fecha = fecha,
                DiaSemana = ObtenerNombreDia(diaSemana),
                EntradaReal = entrada,
                SalidaReal = salida,
                EsDomingo = diaSemana == DayOfWeek.Sunday,
                EsSabado = diaSemana == DayOfWeek.Saturday
            };

            if (diaSemana == DayOfWeek.Sunday)
            {
                // Domingo: registrar todo como extra
                if (entrada.HasValue && salida.HasValue)
                {
                    dia.MinutosExtraDespues = (int)(salida.Value.ToTimeSpan() - entrada.Value.ToTimeSpan()).TotalMinutes;
                    dia.Observacion = "Trabajo en domingo";
                }
                else if (entrada.HasValue)
                {
                    dia.Observacion = "Domingo - solo entrada registrada";
                }
            }
            else if (diaSemana == DayOfWeek.Saturday)
            {
                CalcularSabado(empleado, dia, entrada, salida);
            }
            else
            {
                CalcularDiaNormal(empleado, dia, entrada, salida);
            }

            resultado.Dias.Add(dia);
        }

        return resultado;
    }

    private void CalcularDiaNormal(Empleado emp, ResultadoDia dia,
        TimeOnly? entrada, TimeOnly? salida)
    {
        var horaEntrada = emp.HoraEntrada;
        var horaSalida = emp.HoraSalida;

        // Caso: sin ningún registro
        if (!entrada.HasValue && !salida.HasValue)
        {
            dia.Observacion = "Sin registros";
            return;
        }

        // Caso: solo salida
        if (!entrada.HasValue && salida.HasValue)
        {
            entrada = horaEntrada;
            dia.Observacion = "Sin entrada registrada - se asume hora de entrada";
        }

        // Caso: solo entrada
        if (entrada.HasValue && !salida.HasValue)
        {
            salida = horaSalida;
            dia.Observacion = "Sin salida registrada - se asume hora de salida";
        }

        // Calcular tardanza
        if (entrada!.Value > horaEntrada)
        {
            dia.MinutosTarde = (int)(entrada.Value.ToTimeSpan() - horaEntrada.ToTimeSpan()).TotalMinutes;
        }

        // Calcular extra antes (solo si llegó >= 20 min antes)
        if (entrada.Value < horaEntrada)
        {
            var minAntesDeEntrada = (int)(horaEntrada.ToTimeSpan() - entrada.Value.ToTimeSpan()).TotalMinutes;
            if (minAntesDeEntrada >= MinutosUmbralExtraEntrada)
                dia.MinutosExtraAntes = minAntesDeEntrada;
        }

        // Calcular extra después (solo si salió >= 30 min después)
        if (salida!.Value > horaSalida)
        {
            var minDespuesDeSalida = (int)(salida.Value.ToTimeSpan() - horaSalida.ToTimeSpan()).TotalMinutes;
            if (minDespuesDeSalida >= MinutosUmbralExtraSalida)
                dia.MinutosExtraDespues = minDespuesDeSalida;
        }
    }

    private void CalcularSabado(Empleado emp, ResultadoDia dia,
        TimeOnly? entrada, TimeOnly? salida)
    {
        if (!emp.TrabajaSabado)
        {
            // Si no trabaja sábado y hay registros, todo es extra
            if (entrada.HasValue && salida.HasValue)
            {
                dia.MinutosExtraDespues = (int)(salida.Value.ToTimeSpan() - entrada.Value.ToTimeSpan()).TotalMinutes;
                dia.Observacion = "Sábado no laboral - todo contabilizado como extra";
            }
            return;
        }

        var horaEntrada = emp.HoraEntradaSabado;
        var horaSalida = emp.HoraSalidaSabado;

        if (!entrada.HasValue && !salida.HasValue)
        {
            dia.Observacion = "Sin registros sábado";
            return;
        }

        if (!entrada.HasValue && salida.HasValue)
        {
            entrada = horaEntrada;
            dia.Observacion = "Sin entrada sábado - se asume hora de entrada";
        }

        if (entrada.HasValue && !salida.HasValue)
        {
            salida = horaSalida;
            dia.Observacion = "Sin salida sábado - se asume hora de salida";
        }

        // Tardanza sábado
        if (entrada!.Value > horaEntrada)
            dia.MinutosTarde = (int)(entrada.Value.ToTimeSpan() - horaEntrada.ToTimeSpan()).TotalMinutes;

        // Extra antes sábado
        if (entrada.Value < horaEntrada)
        {
            var min = (int)(horaEntrada.ToTimeSpan() - entrada.Value.ToTimeSpan()).TotalMinutes;
            if (min >= MinutosUmbralExtraEntrada)
                dia.MinutosExtraAntes = min;
        }

        // Extra después sábado
        if (salida!.Value > horaSalida)
        {
            var min = (int)(salida.Value.ToTimeSpan() - horaSalida.ToTimeSpan()).TotalMinutes;
            if (min >= MinutosUmbralExtraSalida)
                dia.MinutosExtraDespues = min;
        }
    }

    private string ObtenerNombreDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        DayOfWeek.Saturday => "Sábado",
        DayOfWeek.Sunday => "Domingo",
        _ => ""
    };

    public static string FormatearMinutos(int minutos)
    {
        if (minutos == 0) return "-";
        var h = minutos / 60;
        var m = minutos % 60;
        if (h > 0) return $"{h}h {m:D2}m";
        return $"{m}m";
    }
}