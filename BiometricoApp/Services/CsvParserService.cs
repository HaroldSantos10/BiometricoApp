using BiometricoApp.Models;
using System.Text;

namespace BiometricoApp.Services;

public class CsvParserService
{
    public List<(string CodigoEmpleado, string Nombre, string Departamento, List<RegistroBiometrico> Registros)> ParsearCsv(Stream stream)
    {
        var resultado = new List<(string, string, string, List<RegistroBiometrico>)>();

        using var reader = new StreamReader(stream, Encoding.Latin1);
        var lineas = new List<string>();
        while (!reader.EndOfStream)
        {
            var linea = reader.ReadLine();
            if (linea != null) lineas.Add(linea.TrimEnd('\r'));
        }

        int i = 0;
        while (i < lineas.Count)
        {
            if (ContieneCampo(lineas[i], "Identificaci"))
            {
                var partes = lineas[i].Split(';');
                string codigo = partes.Length > 4 ? partes[4].Trim() : string.Empty;

                string nombre = string.Empty;
                string departamento = string.Empty;

                for (int k = i + 1; k < Math.Min(i + 6, lineas.Count); k++)
                {
                    if (ContieneCampo(lineas[k], "Nombre:"))
                    {
                        var p = lineas[k].Split(';');
                        nombre = p.Length > 4 ? p[4].Trim() : string.Empty;
                    }
                    if (lineas[k].Contains("|"))
                    {
                        departamento = lineas[k].Split(';')
                            .FirstOrDefault(s => s.Contains("|"))
                            ?.Split('|')[0].Trim() ?? string.Empty;
                    }
                }

                int j = i + 1;
                while (j < lineas.Count && !lineas[j].StartsWith("Fecha"))
                    j++;
                j++;

                // Diccionario: fecha → registro
                var registrosPorFecha = new Dictionary<DateOnly, RegistroBiometrico>();

                while (j < lineas.Count)
                {
                    var linea = lineas[j].TrimEnd('\r').Trim();

                    if (ContieneCampo(linea, "Identificaci") ||
                        ContieneCampo(linea, "Hospital San Gabriel"))
                        break;

                    if (string.IsNullOrWhiteSpace(linea)) { j++; continue; }

                    var cols = linea.Split(';');
                    if (cols.Length < 4) { j++; continue; }

                    string fechaFilaStr = cols.Length > 1 ? cols[1].Trim() : string.Empty;
                    string tipo = cols.Length > 3 ? cols[3].Trim() : string.Empty;
                    string entradaStr = cols.Length > 4 ? cols[4].Trim() : string.Empty;
                    string salidaStr = cols.Length > 5 ? cols[5].Trim() : string.Empty;

                    if (!DateOnly.TryParseExact(fechaFilaStr, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateOnly fechaFila))
                    { j++; continue; }

                    if (tipo == "AU") { j++; continue; }

                    // Extraer hora Y fecha real de cada marcación
                    var (horaEntrada, fechaEntrada) = ExtraerFechaHora(entradaStr);
                    var (horaSalida, fechaSalida) = ExtraerFechaHora(salidaStr);

                    // Usar fecha real si está disponible, si no usar fecha de la fila
                    DateOnly fechaEntradaReal = fechaEntrada ?? fechaFila;
                    DateOnly fechaSalidaReal = fechaSalida ?? fechaFila;

                    // Registrar entrada en su fecha real
                    if (horaEntrada.HasValue)
                    {
                        if (!registrosPorFecha.TryGetValue(fechaEntradaReal, out var regEnt))
                        {
                            regEnt = new RegistroBiometrico
                            {
                                Fecha = fechaEntradaReal,
                                TipoLinea = tipo,
                                EsTurnoNocturno = tipo == "E/S"
                            };
                            registrosPorFecha[fechaEntradaReal] = regEnt;
                        }
                        // Tomar la entrada más temprana del día
                        if (!regEnt.HoraEntrada.HasValue || horaEntrada.Value < regEnt.HoraEntrada.Value)
                            regEnt.HoraEntrada = horaEntrada;
                    }

                    // Registrar salida en su fecha real
                    if (horaSalida.HasValue)
                    {
                        if (!registrosPorFecha.TryGetValue(fechaSalidaReal, out var regSal))
                        {
                            regSal = new RegistroBiometrico
                            {
                                Fecha = fechaSalidaReal,
                                TipoLinea = tipo,
                                EsTurnoNocturno = tipo == "E/S"
                            };
                            registrosPorFecha[fechaSalidaReal] = regSal;
                        }
                        // Tomar la salida más tardía del día
                        if (!regSal.HoraSalida.HasValue || horaSalida.Value > regSal.HoraSalida.Value)
                            regSal.HoraSalida = horaSalida;
                    }

                    j++;
                }

                var registros = registrosPorFecha.Values
                    .OrderBy(r => r.Fecha)
                    .ToList();

                if (!string.IsNullOrEmpty(nombre))
                    resultado.Add((codigo, nombre, departamento, registros));

                i = j;
            }
            else
            {
                i++;
            }
        }

        return resultado;
    }

    private (TimeOnly? hora, DateOnly? fecha) ExtraerFechaHora(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return (null, null);

        valor = valor.Trim();
        var partes = valor.Split(' ');

        if (partes.Length >= 2)
        {
            DateOnly? fecha = null;
            if (DateOnly.TryParseExact(partes[0].Trim(), "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var f))
                fecha = f;

            TimeOnly? hora = null;
            if (TimeOnly.TryParse(partes[1].Trim(), out var h))
                hora = h;

            return (hora, fecha);
        }

        if (TimeOnly.TryParse(partes[0].Trim(), out var soloHora))
            return (soloHora, null);

        return (null, null);
    }

    private bool ContieneCampo(string linea, string campo)
    {
        return linea.Contains(campo, StringComparison.OrdinalIgnoreCase) ||
               linea.Replace("Ã³", "o").Replace("Ã©", "e").Replace("Ã\u0081", "A")
                    .Contains(campo, StringComparison.OrdinalIgnoreCase);
    }
}