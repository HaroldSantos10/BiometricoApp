using BiometricoApp.Models;
using System.Globalization;
using System.Text;

namespace BiometricoApp.Services;

public class EmpleadoImportService
{
    public List<Empleado> ParsearCsvEmpleados(Stream stream)
    {
        var empleados = new List<Empleado>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        // Leer encabezado y detectar índices de columnas dinámicamente
        var headerLine = reader.ReadLine();
        if (headerLine == null) return empleados;

        var headers = ParsearLineaCsv(headerLine);

        // Mapear columnas por nombre para que no importe el orden
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
            idx[headers[i].Trim().Trim('"')] = i;

        while (!reader.EndOfStream)
        {
            var linea = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var partes = ParsearLineaCsv(linea);
            if (partes.Count < 5) continue;

            try
            {
                var empleado = new Empleado
                {
                    Nombre = ObtenerValor(partes, idx, "Nombre completo"),
                    DUI = ObtenerValor(partes, idx, "DUI"),
                    FechaIngreso = ParsearFecha(ObtenerValor(partes, idx, "Fecha de ingreso")),
                    Salario = ParsearDecimal(ObtenerValor(partes, idx, "Salario")),
                    Genero = ObtenerValor(partes, idx, "Genero"),
                    Correo = ObtenerValor(partes, idx, "Correo electrónico actualizado")
                                      .Trim(),
                    Direccion = ObtenerValor(partes, idx, "Dirección actual"),
                    Telefono = ObtenerValor(partes, idx, "Número de teléfono"),
                    Departamento = ObtenerValor(partes, idx, "Departamento"),
                    Cargo = ObtenerValor(partes, idx, "Cargo"),
                    TipoContrato = ObtenerValor(partes, idx, "Tipo de contrato"),
                    CargoAcademico = ObtenerValor(partes, idx, "Cargo academico"),
                    FechaNacimiento = ParsearFecha(ObtenerValor(partes, idx, "Fecha de nacimiento")),
                    CodigoEmpleado = string.Empty,
                    HoraEntrada = new TimeOnly(8, 0),
                    HoraSalida = new TimeOnly(17, 0),
                    HoraEntradaSabado = new TimeOnly(7, 0),
                    HoraSalidaSabado = new TimeOnly(11, 0),
                    TrabajaSabado = false,
                    Activo = true
                };

                if (!string.IsNullOrWhiteSpace(empleado.Nombre))
                    empleados.Add(empleado);
            }
            catch { continue; }
        }

        return empleados;
    }

    private string ObtenerValor(List<string> partes, Dictionary<string, int> idx, string columna)
    {
        // Buscar por nombre exacto primero
        if (idx.TryGetValue(columna, out int i) && i < partes.Count)
            return partes[i].Trim().Trim('"');

        // Buscar por coincidencia parcial si no encuentra exacto
        foreach (var key in idx.Keys)
        {
            if (key.Contains(columna, StringComparison.OrdinalIgnoreCase) ||
                columna.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                i = idx[key];
                if (i < partes.Count)
                    return partes[i].Trim().Trim('"');
            }
        }

        return string.Empty;
    }

    private DateOnly ParsearFecha(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor == "N/A") return default;

        string[] formatos = { "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
        foreach (var formato in formatos)
        {
            if (DateOnly.TryParseExact(valor, formato,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fecha))
                return fecha;
        }
        return default;
    }

    private decimal ParsearDecimal(string valor)
    {
        valor = valor.Replace("$", "").Replace(",", "").Trim();
        return decimal.TryParse(valor, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var resultado) ? resultado : 0;
    }

    private List<string> ParsearLineaCsv(string linea)
    {
        var resultado = new List<string>();
        bool dentroComillas = false;
        var campo = new StringBuilder();

        foreach (char c in linea)
        {
            if (c == '"')
                dentroComillas = !dentroComillas;
            else if (c == ',' && !dentroComillas)
            {
                resultado.Add(campo.ToString());
                campo.Clear();
            }
            else
                campo.Append(c);
        }
        resultado.Add(campo.ToString());
        return resultado;
    }
}