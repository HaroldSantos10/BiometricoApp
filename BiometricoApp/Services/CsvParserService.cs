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
            // Detectar línea de Identificación: ;;Identificación:;;HSGEMP130;...
            if (ContieneCampo(lineas[i], "Identificaci"))
            {
                var partes = lineas[i].Split(';');
                string codigo = partes.Length > 4 ? partes[4].Trim() : string.Empty;

                // Buscar nombre en las siguientes líneas
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

                // Buscar línea de encabezado de datos (Fecha;;Horario;Linea;...)
                int j = i + 1;
                while (j < lineas.Count && !lineas[j].StartsWith("Fecha"))
                    j++;

                j++; // saltar encabezado

                var registros = new List<RegistroBiometrico>();
                var registrosPorFecha = new Dictionary<DateOnly, RegistroBiometrico>();

                while (j < lineas.Count)
                {
                    var linea = lineas[j].TrimEnd('\r').Trim();

                    // Fin de bloque: nueva identificación o línea de hospital
                    if (ContieneCampo(linea, "Identificaci") ||
                        ContieneCampo(linea, "Hospital San Gabriel"))
                        break;

                    if (string.IsNullOrWhiteSpace(linea)) { j++; continue; }

                    var cols = linea.Split(';');
                    if (cols.Length < 4) { j++; continue; }

                    // Columna 1 = fecha (dd/MM/yyyy)
                    // Columna 3 = tipo (AU, E/S, ST)
                    // Columna 4 = entrada (puede incluir fecha: dd/MM/yyyy HH:mm)
                    // Columna 5 = salida (puede incluir fecha: dd/MM/yyyy HH:mm)

                    string fechaStr = cols.Length > 1 ? cols[1].Trim() : string.Empty;
                    string tipo = cols.Length > 3 ? cols[3].Trim() : string.Empty;
                    string entradaStr = cols.Length > 4 ? cols[4].Trim() : string.Empty;
                    string salidaStr = cols.Length > 5 ? cols[5].Trim() : string.Empty;

                    if (!DateOnly.TryParseExact(fechaStr, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateOnly fecha))
                    { j++; continue; }

                    // Ignorar ausencias
                    if (tipo == "AU") { j++; continue; }

                    // Extraer solo la hora de strings como "01/04/2026 07:41"
                    TimeOnly? entrada = ExtraerHora(entradaStr);
                    TimeOnly? salida = ExtraerHora(salidaStr);

                    // Agrupar por fecha — acumular entrada y salida
                    if (!registrosPorFecha.TryGetValue(fecha, out var reg))
                    {
                        reg = new RegistroBiometrico
                        {
                            Fecha = fecha,
                            TipoLinea = tipo,
                            EsTurnoNocturno = tipo == "E/S"
                        };
                        registrosPorFecha[fecha] = reg;
                    }

                    // Tomar la entrada más temprana y la salida más tardía
                    if (entrada.HasValue)
                    {
                        if (!reg.HoraEntrada.HasValue || entrada.Value < reg.HoraEntrada.Value)
                            reg.HoraEntrada = entrada;
                    }
                    if (salida.HasValue)
                    {
                        if (!reg.HoraSalida.HasValue || salida.Value > reg.HoraSalida.Value)
                            reg.HoraSalida = salida;
                    }

                    j++;
                }

                registros = registrosPorFecha.Values
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

    private TimeOnly? ExtraerHora(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        // Formato: "dd/MM/yyyy HH:mm" → extraer solo HH:mm
        var partes = valor.Trim().Split(' ');
        string horaStr = partes.Length > 1 ? partes[1].Trim() : partes[0].Trim();

        if (TimeOnly.TryParse(horaStr, out var hora))
            return hora;

        return null;
    }

    private bool ContieneCampo(string linea, string campo)
    {
        // Buscar en versión limpia sin acentos problemáticos
        return linea.Contains(campo, StringComparison.OrdinalIgnoreCase) ||
               linea.Replace("Ã³", "o").Replace("Ã©", "e").Replace("Ã\u0081", "A")
                    .Contains(campo, StringComparison.OrdinalIgnoreCase);
    }
}