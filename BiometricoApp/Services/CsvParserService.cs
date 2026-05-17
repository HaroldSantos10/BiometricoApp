using BiometricoApp.Models;
using System.Text;

namespace BiometricoApp.Services;

public class CsvParserService
{
    public List<(string CodigoEmpleado, string Nombre, string Departamento, List<RegistroBiometrico> Registros)> ParsearCsv(Stream stream)
    {
        var resultado = new List<(string, string, string, List<RegistroBiometrico>)>();
        var lineas = new List<string>();

        using var reader = new StreamReader(stream, Encoding.Latin1);
        while (!reader.EndOfStream)
        {
            var linea = reader.ReadLine();
            if (linea != null) lineas.Add(linea);
        }

        int i = 0;
        while (i < lineas.Count)
        {
            // Detectar inicio de bloque de empleado
            if (lineas[i].Contains("Identificaci") && lineas[i].Contains(":"))
            {
                string codigo = ExtraerValor(lineas[i]);
                string nombre = i + 1 < lineas.Count ? ExtraerValor(lineas[i + 1]) : "";
                string departamento = i + 2 < lineas.Count ? ExtraerPrimerCampo(lineas[i + 2]) : "";

                // Saltar hasta la fila de encabezado de datos
                int j = i + 3;
                while (j < lineas.Count && !lineas[j].StartsWith("Fecha"))
                    j++;

                j++; // saltar la fila de encabezados

                var registros = new List<RegistroBiometrico>();

                while (j < lineas.Count)
                {
                    var linea = lineas[j].Trim();

                    // Fin del bloque si encontramos otro empleado o línea vacía larga
                    if (linea.Contains("Identificaci") && linea.Contains(":")) break;
                    if (string.IsNullOrWhiteSpace(linea)) { j++; continue; }

                    var partes = linea.Split('\t');
                    if (partes.Length < 3) { j++; continue; }

                    // Intentar parsear la fecha
                    if (!DateOnly.TryParseExact(partes[0].Trim(), "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateOnly fecha))
                    { j++; continue; }

                    string tipoLinea = partes.Length > 2 ? partes[2].Trim() : "ST";

                    // Ausencias: no tienen entrada ni salida
                    if (tipoLinea == "AU") { j++; continue; }

                    TimeOnly? entrada = null;
                    TimeOnly? salida = null;

                    if (partes.Length > 3 && TimeOnly.TryParse(partes[3].Trim(), out TimeOnly e))
                        entrada = e;

                    if (partes.Length > 4 && TimeOnly.TryParse(partes[4].Trim(), out TimeOnly s))
                        salida = s;

                    bool esNocturno = tipoLinea == "E/S" || tipoLinea == "ES";

                    registros.Add(new RegistroBiometrico
                    {
                        Fecha = fecha,
                        HoraEntrada = entrada,
                        HoraSalida = salida,
                        TipoLinea = tipoLinea,
                        EsTurnoNocturno = esNocturno
                    });

                    j++;
                }

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

    private string ExtraerValor(string linea)
    {
        var partes = linea.Split(':');
        return partes.Length > 1 ? partes[1].Trim() : linea.Trim();
    }

    private string ExtraerPrimerCampo(string linea)
    {
        var partes = linea.Split('|');
        return partes.Length > 0 ? partes[0].Trim() : linea.Trim();
    }
}