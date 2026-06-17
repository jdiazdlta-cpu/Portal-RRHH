using System.Globalization;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

var options = MigrationOptions.Parse(args);
await new MigrationRunner(options).RunAsync();

internal sealed record MigrationOptions(string Mode, string Source, string ConnectionString, bool Confirm, string ReportDirectory)
{
    public static MigrationOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var confirm = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--confirm", StringComparison.OrdinalIgnoreCase))
            {
                confirm = true;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                values["mode"] = arg;
                continue;
            }

            var key = arg[2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[++i];
            }
        }

        var root = FindWorkspaceRoot();
        var source = values.GetValueOrDefault("source",
            values.GetValueOrDefault("file", Path.Combine(root, "Dataverse_Import_Normalizado_v2.xlsx")));

        return new MigrationOptions(
            values.GetValueOrDefault("mode", "validate"),
            source,
            values.GetValueOrDefault("connection", "Server=localhost;Database=PortalRRHHFZ;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;"),
            confirm,
            values.GetValueOrDefault("report-dir", Path.Combine(root, "docs", "migration-import-report")));
    }

    private static string FindWorkspaceRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "backend")) && Directory.Exists(Path.Combine(current.FullName, "docs")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

internal sealed class MigrationRunner(MigrationOptions options)
{
    private static readonly string[] Modes = ["validate", "import-catalogs", "import-colaboradores", "assign-leaders", "full-import", "db-summary"];
    private static readonly string[] WriteModes = ["import-catalogs", "import-colaboradores", "assign-leaders", "full-import"];
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private object? _lastResult;

    public async Task RunAsync()
    {
        Directory.CreateDirectory(options.ReportDirectory);
        var mode = options.Mode.ToLowerInvariant();
        if (!Modes.Contains(mode))
        {
            throw new InvalidOperationException($"Modo no soportado: {options.Mode}");
        }

        if (mode == "db-summary")
        {
            _lastResult = await BuildDbSummaryAsync();
            await WriteSummaryAsync(mode, null);
            Console.WriteLine(JsonSerializer.Serialize(_lastResult, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (WriteModes.Contains(mode) && !options.Confirm)
        {
            _warnings.Add("Ejecucion en modo simulacion. No se escribira en SQL porque falta --confirm.");
        }

        if (!File.Exists(options.Source))
        {
            _errors.Add($"No se encontro el archivo Excel: {options.Source}");
            await WriteSummaryAsync(mode, null);
            Console.WriteLine("Archivo Excel no encontrado. Se genero resumen con error.");
            return;
        }

        using var document = SpreadsheetDocument.Open(options.Source, false);
        var source = SourceWorkbook.Load(document);
        var validation = ValidationResult.Build(source);

        WriteValidationReports(validation);
        if (mode == "validate")
        {
            _lastResult = validation.ToSummary();
            await WriteSummaryAsync(mode, validation);
            Console.WriteLine(JsonSerializer.Serialize(_lastResult, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (mode is "import-catalogs" or "full-import")
        {
            _lastResult = await ImportCatalogsAsync(source, validation);
        }

        if (mode is "import-colaboradores" or "full-import")
        {
            _lastResult = await ImportColaboradoresAsync(source, validation);
        }

        if (mode is "assign-leaders" or "full-import")
        {
            _lastResult = await AssignLeadersAsync(source, validation);
        }

        WriteCsv("import-warnings.csv", ["Warning"], _warnings.Select(x => new[] { x }));
        WriteCsv("import-errors.csv", ["Error"], _errors.Select(x => new[] { x }));
        await WriteSummaryAsync(mode, validation);
        Console.WriteLine($"Modo {mode} finalizado. Confirm={options.Confirm}. Reportes: {options.ReportDirectory}");
    }

    private async Task<object> ImportCatalogsAsync(SourceWorkbook source, ValidationResult validation)
    {
        await using var db = CreateDbContext();
        var rows = new List<string[]>();
        var insertedEmpresas = 0;
        var insertedDepartamentos = 0;
        var insertedCargos = 0;

        foreach (var row in source.Empresas)
        {
            var nombre = Get(row, "EMPRESA");
            var externalId = NormalizeId(Get(row, "ID_EMPRESA"));
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(externalId))
            {
                rows.Add(["Empresa", externalId, nombre, "Skipped", "ID_EMPRESA/EMPRESA vacio"]);
                continue;
            }

            var exists = await db.Empresas.AnyAsync(x => x.Nombre == nombre);
            rows.Add(["Empresa", externalId, nombre, exists ? "Exists" : options.Confirm ? "Inserted" : "SimulatedInsert", ""]);
            if (options.Confirm && !exists)
            {
                db.Empresas.Add(new Empresa { Nombre = nombre, Ruc = null, IsActive = true, CreatedBy = "MigrationAnalyzer" });
                insertedEmpresas++;
            }
        }

        if (options.Confirm)
        {
            await db.SaveChangesAsync();
        }

        var empresaMap = await BuildEmpresaMapAsync(db, source);
        foreach (var row in source.Departamentos)
        {
            var externalId = NormalizeId(Get(row, "ID_DEPARTAMENTO"));
            var empresaExternalId = NormalizeId(Get(row, "ID_EMPRESA"));
            var nombre = Get(row, "DEPARTAMENTO");
            if (!empresaMap.TryGetValue(empresaExternalId, out var empresaId))
            {
                rows.Add(["Departamento", externalId, nombre, "Skipped", "Empresa no encontrada"]);
                continue;
            }

            var exists = await db.Departamentos.AnyAsync(x => x.EmpresaId == empresaId && x.Nombre == nombre);
            rows.Add(["Departamento", externalId, nombre, exists ? "Exists" : options.Confirm ? "Inserted" : "SimulatedInsert", ""]);
            if (options.Confirm && !exists)
            {
                db.Departamentos.Add(new Departamento { EmpresaId = empresaId, Nombre = nombre, IsActive = true, CreatedBy = "MigrationAnalyzer" });
                insertedDepartamentos++;
            }
        }

        if (options.Confirm)
        {
            await db.SaveChangesAsync();
        }

        var departamentoMap = await BuildDepartamentoMapAsync(db, source, empresaMap);
        foreach (var row in source.Cargos)
        {
            var externalId = NormalizeId(Get(row, "ID_CARGO"));
            var departamentoExternalId = NormalizeId(Get(row, "ID_DEPARTAMENTO"));
            var nombre = Get(row, "CARGO");
            if (!validation.CargosImportablesIds.Contains(externalId))
            {
                rows.Add(["Cargo", externalId, nombre, "Skipped", "Cargo no referenciado por colaboradores"]);
                continue;
            }

            if (!departamentoMap.TryGetValue(departamentoExternalId, out var departamentoId))
            {
                rows.Add(["Cargo", externalId, nombre, "Skipped", "Departamento no encontrado"]);
                continue;
            }

            var exists = await db.Cargos.AnyAsync(x => x.DepartamentoId == departamentoId && x.Nombre == nombre);
            rows.Add(["Cargo", externalId, nombre, exists ? "Exists" : options.Confirm ? "Inserted" : "SimulatedInsert", ""]);
            if (options.Confirm && !exists)
            {
                db.Cargos.Add(new Cargo { DepartamentoId = departamentoId, Nombre = nombre, IsActive = true, CreatedBy = "MigrationAnalyzer" });
                insertedCargos++;
            }
        }

        if (options.Confirm)
        {
            await db.SaveChangesAsync();
        }

        WriteCsv("import-catalogs-result.csv", ["Entity", "ExternalId", "Name", "Action", "Message"], rows);
        return new { insertedEmpresas, insertedDepartamentos, insertedCargos, totalRows = rows.Count };
    }

    private async Task<object> ImportColaboradoresAsync(SourceWorkbook source, ValidationResult validation)
    {
        await using var db = CreateDbContext();
        var resultRows = new List<string[]>();
        var skippedRows = validation.BlockedRows.Select(x => new[]
        {
            NormalizeId(Get(x.Row, "ID COLABORADOR")),
            Get(x.Row, "CEDULA"),
            Get(x.Row, "PNOMBRE"),
            Get(x.Row, "PAPELLIDO"),
            "Blocked",
            x.Reason
        }).ToList();

        var maps = await BuildCatalogMapsAsync(db, source);
        var tipoPermanente = await db.TiposContrato.FirstAsync(x => x.Nombre == "Permanente");
        var tipoEventual = await db.TiposContrato.FirstAsync(x => x.Nombre == "Eventual");
        var estatusMap = await db.EstatusColaborador.ToDictionaryAsync(x => NormalizeKey(x.Codigo), x => x.EstatusId);
        var motivoMap = await db.MotivosSalida.ToDictionaryAsync(x => NormalizeKey(x.Nombre), x => x.MotivoSalidaId);
        var existingNoEmpleado = await db.Colaboradores.Select(x => x.NoEmpleado).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        var existingCedula = await db.Colaboradores.Select(x => x.Cedula).ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var existing = 0;
        var catalogSkipped = 0;

        foreach (var sourceRow in validation.ReadyRows)
        {
            var row = sourceRow.Row;
            var noEmpleado = NormalizeId(Get(row, "ID COLABORADOR"));
            var cedula = Get(row, "CEDULA");

            if (existingNoEmpleado.Contains(noEmpleado) || existingCedula.Contains(cedula))
            {
                existing++;
                resultRows.Add([noEmpleado, cedula, "Exists", "Ya existe por NoEmpleado o Cedula"]);
                continue;
            }

            if (!maps.Empresas.TryGetValue(NormalizeId(Get(row, "ID_EMPRESA")), out var empresaId) ||
                !maps.Departamentos.TryGetValue(NormalizeId(Get(row, "ID_DEPARTAMENTO")), out var departamentoId) ||
                !maps.Cargos.TryGetValue(NormalizeId(Get(row, "ID_CARGO")), out var cargoId))
            {
                catalogSkipped++;
                skippedRows.Add([noEmpleado, cedula, Get(row, "PNOMBRE"), Get(row, "PAPELLIDO"), "Skipped", "Catalogo no encontrado"]);
                continue;
            }

            if (!estatusMap.TryGetValue(NormalizeKey(Get(row, "ESTATUS")), out var estatusId))
            {
                skippedRows.Add([noEmpleado, cedula, Get(row, "PNOMBRE"), Get(row, "PAPELLIDO"), "Skipped", "Estatus no reconocido"]);
                continue;
            }

            var motivoSalidaId = ResolveMotivoSalidaId(motivoMap, Get(row, "MOTIVO DE SALIDA"));
            var contrato = NormalizeKey(Get(row, "TIPO DE CONTRATO")) == "E" ? tipoEventual : tipoPermanente;
            resultRows.Add([noEmpleado, cedula, options.Confirm ? "Inserted" : "SimulatedInsert", ""]);

            if (!options.Confirm)
            {
                continue;
            }

            db.Colaboradores.Add(new Colaborador
            {
                NoEmpleado = noEmpleado,
                Cedula = cedula,
                FechaVencimientoCedula = TryDate(Get(row, "VCED")),
                SeguroSocial = NullIfEmpty(Get(row, "SEGURO SOCIAL")),
                PrimerNombre = Get(row, "PNOMBRE"),
                SegundoNombre = NullIfEmpty(Get(row, "SNOMBRE")),
                PrimerApellido = Get(row, "PAPELLIDO"),
                SegundoApellido = NullIfEmpty(Get(row, "SAPELLIDO")),
                FechaNacimiento = TryDate(Get(row, "FECHA DE NACIMIENTO")),
                Sexo = NullIfEmpty(Get(row, "SEXO")),
                Telefono = NullIfEmpty(Get(row, "TELEFONO")),
                Email = NullIfEmpty(Get(row, "EMAIL")),
                Direccion = NullIfEmpty(Get(row, "DIRECCION")),
                EmpresaId = empresaId,
                DepartamentoId = departamentoId,
                CargoId = cargoId,
                JefeInmediatoId = null,
                FechaIngreso = TryDate(Get(row, "FECHA DE INGRESO")) ?? DateTime.Today,
                TipoContratoId = contrato.TipoContratoId,
                FechaVencimientoContrato = TryDate(Get(row, "VENCIMIENTO DE CONTRATO")),
                FechaVencimientoPeriodoProbatorio = null,
                TieneLicencia = ParseBool(Get(row, "LICENCIA DE CONDUCIR")),
                NumeroLicencia = null,
                TipoLicencia = NullIfEmpty(Get(row, "TIPO DE LICENCIA")),
                FechaVencimientoLicencia = TryDate(Get(row, "VLIC")),
                EstatusId = estatusId,
                Salario = TryDecimal(Get(row, "SALARIO")),
                Viaticos = TryDecimal(Get(row, "VIATICOS")),
                GastosRepresentacion = TryDecimal(Get(row, "GASTOS DE REPRESENTACION")),
                FechaSalida = TryDate(Get(row, "FECHA DE SALIDA")),
                MotivoSalidaId = motivoSalidaId,
                Vacante = ParseBool(Get(row, "VACANTE")),
                UltimaVacacion = TryDate(Get(row, "ULTIMA VACACIONES")),
                IsActive = true,
                CreatedBy = "MigrationAnalyzer"
            });
            inserted++;
            existingNoEmpleado.Add(noEmpleado);
            existingCedula.Add(cedula);
        }

        if (options.Confirm)
        {
            await db.SaveChangesAsync();
        }

        WriteCsv("import-colaboradores-result.csv", ["NoEmpleado", "Cedula", "Action", "Message"], resultRows);
        WriteCsv("import-skipped.csv", ["NoEmpleado", "Cedula", "PrimerNombre", "PrimerApellido", "Action", "Reason"], skippedRows);
        return new { inserted, blocked = validation.BlockedRows.Count, existing, catalogSkipped, processed = validation.ReadyRows.Count };
    }

    private async Task<object> AssignLeadersAsync(SourceWorkbook source, ValidationResult validation)
    {
        await using var db = CreateDbContext();
        var colaboradores = await db.Colaboradores.ToDictionaryAsync(x => x.NoEmpleado, x => x, StringComparer.OrdinalIgnoreCase);
        var resultRows = new List<string[]>();
        var assigned = 0;
        var pending = 0;
        var skipped = 0;

        foreach (var sourceRow in validation.ReadyRows)
        {
            var row = sourceRow.Row;
            var noEmpleado = NormalizeId(Get(row, "ID COLABORADOR"));
            var lider = NormalizeId(Get(row, "ID_LIDER_INMEDIATO"));
            if (string.IsNullOrWhiteSpace(lider))
            {
                continue;
            }

            if (!colaboradores.TryGetValue(noEmpleado, out var colaborador))
            {
                skipped++;
                resultRows.Add([noEmpleado, lider, "Skipped", "Colaborador no importado"]);
                continue;
            }

            if (!colaboradores.TryGetValue(lider, out var jefe))
            {
                pending++;
                resultRows.Add([noEmpleado, lider, "Pending", "Lider no existe en SQL o fue bloqueado"]);
                continue;
            }

            if (colaborador.ColaboradorId == jefe.ColaboradorId)
            {
                skipped++;
                resultRows.Add([noEmpleado, lider, "Skipped", "No se asigna jefe igual a si mismo"]);
                continue;
            }

            if (options.Confirm)
            {
                colaborador.JefeInmediatoId = jefe.ColaboradorId;
            }

            assigned++;
            resultRows.Add([noEmpleado, lider, options.Confirm ? "Assigned" : "SimulatedAssign", ""]);
        }

        if (options.Confirm)
        {
            await db.SaveChangesAsync();
        }

        WriteCsv("import-leaders-result.csv", ["NoEmpleado", "IdLider", "Action", "Message"], resultRows);
        return new { assigned, pending, skipped, processed = resultRows.Count };
    }

    private async Task<object> BuildDbSummaryAsync()
    {
        await using var db = CreateDbContext();
        var total = await db.Colaboradores.CountAsync();
        var porEstatus = await db.Colaboradores
            .Include(x => x.Estatus)
            .GroupBy(x => x.Estatus.Nombre)
            .Select(x => new { estatus = x.Key, count = x.Count() })
            .OrderBy(x => x.estatus)
            .ToListAsync();
        var porEmpresa = await db.Colaboradores
            .Include(x => x.Empresa)
            .GroupBy(x => x.Empresa.Nombre)
            .Select(x => new { empresa = x.Key, count = x.Count() })
            .OrderBy(x => x.empresa)
            .ToListAsync();
        var porContrato = await db.Colaboradores
            .Include(x => x.TipoContrato)
            .GroupBy(x => x.TipoContrato.Nombre)
            .Select(x => new { tipoContrato = x.Key, count = x.Count() })
            .OrderBy(x => x.tipoContrato)
            .ToListAsync();
        var catalogos = new
        {
            empresas = await db.Empresas.CountAsync(),
            departamentos = await db.Departamentos.CountAsync(),
            cargos = await db.Cargos.CountAsync()
        };

        return new { totalColaboradores = total, catalogos, porEstatus, porEmpresa, porContrato };
    }

    private async Task<Dictionary<string, int>> BuildEmpresaMapAsync(AppDbContext db, SourceWorkbook source)
    {
        var dbRows = await db.Empresas.ToDictionaryAsync(x => NormalizeKey(x.Nombre), x => x.EmpresaId);
        return source.Empresas
            .Where(x => dbRows.ContainsKey(NormalizeKey(Get(x, "EMPRESA"))))
            .ToDictionary(x => NormalizeId(Get(x, "ID_EMPRESA")), x => dbRows[NormalizeKey(Get(x, "EMPRESA"))]);
    }

    private async Task<Dictionary<string, int>> BuildDepartamentoMapAsync(AppDbContext db, SourceWorkbook source, Dictionary<string, int> empresaMap)
    {
        var dbRows = await db.Departamentos
            .Select(x => new { x.DepartamentoId, x.EmpresaId, x.Nombre })
            .ToDictionaryAsync(x => $"{x.EmpresaId}|{NormalizeKey(x.Nombre)}", x => x.DepartamentoId);

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in source.Departamentos)
        {
            var externalId = NormalizeId(Get(row, "ID_DEPARTAMENTO"));
            var empresaExternalId = NormalizeId(Get(row, "ID_EMPRESA"));
            if (!empresaMap.TryGetValue(empresaExternalId, out var empresaId))
            {
                continue;
            }

            var key = $"{empresaId}|{NormalizeKey(Get(row, "DEPARTAMENTO"))}";
            if (dbRows.TryGetValue(key, out var departamentoId))
            {
                map[externalId] = departamentoId;
            }
        }

        return map;
    }

    private async Task<CatalogMaps> BuildCatalogMapsAsync(AppDbContext db, SourceWorkbook source)
    {
        var empresaMap = await BuildEmpresaMapAsync(db, source);
        var departamentoMap = await BuildDepartamentoMapAsync(db, source, empresaMap);
        var dbCargos = await db.Cargos
            .Select(x => new { x.CargoId, x.DepartamentoId, x.Nombre })
            .ToDictionaryAsync(x => $"{x.DepartamentoId}|{NormalizeKey(x.Nombre)}", x => x.CargoId);

        var cargoMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in source.Cargos)
        {
            var externalId = NormalizeId(Get(row, "ID_CARGO"));
            var departamentoExternalId = NormalizeId(Get(row, "ID_DEPARTAMENTO"));
            if (!departamentoMap.TryGetValue(departamentoExternalId, out var departamentoId))
            {
                continue;
            }

            var key = $"{departamentoId}|{NormalizeKey(Get(row, "CARGO"))}";
            if (dbCargos.TryGetValue(key, out var cargoId))
            {
                cargoMap[externalId] = cargoId;
            }
        }

        return new CatalogMaps(empresaMap, departamentoMap, cargoMap);
    }

    private AppDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseSqlServer(options.ConnectionString);
        return new AppDbContext(builder.Options);
    }

    private void WriteValidationReports(ValidationResult validation)
    {
        WriteCsv("validation-result.csv",
            ["Metric", "Value"],
            new[]
            {
                new[] { "Empresas", validation.Empresas.ToString(CultureInfo.InvariantCulture) },
                new[] { "Departamentos", validation.Departamentos.ToString(CultureInfo.InvariantCulture) },
                new[] { "CargosAnalizados", validation.CargosAnalizados.ToString(CultureInfo.InvariantCulture) },
                new[] { "CargosImportables", validation.CargosImportables.ToString(CultureInfo.InvariantCulture) },
                new[] { "ColaboradoresTotal", validation.ColaboradoresTotal.ToString(CultureInfo.InvariantCulture) },
                new[] { "ColaboradoresListos", validation.ColaboradoresListos.ToString(CultureInfo.InvariantCulture) },
                new[] { "ColaboradoresBloqueados", validation.ColaboradoresBloqueados.ToString(CultureInfo.InvariantCulture) }
            });

        WriteCsv("import-skipped.csv",
            ["NoEmpleado", "Cedula", "PrimerNombre", "PrimerApellido", "Action", "Reason"],
            validation.BlockedRows.Select(x => new[]
            {
                NormalizeId(Get(x.Row, "ID COLABORADOR")),
                Get(x.Row, "CEDULA"),
                Get(x.Row, "PNOMBRE"),
                Get(x.Row, "PAPELLIDO"),
                "Blocked",
                x.Reason
            }));
    }

    private async Task WriteSummaryAsync(string mode, ValidationResult? validation)
    {
        var summary = new
        {
            mode,
            options.Confirm,
            options.Source,
            options.ConnectionString,
            timestamp = DateTime.UtcNow,
            validation = validation?.ToSummary(),
            result = _lastResult,
            warnings = _warnings.Count,
            errors = _errors.Count
        };
        await File.WriteAllTextAsync(Path.Combine(options.ReportDirectory, "import-summary.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void WriteCsv(string fileName, string[] headers, IEnumerable<string[]> rows)
    {
        var path = Path.Combine(options.ReportDirectory, fileName);
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    private static int? ResolveMotivoSalidaId(Dictionary<string, int> motivoMap, string value)
    {
        var key = NormalizeKey(string.IsNullOrWhiteSpace(value) ? "No aplica" : value);
        if (motivoMap.TryGetValue(key, out var id))
        {
            return id;
        }

        return motivoMap.TryGetValue(NormalizeKey("No aplica"), out var noAplicaId) ? noAplicaId : null;
    }

    private static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? TryDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 60000)
        {
            return DateTime.FromOADate(serial).Date;
        }

        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed) ||
               DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed)
            ? parsed.Date
            : null;
    }

    private static decimal TryDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var cleaned = value
            .Replace("B/.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("$", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(",", "", StringComparison.OrdinalIgnoreCase);
        }
        else if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(",", ".", StringComparison.OrdinalIgnoreCase);
        }

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static bool ParseBool(string value)
    {
        var normalized = NormalizeKey(value);
        return normalized is "1" or "TRUE" or "SI" or "S" or "YES" or "Y";
    }

    private static string NormalizeId(string value)
    {
        value = value.Trim();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric) && numeric == decimal.Truncate(numeric))
        {
            return decimal.ToInt64(numeric).ToString(CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static string NormalizeKey(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

internal sealed class SourceWorkbook
{
    public required List<Dictionary<string, string>> Empresas { get; init; }
    public required List<Dictionary<string, string>> Departamentos { get; init; }
    public required List<Dictionary<string, string>> Cargos { get; init; }
    public required List<SourceRow> Colaboradores { get; init; }

    public static SourceWorkbook Load(SpreadsheetDocument document)
    {
        return new SourceWorkbook
        {
            Empresas = ReadRows(document, "Empresas").Select(x => x.Row).ToList(),
            Departamentos = ReadRows(document, "Departamentos").Select(x => x.Row).ToList(),
            Cargos = ReadRows(document, "Cargos").Select(x => x.Row).ToList(),
            Colaboradores = ReadRows(document, "Colaboradores_Import")
        };
    }

    private static List<SourceRow> ReadRows(SpreadsheetDocument document, string sheetName)
    {
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart no encontrado.");
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sharedStrings = sharedStringTable is null
            ? []
            : sharedStringTable.Elements<SharedStringItem>().Select(x => x.InnerText).ToArray();
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook no encontrado.");
        var sheets = workbook.Sheets;
        var sheet = sheets?
            .Elements<Sheet>()
            .FirstOrDefault(x => string.Equals(x.Name?.Value, sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet?.Id?.Value is null)
        {
            return [];
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
        var worksheet = worksheetPart.Worksheet ?? throw new InvalidOperationException($"Worksheet no encontrado: {sheetName}");
        var sheetData = worksheet.GetFirstChild<SheetData>();
        var excelRows = sheetData?.Elements<Row>().ToList() ?? [];
        var headerRow = excelRows.FirstOrDefault(x => x.Elements<Cell>().Any());
        if (headerRow is null)
        {
            return [];
        }

        var headers = headerRow.Elements<Cell>()
            .Select(cell => new { Index = GetColumnIndex(cell.CellReference?.Value), Name = GetCellValue(cell, sharedStrings).Trim() })
            .Where(x => x.Name.Length > 0)
            .ToList();

        var rows = new List<SourceRow>();
        foreach (var row in excelRows.Where(x => x.RowIndex?.Value > headerRow.RowIndex?.Value))
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var cells = row.Elements<Cell>()
                .Where(x => x.CellReference?.Value is not null)
                .ToDictionary(x => GetColumnIndex(x.CellReference!.Value!), x => GetCellValue(x, sharedStrings));
            foreach (var header in headers)
            {
                values[header.Name] = cells.GetValueOrDefault(header.Index, string.Empty).Trim();
            }

            rows.Add(new SourceRow((int)(row.RowIndex?.Value ?? 0), values));
        }

        return rows;
    }

    private static string GetCellValue(Cell cell, string[] sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString &&
            int.TryParse(cell.CellValue?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedStringIndex) &&
            sharedStringIndex >= 0 &&
            sharedStringIndex < sharedStrings.Length)
        {
            return sharedStrings[sharedStringIndex];
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InnerText ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.Boolean)
        {
            return cell.CellValue?.Text == "1" ? "true" : "false";
        }

        return cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var column = 0;
        foreach (var ch in cellReference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            column = (column * 26) + char.ToUpperInvariant(ch) - 'A' + 1;
        }

        return column;
    }
}

internal sealed record SourceRow(int RowNumber, Dictionary<string, string> Row);

internal sealed class ValidationResult
{
    public required int Empresas { get; init; }
    public required int Departamentos { get; init; }
    public required int CargosAnalizados { get; init; }
    public required int CargosImportables { get; init; }
    public required int ColaboradoresTotal { get; init; }
    public required List<SourceRow> ReadyRows { get; init; }
    public required List<BlockedRow> BlockedRows { get; init; }
    public required HashSet<string> CargosImportablesIds { get; init; }
    public int ColaboradoresListos => ReadyRows.Count;
    public int ColaboradoresBloqueados => BlockedRows.Count;

    public object ToSummary() => new
    {
        Empresas,
        Departamentos,
        CargosAnalizados,
        CargosImportables,
        ColaboradoresTotal,
        ColaboradoresListos,
        ColaboradoresBloqueados
    };

    public static ValidationResult Build(SourceWorkbook source)
    {
        var idCounts = source.Colaboradores
            .GroupBy(x => NormalizeId(Get(x.Row, "ID COLABORADOR")), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var cedulaCounts = source.Colaboradores
            .GroupBy(x => Get(x.Row, "CEDULA"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);

        var ready = new List<SourceRow>();
        var blocked = new List<BlockedRow>();
        foreach (var row in source.Colaboradores)
        {
            var noEmpleado = NormalizeId(Get(row.Row, "ID COLABORADOR"));
            var cedula = Get(row.Row, "CEDULA");
            var reasons = new List<string>();
            if (string.IsNullOrWhiteSpace(noEmpleado))
            {
                reasons.Add("ID_COLABORADOR vacio");
            }
            else if (idCounts.GetValueOrDefault(noEmpleado) > 1)
            {
                reasons.Add("ID_COLABORADOR duplicado");
            }

            if (string.IsNullOrWhiteSpace(cedula))
            {
                reasons.Add("CEDULA vacia");
            }
            else if (cedulaCounts.GetValueOrDefault(cedula) > 1)
            {
                reasons.Add("CEDULA duplicada");
            }

            if (reasons.Count == 0)
            {
                ready.Add(row);
            }
            else
            {
                blocked.Add(new BlockedRow(row.RowNumber, row.Row, string.Join("; ", reasons)));
            }
        }

        var cargosSheetIds = source.Cargos.Select(x => NormalizeId(Get(x, "ID_CARGO"))).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var cargosReferenciados = source.Colaboradores.Select(x => NormalizeId(Get(x.Row, "ID_CARGO"))).Where(x => x.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var importables = cargosSheetIds.Intersect(cargosReferenciados, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ValidationResult
        {
            Empresas = source.Empresas.Count,
            Departamentos = source.Departamentos.Count,
            CargosAnalizados = source.Cargos.Count,
            CargosImportables = importables.Count,
            ColaboradoresTotal = source.Colaboradores.Count,
            ReadyRows = ready,
            BlockedRows = blocked,
            CargosImportablesIds = importables
        };
    }

    private static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static string NormalizeId(string value)
    {
        value = value.Trim();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric) && numeric == decimal.Truncate(numeric))
        {
            return decimal.ToInt64(numeric).ToString(CultureInfo.InvariantCulture);
        }

        return value;
    }
}

internal sealed record BlockedRow(int RowNumber, Dictionary<string, string> Row, string Reason);
internal sealed record CatalogMaps(Dictionary<string, int> Empresas, Dictionary<string, int> Departamentos, Dictionary<string, int> Cargos);
