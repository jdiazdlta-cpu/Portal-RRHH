using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

var options = CliOptions.Parse(args);

if (!File.Exists(options.InputPath))
{
    Console.Error.WriteLine($"Input file not found: {options.InputPath}");
    return 2;
}

Directory.CreateDirectory(options.OutputDirectory);

if (options.Mode.Equals("normalized", StringComparison.OrdinalIgnoreCase)
    || options.Mode.Equals("validate", StringComparison.OrdinalIgnoreCase))
{
    var normalizedAnalyzer = new NormalizedMigrationAnalyzer(options.InputPath, options.OutputDirectory);
    var normalizedResult = normalizedAnalyzer.Run();

    Console.WriteLine("Normalized migration dry-run completed.");
    Console.WriteLine($"Source: {normalizedResult.SourceFile}");
    Console.WriteLine($"Output: {normalizedResult.OutputDirectory}");
    Console.WriteLine($"Companies: {normalizedResult.Empresas}");
    Console.WriteLine($"Departments: {normalizedResult.Departamentos}");
    Console.WriteLine($"Positions: {normalizedResult.Cargos}");
    Console.WriteLine($"Leaders: {normalizedResult.Lideres}");
    Console.WriteLine($"Collaborators: {normalizedResult.Colaboradores}");
    Console.WriteLine($"Ready: {normalizedResult.ReadyColaboradores}");
    Console.WriteLine($"Blocked: {normalizedResult.BlockedColaboradores}");
    Console.WriteLine($"With warnings: {normalizedResult.ColaboradoresConAdvertencias}");
}
else if (options.Mode.Equals("import-catalogs", StringComparison.OrdinalIgnoreCase)
    || options.Mode.Equals("import-colaboradores", StringComparison.OrdinalIgnoreCase)
    || options.Mode.Equals("assign-leaders", StringComparison.OrdinalIgnoreCase)
    || options.Mode.Equals("full-import", StringComparison.OrdinalIgnoreCase))
{
    var importRunner = new NormalizedImportRunner(options);
    var importResult = importRunner.Run();

    Console.WriteLine("Normalized import run completed.");
    Console.WriteLine($"Mode: {importResult.Mode}");
    Console.WriteLine($"Confirmed: {importResult.Confirmed}");
    Console.WriteLine($"Source: {importResult.SourceFile}");
    Console.WriteLine($"Output: {importResult.OutputDirectory}");
    Console.WriteLine($"Companies inserted: {importResult.EmpresasInsertadas}");
    Console.WriteLine($"Departments inserted: {importResult.DepartamentosInsertados}");
    Console.WriteLine($"Positions inserted: {importResult.CargosInsertados}");
    Console.WriteLine($"Collaborators inserted: {importResult.ColaboradoresInsertados}");
    Console.WriteLine($"Collaborators skipped existing: {importResult.ColaboradoresSaltadosExistentes}");
    Console.WriteLine($"Blocked collaborators not imported: {importResult.ColaboradoresBloqueadosNoImportados}");
    Console.WriteLine($"Leaders assigned: {importResult.JefesAsignados}");
    Console.WriteLine($"Leaders pending: {importResult.JefesPendientes}");
    Console.WriteLine($"Warnings: {importResult.Advertencias}");
    Console.WriteLine($"Errors: {importResult.Errores}");
}
else
{
    var analyzer = new MigrationAnalyzer(options.InputPath, options.OutputDirectory);
    var result = analyzer.Run();

    Console.WriteLine("Migration dry-run completed.");
    Console.WriteLine($"Source: {result.SourceFile}");
    Console.WriteLine($"Output: {result.OutputDirectory}");
    Console.WriteLine($"Rows read: {result.TotalRowsRead}");
    Console.WriteLine($"Companies: {result.UniqueCompanies}");
    Console.WriteLine($"Departments: {result.UniqueDepartments}");
    Console.WriteLine($"Positions: {result.UniquePositions}");
    Console.WriteLine($"Operational active: {result.OperationalActive}");
    Console.WriteLine($"Terminated: {result.Terminated}");
    Console.WriteLine($"Critical errors: {result.CriticalErrorCount}");
    Console.WriteLine($"Warnings: {result.WarningCount}");
}

return 0;

internal sealed class MigrationAnalyzer(string inputPath, string outputDirectory)
{
    private const string SheetName = "query";

    private static readonly string[] RelevantColumns =
    [
        "ID COLABORADOR",
        "CEDULA",
        "VCED",
        "SEGURO SOCIAL",
        "PNOMBRE",
        "SNOMBRE",
        "PAPELLIDO",
        "SAPELLIDO",
        "FECHA DE NACIMIENTO",
        "SEXO",
        "TELEFONO",
        "EMAIL",
        "DIRECCION",
        "EMPRESA",
        "DEPARTAMENTO",
        "CARGO",
        "FECHA DE INGRESO",
        "TIPO DE CONTRATO",
        "VENCIMIENTO DE CONTRATO",
        "ESTATUS",
        "SALARIO",
        "VIATICOS",
        "GASTOS DE REPRESENTACION",
        "FECHA DE SALIDA",
        "MOTIVO DE SALIDA",
        "VACANTE",
        "ULTIMA VACACIONES",
        "LICENCIA DE CONDUCIR",
        "TIPO DE LICENCIA",
        "VLIC",
        "LIDER INMEDIATO"
    ];

    private static readonly string[] IgnoredColumns =
    [
        "TIPO DE SANGRE",
        "NACIONALIDAD",
        "ESTADO CIVIL",
        "CUENTA BANCARIA",
        "ESTUDIOS ACADEMICOS",
        "HIJOS",
        "CANT HIJOS",
        "TALLA DE SUETER",
        "TALLA DE BOTAS",
        "TELEFONO CORP",
        "CARGO DE LIDER INMEDIATO",
        "Tipo de elemento",
        "Ruta de acceso"
    ];

    private static readonly string[] RequiredColumns =
    [
        "ID COLABORADOR",
        "CEDULA",
        "PNOMBRE",
        "PAPELLIDO",
        "EMPRESA",
        "DEPARTAMENTO",
        "CARGO",
        "FECHA DE INGRESO",
        "TIPO DE CONTRATO",
        "ESTATUS"
    ];

    private static readonly string[] DateColumns =
    [
        "FECHA DE NACIMIENTO",
        "VCED",
        "VLIC",
        "FECHA DE INGRESO",
        "VENCIMIENTO DE CONTRATO",
        "FECHA DE SALIDA",
        "ULTIMA VACACIONES"
    ];

    private readonly List<DiagnosticIssue> _errors = [];
    private readonly List<DiagnosticIssue> _warnings = [];
    private readonly Dictionary<string, CatalogPreviewRow> _companies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CatalogPreviewRow> _departments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CatalogPreviewRow> _positions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _companyCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _statusCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _contractCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _employeeIdRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _cedulaRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LeaderAccumulator> _leaders = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ImportedPerson> _people = [];
    private readonly List<MigrationRow> _rows = [];

    public MigrationSummary Run()
    {
        using var workbook = new XLWorkbook(inputPath);

        if (!workbook.Worksheets.TryGetWorksheet(SheetName, out var worksheet))
        {
            throw new InvalidOperationException($"Worksheet '{SheetName}' was not found.");
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            throw new InvalidOperationException($"Worksheet '{SheetName}' has no data.");
        }

        var headerRow = usedRange.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);
        var detectedColumns = headerMap
            .OrderBy(item => item.Value)
            .Select(item => item.Key)
            .ToList();

        var missingRelevantColumns = RelevantColumns
            .Where(column => !headerMap.ContainsKey(NormalizeKey(column)))
            .ToList();
        foreach (var missingColumn in missingRelevantColumns)
        {
            AddError(0, missingColumn, "MissingColumn", $"Column '{missingColumn}' was not found.", null, null);
        }

        var firstDataRow = headerRow.RowNumber() + 1;
        var lastRow = usedRange.LastRowUsed().RowNumber();

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (IsEmptyDataRow(row, headerMap))
            {
                continue;
            }

            AnalyzeRow(row, headerMap);
        }

        AddDuplicateDiagnostics("ID COLABORADOR", "DuplicateEmployeeId", _employeeIdRows, value => value);
        AddDuplicateDiagnostics("CEDULA", "DuplicateCedula", _cedulaRows, MaskSensitive);

        var leaderPreview = BuildLeaderPreview();
        var summary = BuildSummary(detectedColumns, missingRelevantColumns, leaderPreview);

        WriteSummary(summary);
        WriteIssues("migration-errors.csv", _errors);
        WriteIssues("migration-warnings.csv", _warnings);
        WriteCatalogPreview();
        WriteLeaderPreview(leaderPreview);

        return summary;
    }

    private void AnalyzeRow(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        var rowNumber = row.RowNumber();
        var employeeId = GetString(row, headerMap, "ID COLABORADOR");
        var cedula = GetString(row, headerMap, "CEDULA");
        var primerNombre = GetString(row, headerMap, "PNOMBRE");
        var segundoNombre = GetString(row, headerMap, "SNOMBRE");
        var primerApellido = GetString(row, headerMap, "PAPELLIDO");
        var segundoApellido = GetString(row, headerMap, "SAPELLIDO");
        var empresa = NormalizeDisplay(GetString(row, headerMap, "EMPRESA"));
        var departamento = NormalizeDisplay(GetString(row, headerMap, "DEPARTAMENTO"));
        var cargo = NormalizeDisplay(GetString(row, headerMap, "CARGO"));
        var rawContract = GetString(row, headerMap, "TIPO DE CONTRATO");
        var rawStatus = GetString(row, headerMap, "ESTATUS");
        var rawLicense = GetString(row, headerMap, "LICENCIA DE CONDUCIR");
        var rawVacante = GetString(row, headerMap, "VACANTE");
        var tipoLicencia = GetString(row, headerMap, "TIPO DE LICENCIA");
        var lider = NormalizeDisplay(GetString(row, headerMap, "LIDER INMEDIATO"));

        foreach (var requiredColumn in RequiredColumns.Where(column => headerMap.ContainsKey(NormalizeKey(column))))
        {
            if (string.IsNullOrWhiteSpace(GetString(row, headerMap, requiredColumn)))
            {
                AddError(rowNumber, requiredColumn, "RequiredFieldEmpty", $"{requiredColumn} is required.", employeeId, cedula);
            }
        }

        TrackValue(_employeeIdRows, NormalizeIdentifier(employeeId), rowNumber);
        TrackValue(_cedulaRows, NormalizeCedula(cedula), rowNumber);

        var contract = MapContract(rawContract);
        if (contract is null)
        {
            AddError(rowNumber, "TIPO DE CONTRATO", "InvalidContractType", $"Unknown contract type '{rawContract}'. Expected P or E.", employeeId, cedula);
        }
        else
        {
            Increment(_contractCounts, contract);
        }

        var status = MapStatus(rawStatus);
        if (status is null)
        {
            AddError(rowNumber, "ESTATUS", "InvalidStatus", $"Unknown status '{rawStatus}'. Expected A, C, V, S or SU.", employeeId, cedula);
        }
        else
        {
            Increment(_statusCounts, status);
        }

        foreach (var dateColumn in DateColumns.Where(column => headerMap.ContainsKey(NormalizeKey(column))))
        {
            var parsedDate = ParseDate(row.Cell(headerMap[NormalizeKey(dateColumn)]), out var dateError);
            if (!parsedDate.HasValue && dateError is not null)
            {
                AddError(rowNumber, dateColumn, "InvalidDate", dateError, employeeId, cedula);
            }
        }

        var contractExpiration = TryGetDate(row, headerMap, "VENCIMIENTO DE CONTRATO");
        if (contract == "Eventual" && contractExpiration is null)
        {
            AddWarning(rowNumber, "VENCIMIENTO DE CONTRATO", "EventualWithoutContractExpiration", "Eventual contract has no expiration date.", employeeId, cedula);
        }

        var fechaSalida = TryGetDate(row, headerMap, "FECHA DE SALIDA");
        if (status == "Cesante" && fechaSalida is null)
        {
            AddError(rowNumber, "FECHA DE SALIDA", "TerminatedWithoutExitDate", "Cesante status requires FECHA DE SALIDA for the current model.", employeeId, cedula);
        }
        else if (status != "Cesante" && fechaSalida is not null)
        {
            AddWarning(rowNumber, "FECHA DE SALIDA", "ExitDateOnNonTerminated", "FECHA DE SALIDA is present but ESTATUS is not Cesante.", employeeId, cedula);
        }

        var motivoSalida = GetString(row, headerMap, "MOTIVO DE SALIDA");
        if ((status == "Cesante" || fechaSalida is not null) && string.IsNullOrWhiteSpace(motivoSalida))
        {
            AddWarning(rowNumber, "MOTIVO DE SALIDA", "MissingExitReason", "Exit reason is missing for a terminated collaborator or a row with exit date.", employeeId, cedula);
        }

        ParseDecimal(row, headerMap, "SALARIO", employeeId, cedula);
        ParseDecimal(row, headerMap, "VIATICOS", employeeId, cedula);
        ParseDecimal(row, headerMap, "GASTOS DE REPRESENTACION", employeeId, cedula);

        var hasLicense = ParseBoolean(rawLicense, defaultValue: false, out var licenseWarning);
        if (licenseWarning is not null)
        {
            AddWarning(rowNumber, "LICENCIA DE CONDUCIR", "UnknownBoolean", licenseWarning, employeeId, cedula);
        }

        if (hasLicense)
        {
            AddWarning(rowNumber, "LICENCIA DE CONDUCIR", "MissingLicenseNumber", "NumeroLicencia is not available in the Excel export.", employeeId, cedula);

            if (string.IsNullOrWhiteSpace(tipoLicencia))
            {
                AddWarning(rowNumber, "TIPO DE LICENCIA", "MissingLicenseType", "TieneLicencia is SI but TIPO DE LICENCIA is empty.", employeeId, cedula);
            }

            if (TryGetDate(row, headerMap, "VLIC") is null)
            {
                AddWarning(rowNumber, "VLIC", "MissingLicenseExpiration", "TieneLicencia is SI but VLIC is empty or invalid.", employeeId, cedula);
            }
        }

        _ = ParseBoolean(rawVacante, defaultValue: false, out var vacanteWarning);
        if (vacanteWarning is not null)
        {
            AddWarning(rowNumber, "VACANTE", "UnknownBoolean", vacanteWarning, employeeId, cedula);
        }

        if (!string.IsNullOrWhiteSpace(empresa))
        {
            AddCatalog(_companies, "Empresa", empresa, null, null);
            Increment(_companyCounts, empresa);
        }

        if (!string.IsNullOrWhiteSpace(empresa) && !string.IsNullOrWhiteSpace(departamento))
        {
            AddCatalog(_departments, "Departamento", empresa, departamento, null);
        }

        if (!string.IsNullOrWhiteSpace(empresa) && !string.IsNullOrWhiteSpace(departamento) && !string.IsNullOrWhiteSpace(cargo))
        {
            AddCatalog(_positions, "Cargo", empresa, departamento, cargo);
        }

        var fullName = BuildFullName(primerNombre, segundoNombre, primerApellido, segundoApellido);
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            _people.Add(new ImportedPerson(rowNumber, NormalizeKey(fullName), fullName, employeeId));
        }

        if (!string.IsNullOrWhiteSpace(lider))
        {
            var leaderKey = NormalizeKey(lider);
            if (!_leaders.TryGetValue(leaderKey, out var leader))
            {
                leader = new LeaderAccumulator(lider);
                _leaders[leaderKey] = leader;
            }

            leader.Count++;
            leader.Rows.Add(rowNumber);
        }

        _rows.Add(new MigrationRow(rowNumber, employeeId, cedula, empresa, departamento, cargo, contract, status));
    }

    private MigrationSummary BuildSummary(
        IReadOnlyCollection<string> detectedColumns,
        IReadOnlyCollection<string> missingRelevantColumns,
        IReadOnlyCollection<LeaderPreviewRow> leaderPreview)
    {
        var ignoredDetectedColumns = IgnoredColumns
            .Where(column => detectedColumns.Contains(NormalizeKey(column), StringComparer.OrdinalIgnoreCase))
            .ToList();

        var duplicateEmployeeIds = BuildDuplicates(_employeeIdRows, value => value);
        var duplicateCedulas = BuildDuplicates(_cedulaRows, MaskSensitive);

        return new MigrationSummary
        {
            GeneratedAt = DateTimeOffset.Now,
            SourceFile = inputPath,
            SheetName = SheetName,
            OutputDirectory = outputDirectory,
            TotalRowsRead = _rows.Count,
            UniqueCompanies = _companies.Count,
            UniqueDepartments = _departments.Count,
            UniquePositions = _positions.Count,
            OperationalActive = _rows.Count(row => row.Status is "Activo" or "Vacaciones" or "Servicio"),
            Terminated = _rows.Count(row => row.Status == "Cesante"),
            CriticalErrorCount = _errors.Count,
            WarningCount = _warnings.Count,
            CountsByCompany = OrderedDictionary(_companyCounts),
            CountsByStatus = OrderedDictionary(_statusCounts),
            CountsByContractType = OrderedDictionary(_contractCounts),
            DuplicateEmployeeIds = duplicateEmployeeIds,
            DuplicateCedulas = duplicateCedulas,
            MissingRequiredFieldRows = _errors
                .Where(issue => issue.Code == "RequiredFieldEmpty")
                .Select(issue => issue.RowNumber)
                .Distinct()
                .Order()
                .ToList(),
            InvalidDateRows = _errors
                .Where(issue => issue.Code == "InvalidDate")
                .Select(issue => issue.RowNumber)
                .Distinct()
                .Order()
                .ToList(),
            EventualWithoutContractExpiration = _warnings.Count(issue => issue.Code == "EventualWithoutContractExpiration"),
            LicenseWarnings = _warnings.Count(issue => issue.Code is "MissingLicenseNumber" or "MissingLicenseType" or "MissingLicenseExpiration"),
            UnresolvedLeaderCount = leaderPreview.Count(row => row.MatchStatus is "Pending" or "Ambiguous"),
            IgnoredColumnsDetected = ignoredDetectedColumns,
            MissingRelevantColumns = missingRelevantColumns.OrderBy(column => column).ToList(),
            Reports = new Dictionary<string, string>
            {
                ["summary"] = Path.Combine(outputDirectory, "migration-summary.json"),
                ["errors"] = Path.Combine(outputDirectory, "migration-errors.csv"),
                ["warnings"] = Path.Combine(outputDirectory, "migration-warnings.csv"),
                ["catalogsPreview"] = Path.Combine(outputDirectory, "migration-catalogs-preview.csv"),
                ["leadersPreview"] = Path.Combine(outputDirectory, "migration-leaders-preview.csv")
            }
        };
    }

    private List<LeaderPreviewRow> BuildLeaderPreview()
    {
        var peopleByName = _people
            .GroupBy(person => person.NormalizedFullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        return _leaders
            .OrderBy(item => item.Value.DisplayName)
            .Select(item =>
            {
                if (peopleByName.TryGetValue(item.Key, out var exactMatches))
                {
                    return exactMatches.Count == 1
                        ? new LeaderPreviewRow(item.Value.DisplayName, item.Value.Count, "Exact", exactMatches[0].FullName, exactMatches[0].EmployeeId, JoinRows(item.Value.Rows))
                        : new LeaderPreviewRow(item.Value.DisplayName, item.Value.Count, "Ambiguous", string.Join(" | ", exactMatches.Select(match => match.FullName).Distinct()), null, JoinRows(item.Value.Rows));
                }

                var suggestions = SuggestLeaders(item.Key);
                return suggestions.Count == 0
                    ? new LeaderPreviewRow(item.Value.DisplayName, item.Value.Count, "Pending", null, null, JoinRows(item.Value.Rows))
                    : new LeaderPreviewRow(item.Value.DisplayName, item.Value.Count, "Suggested", string.Join(" | ", suggestions.Select(match => match.FullName)), null, JoinRows(item.Value.Rows));
            })
            .ToList();
    }

    private List<ImportedPerson> SuggestLeaders(string leaderKey)
    {
        var leaderTokens = leaderKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (leaderTokens.Count == 0)
        {
            return [];
        }

        return _people
            .Select(person => new
            {
                Person = person,
                Score = person.NormalizedFullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Count(token => leaderTokens.Contains(token))
            })
            .Where(item => item.Score >= 2)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Person.FullName)
            .Take(3)
            .Select(item => item.Person)
            .ToList();
    }

    private Dictionary<string, int> BuildHeaderMap(IXLRangeRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeKey(cell.GetFormattedString());
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            map[header] = cell.Address.ColumnNumber;
        }

        return map;
    }

    private bool IsEmptyDataRow(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        return RelevantColumns
            .Where(column => headerMap.ContainsKey(NormalizeKey(column)))
            .All(column => string.IsNullOrWhiteSpace(GetString(row, headerMap, column)));
    }

    private string GetString(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        var key = NormalizeKey(columnName);
        if (!headerMap.TryGetValue(key, out var columnNumber))
        {
            return string.Empty;
        }

        return NormalizeDisplay(row.Cell(columnNumber).GetFormattedString());
    }

    private DateTime? TryGetDate(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        var key = NormalizeKey(columnName);
        return !headerMap.TryGetValue(key, out var columnNumber)
            ? null
            : ParseDate(row.Cell(columnNumber), out _);
    }

    private void ParseDecimal(
        IXLRow row,
        IReadOnlyDictionary<string, int> headerMap,
        string columnName,
        string? employeeId,
        string? cedula)
    {
        var key = NormalizeKey(columnName);
        if (!headerMap.TryGetValue(key, out var columnNumber))
        {
            return;
        }

        var raw = GetString(row, headerMap, columnName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var cell = row.Cell(columnNumber);
        if (cell.TryGetValue<decimal>(out _))
        {
            return;
        }

        var normalized = raw
            .Replace("B/.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _)
            && !decimal.TryParse(raw, NumberStyles.Number | NumberStyles.Currency, CultureInfo.GetCultureInfo("es-PA"), out _))
        {
            AddError(row.RowNumber(), columnName, "InvalidDecimal", $"{columnName} is not a valid decimal value.", employeeId, cedula);
        }
    }

    private static DateTime? ParseDate(IXLCell cell, out string? error)
    {
        error = null;
        if (cell.IsEmpty() || string.IsNullOrWhiteSpace(cell.GetFormattedString()))
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var directDate))
        {
            return ValidateDate(directDate, cell.GetFormattedString(), out error);
        }

        if (cell.TryGetValue<double>(out var serial) && serial > 1 && serial < 80000)
        {
            try
            {
                return ValidateDate(DateTime.FromOADate(serial), cell.GetFormattedString(), out error);
            }
            catch (ArgumentException)
            {
                error = $"Excel serial date '{serial}' could not be converted.";
                return null;
            }
        }

        var raw = NormalizeDisplay(cell.GetFormattedString());
        if (double.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericSerial)
            && numericSerial > 1
            && numericSerial < 80000)
        {
            try
            {
                return ValidateDate(DateTime.FromOADate(numericSerial), raw, out error);
            }
            catch (ArgumentException)
            {
                error = $"Excel serial date '{raw}' could not be converted.";
                return null;
            }
        }

        var cultures = new[]
        {
            CultureInfo.GetCultureInfo("es-PA"),
            CultureInfo.GetCultureInfo("es-ES"),
            CultureInfo.InvariantCulture
        };

        var formats = new[]
        {
            "d/M/yyyy",
            "dd/MM/yyyy",
            "M/d/yyyy",
            "MM/dd/yyyy",
            "yyyy-MM-dd",
            "d-M-yyyy",
            "dd-MM-yyyy",
            "d/M/yy",
            "dd/MM/yy",
            "M/d/yy",
            "MM/dd/yy",
            "d-MMM-yyyy",
            "dd-MMM-yyyy"
        };

        foreach (var culture in cultures)
        {
            if (DateTime.TryParseExact(raw, formats, culture, DateTimeStyles.AssumeLocal, out var exactDate)
                || DateTime.TryParse(raw, culture, DateTimeStyles.AssumeLocal, out exactDate))
            {
                return ValidateDate(exactDate, raw, out error);
            }
        }

        error = $"Date value '{raw}' could not be converted.";
        return null;
    }

    private static DateTime? ValidateDate(DateTime value, string rawValue, out string? error)
    {
        error = null;
        if (value.Year is < 1900 or > 2100)
        {
            error = $"Date value '{rawValue}' is outside the accepted range 1900-2100.";
            return null;
        }

        return value.Date;
    }

    private static string? MapContract(string raw)
    {
        return NormalizeKey(raw) switch
        {
            "P" or "PERMANENTE" => "Permanente",
            "E" or "EVENTUAL" => "Eventual",
            _ => null
        };
    }

    private static string? MapStatus(string raw)
    {
        return NormalizeKey(raw) switch
        {
            "A" or "ACTIVO" => "Activo",
            "C" or "CESANTE" => "Cesante",
            "V" or "VACACIONES" => "Vacaciones",
            "S" or "SERVICIO" => "Servicio",
            "SU" or "SUSPENDIDO" => "Suspendido",
            _ => null
        };
    }

    private static bool ParseBoolean(string raw, bool defaultValue, out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return NormalizeKey(raw) switch
        {
            "SI" or "S" or "YES" or "Y" or "TRUE" or "1" => true,
            "NO" or "N" or "FALSE" or "0" => false,
            _ => WarnAndDefault(raw, defaultValue, out warning)
        };
    }

    private static bool WarnAndDefault(string raw, bool defaultValue, out string? warning)
    {
        warning = $"Boolean value '{raw}' is not recognized. Defaulted to {defaultValue}.";
        return defaultValue;
    }

    private void AddCatalog(
        IDictionary<string, CatalogPreviewRow> target,
        string catalogType,
        string empresa,
        string? departamento,
        string? cargo)
    {
        var key = string.Join("|", new[] { catalogType, empresa, departamento, cargo }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(NormalizeKey));

        if (!target.TryGetValue(key, out var row))
        {
            row = new CatalogPreviewRow(catalogType, empresa, departamento, cargo, 0);
            target[key] = row;
        }

        row.SourceRowsCount++;
    }

    private void AddDuplicateDiagnostics(
        string field,
        string code,
        IReadOnlyDictionary<string, List<int>> values,
        Func<string, string> displayValue)
    {
        foreach (var duplicate in values.Where(item => !string.IsNullOrWhiteSpace(item.Key) && item.Value.Count > 1))
        {
            var rows = JoinRows(duplicate.Value);
            AddError(
                0,
                field,
                code,
                $"{field} '{displayValue(duplicate.Key)}' appears in rows {rows}.",
                field == "CEDULA" ? null : duplicate.Key,
                field == "CEDULA" ? duplicate.Key : null);
        }
    }

    private static List<DuplicateSummary> BuildDuplicates(
        IReadOnlyDictionary<string, List<int>> values,
        Func<string, string> displayValue)
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item.Key) && item.Value.Count > 1)
            .OrderBy(item => item.Key)
            .Select(item => new DuplicateSummary(displayValue(item.Key), item.Value.Count, item.Value.Order().ToList()))
            .ToList();
    }

    private void AddError(int rowNumber, string field, string code, string message, string? employeeId, string? cedula)
    {
        _errors.Add(new DiagnosticIssue(rowNumber, "Critical", field, code, message, NormalizeDisplay(employeeId), MaskSensitive(cedula)));
    }

    private void AddWarning(int rowNumber, string field, string code, string message, string? employeeId, string? cedula)
    {
        _warnings.Add(new DiagnosticIssue(rowNumber, "Warning", field, code, message, NormalizeDisplay(employeeId), MaskSensitive(cedula)));
    }

    private static void TrackValue(IDictionary<string, List<int>> target, string value, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!target.TryGetValue(value, out var rows))
        {
            rows = [];
            target[value] = rows;
        }

        rows.Add(rowNumber);
    }

    private static void Increment(IDictionary<string, int> target, string key)
    {
        target[key] = target.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private void WriteSummary(MigrationSummary summary)
    {
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });

        File.WriteAllText(Path.Combine(outputDirectory, "migration-summary.json"), json, Encoding.UTF8);
    }

    private void WriteIssues(string fileName, IReadOnlyCollection<DiagnosticIssue> issues)
    {
        var rows = new List<string[]>
        {
            new[] { "RowNumber", "Severity", "Field", "Code", "Message", "NoEmpleado", "CedulaMasked" }
        };

        rows.AddRange(issues
            .OrderBy(issue => issue.RowNumber)
            .ThenBy(issue => issue.Field)
            .Select(issue => new[]
            {
                issue.RowNumber.ToString(CultureInfo.InvariantCulture),
                issue.Severity,
                issue.Field,
                issue.Code,
                issue.Message,
                issue.NoEmpleado ?? string.Empty,
                issue.CedulaMasked ?? string.Empty
            }));

        WriteCsv(Path.Combine(outputDirectory, fileName), rows);
    }

    private void WriteCatalogPreview()
    {
        var rows = new List<string[]>
        {
            new[] { "CatalogType", "Empresa", "Departamento", "Cargo", "SourceRowsCount" }
        };

        rows.AddRange(_companies.Values
            .Concat(_departments.Values)
            .Concat(_positions.Values)
            .OrderBy(row => row.CatalogType)
            .ThenBy(row => row.Empresa)
            .ThenBy(row => row.Departamento)
            .ThenBy(row => row.Cargo)
            .Select(row => new[]
            {
                row.CatalogType,
                row.Empresa,
                row.Departamento ?? string.Empty,
                row.Cargo ?? string.Empty,
                row.SourceRowsCount.ToString(CultureInfo.InvariantCulture)
            }));

        WriteCsv(Path.Combine(outputDirectory, "migration-catalogs-preview.csv"), rows);
    }

    private void WriteLeaderPreview(IReadOnlyCollection<LeaderPreviewRow> leaders)
    {
        var rows = new List<string[]>
        {
            new[] { "LiderInmediato", "Occurrences", "MatchStatus", "SuggestedMatch", "SuggestedNoEmpleado", "SourceRows" }
        };

        rows.AddRange(leaders.Select(row => new[]
        {
            row.LiderInmediato,
            row.Occurrences.ToString(CultureInfo.InvariantCulture),
            row.MatchStatus,
            row.SuggestedMatch ?? string.Empty,
            row.SuggestedNoEmpleado ?? string.Empty,
            row.SourceRows
        }));

        WriteCsv(Path.Combine(outputDirectory, "migration-leaders-preview.csv"), rows);
    }

    private static void WriteCsv(string path, IReadOnlyCollection<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }

    private static Dictionary<string, int> OrderedDictionary(IReadOnlyDictionary<string, int> source)
    {
        return source
            .OrderBy(item => item.Key)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string JoinRows(IEnumerable<int> rows)
    {
        return string.Join(";", rows.Order());
    }

    private static string BuildFullName(params string[] values)
    {
        return NormalizeDisplay(string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))));
    }

    private static string NormalizeDisplay(string? value)
    {
        return Regex.Replace(value?.Trim() ?? string.Empty, @"\s+", " ");
    }

    private static string NormalizeIdentifier(string? value)
    {
        return NormalizeDisplay(value).ToUpperInvariant();
    }

    private static string NormalizeCedula(string? value)
    {
        return Regex.Replace(NormalizeDisplay(value).ToUpperInvariant(), @"[\s-]", string.Empty);
    }

    private static string MaskSensitive(string? value)
    {
        var normalized = NormalizeCedula(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.Length <= 4
            ? new string('*', normalized.Length)
            : $"{new string('*', normalized.Length - 4)}{normalized[^4..]}";
    }

    private static string NormalizeKey(string? value)
    {
        var display = NormalizeDisplay(value);
        if (string.IsNullOrWhiteSpace(display))
        {
            return string.Empty;
        }

        var formD = display.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        var alphaNumeric = Regex.Replace(withoutDiacritics, @"[^A-Z0-9]+", " ");
        return Regex.Replace(alphaNumeric.Trim(), @"\s+", " ");
    }
}

internal sealed class NormalizedMigrationAnalyzer(string inputPath, string outputDirectory)
{
    private static readonly string[] ExpectedSheets =
    [
        "README",
        "Empresas",
        "Departamentos",
        "Cargos",
        "Lideres",
        "Colaboradores_Import",
        "Correcciones_Lider",
        "Validaciones",
        "Duplicados_ID",
        "Duplicados_CEDULA"
    ];

    private static readonly string[] ColaboradorColumns =
    [
        "ID COLABORADOR",
        "CEDULA",
        "VCED",
        "SEGURO SOCIAL",
        "PNOMBRE",
        "SNOMBRE",
        "PAPELLIDO",
        "SAPELLIDO",
        "FECHA DE NACIMIENTO",
        "SEXO",
        "TELEFONO",
        "EMAIL",
        "DIRECCION",
        "EMPRESA",
        "DEPARTAMENTO",
        "CARGO",
        "LIDER INMEDIATO",
        "FECHA DE INGRESO",
        "TIPO DE CONTRATO",
        "VENCIMIENTO DE CONTRATO",
        "ESTATUS",
        "SALARIO",
        "VIATICOS",
        "GASTOS DE REPRESENTACION",
        "FECHA DE SALIDA",
        "MOTIVO DE SALIDA",
        "VACANTE",
        "ULTIMA VACACIONES",
        "LICENCIA DE CONDUCIR",
        "TIPO DE LICENCIA",
        "VLIC",
        "ID_EMPRESA",
        "ID_DEPARTAMENTO",
        "ID_CARGO",
        "ID_LIDER_INMEDIATO",
        "LIDER_INMEDIATO_NORMALIZADO",
        "CARGO_DE_LIDER_CORREGIDO",
        "OBS_VALIDACION"
    ];

    private readonly List<NormalizedIssue> _errors = [];
    private readonly List<NormalizedIssue> _warnings = [];
    private readonly List<NormalizedIssue> _catalogIssues = [];
    private readonly List<NormalizedColaboradorResult> _collaborators = [];
    private readonly List<NormalizedLeaderPending> _leadersPending = [];
    private readonly Dictionary<string, string> _companyIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NormalizedDepartment> _departmentIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NormalizedPosition> _positionIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _collaboratorIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _duplicatedIdsFromSheet = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _duplicatedCedulasFromSheet = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _idsInCollaborators = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _cedulasInCollaborators = new(StringComparer.OrdinalIgnoreCase);
    private int _empresasCount;
    private int _departamentosCount;
    private int _cargosCount;
    private int _lideresCount;
    private int _colaboradoresCount;
    private int _validacionesCount;
    private int _duplicadosIdCount;
    private int _duplicadosCedulaCount;

    public NormalizedMigrationSummary Run()
    {
        var workbook = NormalizedWorkbook.Load(inputPath);

        foreach (var sheet in ExpectedSheets.Where(sheet => !workbook.Worksheets.Contains(sheet)))
        {
            AddCatalogIssue(sheet, 0, "Hoja", "MissingSheet", $"Expected worksheet '{sheet}' was not found.");
        }

        if (workbook.Worksheets.TryGetWorksheet("Empresas", out var empresasSheet))
        {
            AnalyzeEmpresas(empresasSheet);
        }

        if (workbook.Worksheets.TryGetWorksheet("Departamentos", out var departamentosSheet))
        {
            AnalyzeDepartamentos(departamentosSheet);
        }

        if (workbook.Worksheets.TryGetWorksheet("Cargos", out var cargosSheet))
        {
            AnalyzeCargos(cargosSheet);
        }

        _lideresCount = CountDataRows(workbook, "Lideres");
        _validacionesCount = CountDataRows(workbook, "Validaciones");
        _duplicadosIdCount = CountDataRows(workbook, "Duplicados_ID");
        _duplicadosCedulaCount = CountDataRows(workbook, "Duplicados_CEDULA");

        if (workbook.Worksheets.TryGetWorksheet("Duplicados_ID", out var duplicatedIdsSheet))
        {
            LoadDuplicateValues(duplicatedIdsSheet, "ID COLABORADOR", _duplicatedIdsFromSheet);
        }

        if (workbook.Worksheets.TryGetWorksheet("Duplicados_CEDULA", out var duplicatedCedulasSheet))
        {
            LoadDuplicateValues(duplicatedCedulasSheet, "CEDULA", _duplicatedCedulasFromSheet, normalizeCedula: true);
        }

        if (workbook.Worksheets.TryGetWorksheet("Colaboradores_Import", out var colaboradoresSheet))
        {
            AnalyzeColaboradoresFirstPass(colaboradoresSheet);
            AnalyzeColaboradoresSecondPass();
        }
        else
        {
            AddError("Colaboradores_Import", 0, "Hoja", "MissingSheet", "Expected worksheet 'Colaboradores_Import' was not found.", null, null, null);
        }

        var summary = BuildSummary(workbook);
        WriteReports(summary);

        return summary;
    }

    private void AnalyzeEmpresas(NormalizedSheet sheet)
    {
        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        ValidateColumns(sheet.Name, headers, ["ID_EMPRESA", "EMPRESA"]);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            _empresasCount++;
            var id = GetString(row, headers, "ID_EMPRESA");
            var empresa = GetString(row, headers, "EMPRESA");

            if (string.IsNullOrWhiteSpace(id))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_EMPRESA", "EmptyId", "ID_EMPRESA is empty.");
            }
            else if (!seen.TryAdd(id, rowNumber))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_EMPRESA", "DuplicateId", $"ID_EMPRESA '{id}' is duplicated. First row: {seen[id]}.");
            }
            else
            {
                _companyIds[id] = empresa;
            }

            if (string.IsNullOrWhiteSpace(empresa))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "EMPRESA", "EmptyName", "EMPRESA is empty.");
            }
        }
    }

    private void AnalyzeDepartamentos(NormalizedSheet sheet)
    {
        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        ValidateColumns(sheet.Name, headers, ["ID_DEPARTAMENTO", "ID_EMPRESA", "EMPRESA", "DEPARTAMENTO"]);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            _departamentosCount++;
            var id = GetString(row, headers, "ID_DEPARTAMENTO");
            var companyId = GetString(row, headers, "ID_EMPRESA");
            var empresa = GetString(row, headers, "EMPRESA");
            var departamento = GetString(row, headers, "DEPARTAMENTO");

            if (string.IsNullOrWhiteSpace(id))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_DEPARTAMENTO", "EmptyId", "ID_DEPARTAMENTO is empty.");
            }
            else if (!seen.TryAdd(id, rowNumber))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_DEPARTAMENTO", "DuplicateId", $"ID_DEPARTAMENTO '{id}' is duplicated. First row: {seen[id]}.");
            }

            if (string.IsNullOrWhiteSpace(companyId) || !_companyIds.ContainsKey(companyId))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_EMPRESA", "MissingCompany", $"ID_EMPRESA '{companyId}' does not exist in Empresas.");
            }

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(companyId))
            {
                _departmentIds[id] = new NormalizedDepartment(id, companyId, empresa, departamento);
            }
        }
    }

    private void AnalyzeCargos(NormalizedSheet sheet)
    {
        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        ValidateColumns(sheet.Name, headers, ["ID_CARGO", "ID_DEPARTAMENTO", "EMPRESA", "DEPARTAMENTO", "CARGO"]);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            _cargosCount++;
            var id = GetString(row, headers, "ID_CARGO");
            var departmentId = GetString(row, headers, "ID_DEPARTAMENTO");
            var empresa = GetString(row, headers, "EMPRESA");
            var departamento = GetString(row, headers, "DEPARTAMENTO");
            var cargo = GetString(row, headers, "CARGO");

            if (string.IsNullOrWhiteSpace(id))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_CARGO", "EmptyId", "ID_CARGO is empty.");
            }
            else if (!seen.TryAdd(id, rowNumber))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_CARGO", "DuplicateId", $"ID_CARGO '{id}' is duplicated. First row: {seen[id]}.");
            }

            if (string.IsNullOrWhiteSpace(departmentId) || !_departmentIds.ContainsKey(departmentId))
            {
                AddCatalogIssue(sheet.Name, rowNumber, "ID_DEPARTAMENTO", "MissingDepartment", $"ID_DEPARTAMENTO '{departmentId}' does not exist in Departamentos.");
            }

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(departmentId))
            {
                _positionIds[id] = new NormalizedPosition(id, departmentId, empresa, departamento, cargo);
            }
        }
    }

    private void AnalyzeColaboradoresFirstPass(NormalizedSheet sheet)
    {
        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        ValidateColumns(sheet.Name, headers, ColaboradorColumns);

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            _colaboradoresCount++;
            var noEmpleado = GetString(row, headers, "ID COLABORADOR");
            var cedula = GetString(row, headers, "CEDULA");
            TrackValue(_idsInCollaborators, noEmpleado, rowNumber);
            TrackValue(_cedulasInCollaborators, NormalizeCedula(cedula), rowNumber);

            if (!string.IsNullOrWhiteSpace(noEmpleado))
            {
                _collaboratorIds.Add(noEmpleado);
            }

            var result = new NormalizedColaboradorResult(
                rowNumber,
                noEmpleado,
                MaskSensitive(cedula),
                BuildFullName(
                    GetString(row, headers, "PNOMBRE"),
                    GetString(row, headers, "SNOMBRE"),
                    GetString(row, headers, "PAPELLIDO"),
                    GetString(row, headers, "SAPELLIDO")),
                GetString(row, headers, "ID_EMPRESA"),
                GetString(row, headers, "ID_DEPARTAMENTO"),
                GetString(row, headers, "ID_CARGO"),
                GetString(row, headers, "TIPO DE CONTRATO"),
                GetString(row, headers, "ESTATUS"));

            ValidateColaboradorRow(sheet.Name, row, headers, result);
            _collaborators.Add(result);
        }

        AddDuplicateBlockers("ID COLABORADOR", _idsInCollaborators, value => value);
        AddDuplicateBlockers("CEDULA", _cedulasInCollaborators, MaskSensitive);
    }

    private void AnalyzeColaboradoresSecondPass()
    {
        foreach (var result in _collaborators)
        {
            if (!string.IsNullOrWhiteSpace(result.PendingLeaderId)
                && !_collaboratorIds.Contains(result.PendingLeaderId))
            {
                AddWarning(
                    "Colaboradores_Import",
                    result.RowNumber,
                    "ID_LIDER_INMEDIATO",
                    "LeaderIdNotFound",
                    $"ID_LIDER_INMEDIATO '{result.PendingLeaderId}' does not match any imported ID COLABORADOR.",
                    result.NoEmpleado,
                    result.CedulaMasked,
                    result);

                _leadersPending.Add(new NormalizedLeaderPending(
                    result.RowNumber,
                    result.NoEmpleado,
                    result.PendingLeaderText,
                    result.PendingLeaderId,
                    "ID_LIDER_INMEDIATO not found among imported collaborators."));
            }
        }
    }

    private void ValidateColaboradorRow(
        string sheetName,
        NormalizedRow row,
        IReadOnlyDictionary<string, int> headers,
        NormalizedColaboradorResult result)
    {
        var noEmpleado = result.NoEmpleado;
        var cedulaMasked = result.CedulaMasked;
        var cedulaRaw = GetString(row, headers, "CEDULA");
        var tipoContrato = MapContract(GetString(row, headers, "TIPO DE CONTRATO"));
        var estatus = MapStatus(GetString(row, headers, "ESTATUS"));
        var idEmpresa = GetString(row, headers, "ID_EMPRESA");
        var idDepartamento = GetString(row, headers, "ID_DEPARTAMENTO");
        var idCargo = GetString(row, headers, "ID_CARGO");
        var leaderId = GetString(row, headers, "ID_LIDER_INMEDIATO");
        var leaderText = GetString(row, headers, "LIDER INMEDIATO");
        var obsValidacion = GetString(row, headers, "OBS_VALIDACION");

        result.MappedContract = tipoContrato;
        result.MappedStatus = estatus;
        result.PendingLeaderId = leaderId;
        result.PendingLeaderText = leaderText;

        AddBlockingIfEmpty(sheetName, row.RowNumber(), "ID COLABORADOR", noEmpleado, result);
        AddBlockingIfEmpty(sheetName, row.RowNumber(), "CEDULA", cedulaRaw, result);
        AddBlockingIfEmpty(sheetName, row.RowNumber(), "ID_EMPRESA", idEmpresa, result);
        AddBlockingIfEmpty(sheetName, row.RowNumber(), "ID_DEPARTAMENTO", idDepartamento, result);
        AddBlockingIfEmpty(sheetName, row.RowNumber(), "ID_CARGO", idCargo, result);
        AddBlockingIfEmpty(sheetName, row.RowNumber(), "FECHA DE INGRESO", GetString(row, headers, "FECHA DE INGRESO"), result);

        if (!string.IsNullOrWhiteSpace(idEmpresa) && !_companyIds.ContainsKey(idEmpresa))
        {
            AddError(sheetName, row.RowNumber(), "ID_EMPRESA", "CompanyIdNotFound", $"ID_EMPRESA '{idEmpresa}' does not exist in Empresas.", noEmpleado, cedulaMasked, result);
        }

        if (!string.IsNullOrWhiteSpace(idDepartamento) && !_departmentIds.ContainsKey(idDepartamento))
        {
            AddError(sheetName, row.RowNumber(), "ID_DEPARTAMENTO", "DepartmentIdNotFound", $"ID_DEPARTAMENTO '{idDepartamento}' does not exist in Departamentos.", noEmpleado, cedulaMasked, result);
        }

        if (!string.IsNullOrWhiteSpace(idCargo) && !_positionIds.ContainsKey(idCargo))
        {
            AddError(sheetName, row.RowNumber(), "ID_CARGO", "PositionIdNotFound", $"ID_CARGO '{idCargo}' does not exist in Cargos.", noEmpleado, cedulaMasked, result);
        }

        if (tipoContrato is null)
        {
            AddError(sheetName, row.RowNumber(), "TIPO DE CONTRATO", "InvalidContractType", "TIPO DE CONTRATO must be P or E.", noEmpleado, cedulaMasked, result);
        }

        if (estatus is null)
        {
            AddError(sheetName, row.RowNumber(), "ESTATUS", "InvalidStatus", "ESTATUS must be A, C, V, S or SU.", noEmpleado, cedulaMasked, result);
        }

        if (!string.IsNullOrWhiteSpace(noEmpleado) && _duplicatedIdsFromSheet.Contains(noEmpleado))
        {
            AddError(sheetName, row.RowNumber(), "ID COLABORADOR", "DuplicateIdFromSheet", "ID COLABORADOR is listed in Duplicados_ID.", noEmpleado, cedulaMasked, result);
        }

        var normalizedCedula = NormalizeCedula(cedulaRaw);
        if (!string.IsNullOrWhiteSpace(normalizedCedula) && _duplicatedCedulasFromSheet.Contains(normalizedCedula))
        {
            AddError(sheetName, row.RowNumber(), "CEDULA", "DuplicateCedulaFromSheet", "CEDULA is listed in Duplicados_CEDULA.", noEmpleado, cedulaMasked, result);
        }

        ValidateDateIfPresent(sheetName, row, headers, "VCED", result);
        ValidateDateIfPresent(sheetName, row, headers, "VLIC", result);
        ValidateDateIfPresent(sheetName, row, headers, "FECHA DE NACIMIENTO", result);
        ValidateDateIfPresent(sheetName, row, headers, "FECHA DE INGRESO", result, required: true);
        ValidateDateIfPresent(sheetName, row, headers, "VENCIMIENTO DE CONTRATO", result);
        ValidateDateIfPresent(sheetName, row, headers, "FECHA DE SALIDA", result);
        ValidateDateIfPresent(sheetName, row, headers, "ULTIMA VACACIONES", result);

        if (tipoContrato == "Eventual" && TryGetDate(row, headers, "VENCIMIENTO DE CONTRATO") is null)
        {
            AddWarning(sheetName, row.RowNumber(), "VENCIMIENTO DE CONTRATO", "EventualWithoutContractExpiration", "Eventual collaborator has no contract expiration date.", noEmpleado, cedulaMasked, result);
        }

        if (estatus == "Cesante" && TryGetDate(row, headers, "FECHA DE SALIDA") is null)
        {
            AddWarning(sheetName, row.RowNumber(), "FECHA DE SALIDA", "TerminatedWithoutExitDate", "Cesante collaborator has no FECHA DE SALIDA.", noEmpleado, cedulaMasked, result);
        }

        if (estatus == "Cesante" && string.IsNullOrWhiteSpace(GetString(row, headers, "MOTIVO DE SALIDA")))
        {
            AddWarning(sheetName, row.RowNumber(), "MOTIVO DE SALIDA", "TerminatedWithoutExitReason", "Cesante collaborator has no MOTIVO DE SALIDA.", noEmpleado, cedulaMasked, result);
        }

        var hasLicense = ParseBoolean(GetString(row, headers, "LICENCIA DE CONDUCIR"));
        if (hasLicense)
        {
            AddWarning(sheetName, row.RowNumber(), "LICENCIA DE CONDUCIR", "MissingLicenseNumber", "NumeroLicencia is not available in the normalized source.", noEmpleado, cedulaMasked, result);

            if (TryGetDate(row, headers, "VLIC") is null)
            {
                AddWarning(sheetName, row.RowNumber(), "VLIC", "LicenseWithoutExpiration", "TieneLicencia is SI but VLIC is empty or invalid.", noEmpleado, cedulaMasked, result);
            }

            if (string.IsNullOrWhiteSpace(GetString(row, headers, "TIPO DE LICENCIA")))
            {
                AddWarning(sheetName, row.RowNumber(), "TIPO DE LICENCIA", "LicenseWithoutType", "TieneLicencia is SI but TIPO DE LICENCIA is empty.", noEmpleado, cedulaMasked, result);
            }
        }

        if (!string.IsNullOrWhiteSpace(leaderText) && string.IsNullOrWhiteSpace(leaderId))
        {
            AddWarning(sheetName, row.RowNumber(), "ID_LIDER_INMEDIATO", "LeaderWithoutId", "LIDER INMEDIATO has text but no ID_LIDER_INMEDIATO.", noEmpleado, cedulaMasked, result);
            _leadersPending.Add(new NormalizedLeaderPending(row.RowNumber(), noEmpleado, leaderText, leaderId, "Leader text has no normalized ID."));
        }

        if (!string.IsNullOrWhiteSpace(obsValidacion))
        {
            AddWarning(sheetName, row.RowNumber(), "OBS_VALIDACION", "ValidationObservation", obsValidacion, noEmpleado, cedulaMasked, result);
        }
    }

    private void AddDuplicateBlockers(string field, IReadOnlyDictionary<string, List<int>> values, Func<string, string> display)
    {
        foreach (var duplicate in values.Where(item => !string.IsNullOrWhiteSpace(item.Key) && item.Value.Count > 1))
        {
            foreach (var rowNumber in duplicate.Value)
            {
                var result = _collaborators.FirstOrDefault(item => item.RowNumber == rowNumber);
                AddError(
                    "Colaboradores_Import",
                    rowNumber,
                    field,
                    field == "CEDULA" ? "DuplicateCedulaInCollaborators" : "DuplicateIdInCollaborators",
                    $"{field} '{display(duplicate.Key)}' is duplicated in Colaboradores_Import rows {JoinRows(duplicate.Value)}.",
                    result?.NoEmpleado,
                    result?.CedulaMasked,
                    result);
            }
        }
    }

    private NormalizedMigrationSummary BuildSummary(NormalizedWorkbook workbook)
    {
        var missingSheets = ExpectedSheets
            .Where(sheet => !workbook.Worksheets.Contains(sheet))
            .OrderBy(sheet => sheet)
            .ToList();

        return new NormalizedMigrationSummary
        {
            GeneratedAt = DateTimeOffset.Now,
            SourceFile = inputPath,
            OutputDirectory = outputDirectory,
            MissingSheets = missingSheets,
            Empresas = _empresasCount,
            Departamentos = _departamentosCount,
            Cargos = _cargosCount,
            Lideres = _lideresCount,
            Colaboradores = _colaboradoresCount,
            Validaciones = _validacionesCount,
            DuplicadosId = _duplicadosIdCount,
            DuplicadosCedula = _duplicadosCedulaCount,
            ReadyColaboradores = _collaborators.Count(item => !item.IsBlocked),
            BlockedColaboradores = _collaborators.Count(item => item.IsBlocked),
            ColaboradoresConAdvertencias = _collaborators.Count(item => item.WarningCodes.Count > 0),
            CriticalErrorCount = _errors.Count,
            WarningCount = _warnings.Count,
            CatalogValidationIssues = _catalogIssues.Count,
            ErrorCodes = CountByCode(_errors),
            WarningCodes = CountByCode(_warnings),
            Reports = new Dictionary<string, string>
            {
                ["summary"] = Path.Combine(outputDirectory, "normalized-summary.json"),
                ["errors"] = Path.Combine(outputDirectory, "normalized-errors.csv"),
                ["warnings"] = Path.Combine(outputDirectory, "normalized-warnings.csv"),
                ["readyColaboradores"] = Path.Combine(outputDirectory, "normalized-ready-colaboradores.csv"),
                ["blockedColaboradores"] = Path.Combine(outputDirectory, "normalized-blocked-colaboradores.csv"),
                ["catalogValidation"] = Path.Combine(outputDirectory, "normalized-catalog-validation.csv"),
                ["leadersPending"] = Path.Combine(outputDirectory, "normalized-leaders-pending.csv"),
                ["importPlan"] = Path.Combine(outputDirectory, "normalized-import-plan.md")
            }
        };
    }

    private void WriteReports(NormalizedMigrationSummary summary)
    {
        WriteJson(Path.Combine(outputDirectory, "normalized-summary.json"), summary);
        WriteIssues(Path.Combine(outputDirectory, "normalized-errors.csv"), _errors);
        WriteIssues(Path.Combine(outputDirectory, "normalized-warnings.csv"), _warnings);
        WriteCollaborators(Path.Combine(outputDirectory, "normalized-ready-colaboradores.csv"), _collaborators.Where(item => !item.IsBlocked));
        WriteCollaborators(Path.Combine(outputDirectory, "normalized-blocked-colaboradores.csv"), _collaborators.Where(item => item.IsBlocked));
        WriteIssues(Path.Combine(outputDirectory, "normalized-catalog-validation.csv"), _catalogIssues);
        WriteLeadersPending(Path.Combine(outputDirectory, "normalized-leaders-pending.csv"));
        WriteImportPlan(Path.Combine(outputDirectory, "normalized-import-plan.md"), summary);
    }

    private static void WriteJson(string path, object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private static void WriteIssues(string path, IEnumerable<NormalizedIssue> issues)
    {
        var rows = new List<string[]>
        {
            new[] { "Sheet", "RowNumber", "Severity", "Field", "Code", "Message", "NoEmpleado", "CedulaMasked" }
        };

        rows.AddRange(issues
            .OrderBy(issue => issue.Sheet)
            .ThenBy(issue => issue.RowNumber)
            .ThenBy(issue => issue.Code)
            .Select(issue => new[]
            {
                issue.Sheet,
                issue.RowNumber.ToString(CultureInfo.InvariantCulture),
                issue.Severity,
                issue.Field,
                issue.Code,
                issue.Message,
                issue.NoEmpleado ?? string.Empty,
                issue.CedulaMasked ?? string.Empty
            }));

        WriteCsv(path, rows);
    }

    private static void WriteCollaborators(string path, IEnumerable<NormalizedColaboradorResult> collaborators)
    {
        var rows = new List<string[]>
        {
            new[] { "RowNumber", "NoEmpleado", "CedulaMasked", "NombreCompleto", "ID_EMPRESA", "ID_DEPARTAMENTO", "ID_CARGO", "TipoContrato", "Estatus", "IsActiveImport", "BlockerCodes", "WarningCodes" }
        };

        rows.AddRange(collaborators
            .OrderBy(item => item.RowNumber)
            .Select(item => new[]
            {
                item.RowNumber.ToString(CultureInfo.InvariantCulture),
                item.NoEmpleado,
                item.CedulaMasked,
                item.NombreCompleto,
                item.IdEmpresa,
                item.IdDepartamento,
                item.IdCargo,
                item.MappedContract ?? string.Empty,
                item.MappedStatus ?? string.Empty,
                "true",
                string.Join(";", item.BlockerCodes.Distinct().Order()),
                string.Join(";", item.WarningCodes.Distinct().Order())
            }));

        WriteCsv(path, rows);
    }

    private void WriteLeadersPending(string path)
    {
        var rows = new List<string[]>
        {
            new[] { "RowNumber", "NoEmpleado", "LiderInmediato", "ID_LIDER_INMEDIATO", "Reason" }
        };

        rows.AddRange(_leadersPending
            .GroupBy(item => $"{item.RowNumber}|{item.NoEmpleado}|{item.IdLiderInmediato}|{item.Reason}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.RowNumber)
            .Select(item => new[]
            {
                item.RowNumber.ToString(CultureInfo.InvariantCulture),
                item.NoEmpleado,
                item.LiderInmediato,
                item.IdLiderInmediato,
                item.Reason
            }));

        WriteCsv(path, rows);
    }

    private static void WriteImportPlan(string path, NormalizedMigrationSummary summary)
    {
        var content = $"""
        # Plan de importacion normalizada Portal RRHH FZ

        Fuente principal: `{summary.SourceFile}`

        Esta etapa es diagnostico/dry-run. No se insertaron datos en SQL Server.

        ## Orden recomendado

        1. Importar Empresas desde hoja `Empresas`.
        2. Importar Departamentos desde hoja `Departamentos`, resolviendo `ID_EMPRESA`.
        3. Importar Cargos desde hoja `Cargos`, resolviendo `ID_DEPARTAMENTO`.
        4. Importar Colaboradores sin `JefeInmediatoId`, usando `IsActive = true` para todos.
        5. Segunda pasada de jefes inmediatos: buscar colaborador importado con `NoEmpleado = ID_LIDER_INMEDIATO` y asignar `JefeInmediatoId`.

        ## Reglas confirmadas

        - `TIPO DE CONTRATO`: `P` -> Permanente, `E` -> Eventual.
        - `ESTATUS`: `A` -> Activo, `C` -> Cesante, `V` -> Vacaciones, `S` -> Servicio, `SU` -> Suspendido.
        - `IsActive` debe importarse como `true` para todos los colaboradores.
        - Licencias con `SI` no deben inventar `NumeroLicencia`; queda `null`.
        - `ID_LIDER_INMEDIATO` se aplica solo en segunda pasada.

        ## Resultado del dry-run

        - Empresas: {summary.Empresas}
        - Departamentos: {summary.Departamentos}
        - Cargos: {summary.Cargos}
        - Lideres: {summary.Lideres}
        - Colaboradores: {summary.Colaboradores}
        - Listos para importar: {summary.ReadyColaboradores}
        - Bloqueados: {summary.BlockedColaboradores}
        - Con advertencias: {summary.ColaboradoresConAdvertencias}

        ## Antes de importar

        - Revisar `normalized-errors.csv`.
        - Resolver duplicados criticos por ID y cedula.
        - Revisar `normalized-leaders-pending.csv`.
        - Aceptar explicitamente si colaboradores con advertencias pueden importarse.
        """;

        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private void ValidateColumns(string sheetName, IReadOnlyDictionary<string, int> headers, IEnumerable<string> requiredColumns)
    {
        foreach (var column in requiredColumns.Where(column => !headers.ContainsKey(NormalizeKey(column))))
        {
            AddCatalogIssue(sheetName, 0, column, "MissingColumn", $"Column '{column}' was not found.");
        }
    }

    private void ValidateDateIfPresent(string sheetName, NormalizedRow row, IReadOnlyDictionary<string, int> headers, string columnName, NormalizedColaboradorResult result, bool required = false)
    {
        var raw = GetString(row, headers, columnName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (required)
            {
                AddError(sheetName, row.RowNumber(), columnName, "RequiredFieldEmpty", $"{columnName} is required.", result.NoEmpleado, result.CedulaMasked, result);
            }

            return;
        }

        if (TryGetDate(row, headers, columnName) is null)
        {
            AddError(sheetName, row.RowNumber(), columnName, "InvalidDate", $"{columnName} could not be converted to a valid date.", result.NoEmpleado, result.CedulaMasked, result);
        }
    }

    private void AddBlockingIfEmpty(string sheetName, int rowNumber, string field, string value, NormalizedColaboradorResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(sheetName, rowNumber, field, "RequiredFieldEmpty", $"{field} is required.", result.NoEmpleado, result.CedulaMasked, result);
        }
    }

    private void AddError(string sheet, int rowNumber, string field, string code, string message, string? noEmpleado, string? cedulaMasked, NormalizedColaboradorResult? result)
    {
        _errors.Add(new NormalizedIssue(sheet, rowNumber, "Critical", field, code, message, noEmpleado, cedulaMasked));
        result?.BlockerCodes.Add(code);
    }

    private void AddWarning(string sheet, int rowNumber, string field, string code, string message, string? noEmpleado, string? cedulaMasked, NormalizedColaboradorResult? result)
    {
        _warnings.Add(new NormalizedIssue(sheet, rowNumber, "Warning", field, code, message, noEmpleado, cedulaMasked));
        result?.WarningCodes.Add(code);
    }

    private void AddCatalogIssue(string sheet, int rowNumber, string field, string code, string message)
    {
        _catalogIssues.Add(new NormalizedIssue(sheet, rowNumber, "Catalog", field, code, message, null, null));
    }

    private static Dictionary<string, int> CountByCode(IEnumerable<NormalizedIssue> issues)
    {
        return issues
            .GroupBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static int CountDataRows(NormalizedWorkbook workbook, string sheetName)
    {
        if (!workbook.Worksheets.TryGetWorksheet(sheetName, out var sheet))
        {
            return 0;
        }

        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        var count = 0;
        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            if (!IsEmpty(sheet.Row(rowNumber), headers))
            {
                count++;
            }
        }

        return count;
    }

    private static void LoadDuplicateValues(NormalizedSheet sheet, string preferredColumn, ISet<string> target, bool normalizeCedula = false)
    {
        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        var preferredKey = NormalizeKey(preferredColumn);
        var columnNumber = headers.TryGetValue(preferredKey, out var exactColumn)
            ? exactColumn
            : headers.FirstOrDefault(item => item.Key.Contains(preferredKey, StringComparison.OrdinalIgnoreCase)).Value;

        if (columnNumber <= 0)
        {
            columnNumber = 1;
        }

        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var value = NormalizeDisplay(sheet.Row(rowNumber).Cell(columnNumber).GetFormattedString());
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(normalizeCedula ? NormalizeCedula(value) : value);
            }
        }
    }

    private static Dictionary<string, int> BuildHeaderMap(NormalizedSheet sheet, out int firstDataRow, out int lastRow)
    {
        var usedRange = sheet.RangeUsed();
        if (usedRange is null)
        {
            firstDataRow = 1;
            lastRow = 0;
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var headerRow = usedRange.FirstRowUsed();
        firstDataRow = headerRow.RowNumber() + 1;
        lastRow = usedRange.LastRowUsed().RowNumber();
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeKey(cell.GetFormattedString());
            if (!string.IsNullOrWhiteSpace(header))
            {
                map[header] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static bool IsEmpty(NormalizedRow row, IReadOnlyDictionary<string, int> headers)
    {
        return headers.Count == 0 || headers.Values.All(column => string.IsNullOrWhiteSpace(row.Cell(column).GetFormattedString()));
    }

    private static string GetString(NormalizedRow row, IReadOnlyDictionary<string, int> headers, string columnName)
    {
        return headers.TryGetValue(NormalizeKey(columnName), out var columnNumber)
            ? NormalizeDisplay(row.Cell(columnNumber).GetFormattedString())
            : string.Empty;
    }

    private static DateTime? TryGetDate(NormalizedRow row, IReadOnlyDictionary<string, int> headers, string columnName)
    {
        return headers.TryGetValue(NormalizeKey(columnName), out var columnNumber)
            ? ParseDate(row.Cell(columnNumber))
            : null;
    }

    private static DateTime? ParseDate(NormalizedCell cell)
    {
        if (cell.IsEmpty() || string.IsNullOrWhiteSpace(cell.GetFormattedString()))
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var directDate))
        {
            return IsReasonableDate(directDate) ? directDate.Date : null;
        }

        if (cell.TryGetValue<double>(out var serial) && serial > 1 && serial < 80000)
        {
            try
            {
                var date = DateTime.FromOADate(serial);
                return IsReasonableDate(date) ? date.Date : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        var raw = NormalizeDisplay(cell.GetFormattedString());
        var cultures = new[] { CultureInfo.GetCultureInfo("es-PA"), CultureInfo.GetCultureInfo("es-ES"), CultureInfo.InvariantCulture };

        if (double.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericSerial)
            && numericSerial > 1
            && numericSerial < 80000)
        {
            try
            {
                var date = DateTime.FromOADate(numericSerial);
                return IsReasonableDate(date) ? date.Date : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        foreach (var culture in cultures)
        {
            if (DateTime.TryParse(raw, culture, DateTimeStyles.AssumeLocal, out var parsed)
                && IsReasonableDate(parsed))
            {
                return parsed.Date;
            }
        }

        return null;
    }

    private static bool IsReasonableDate(DateTime value)
    {
        return value.Year is >= 1900 and <= 2100;
    }

    private static string? MapContract(string raw)
    {
        return NormalizeKey(raw) switch
        {
            "P" or "PERMANENTE" => "Permanente",
            "E" or "EVENTUAL" => "Eventual",
            _ => null
        };
    }

    private static string? MapStatus(string raw)
    {
        return NormalizeKey(raw) switch
        {
            "A" or "ACTIVO" => "Activo",
            "C" or "CESANTE" => "Cesante",
            "V" or "VACACIONES" => "Vacaciones",
            "S" or "SERVICIO" => "Servicio",
            "SU" or "SUSPENDIDO" => "Suspendido",
            _ => null
        };
    }

    private static bool ParseBoolean(string raw)
    {
        return NormalizeKey(raw) switch
        {
            "SI" or "S" or "YES" or "Y" or "TRUE" or "1" => true,
            _ => false
        };
    }

    private static void TrackValue(IDictionary<string, List<int>> target, string value, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!target.TryGetValue(value, out var rows))
        {
            rows = [];
            target[value] = rows;
        }

        rows.Add(rowNumber);
    }

    private static void WriteCsv(string path, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }

    private static string JoinRows(IEnumerable<int> rows)
    {
        return string.Join(";", rows.Order());
    }

    private static string BuildFullName(params string[] values)
    {
        return NormalizeDisplay(string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))));
    }

    private static string NormalizeCedula(string? value)
    {
        return Regex.Replace(NormalizeDisplay(value).ToUpperInvariant(), @"[\s-]", string.Empty);
    }

    private static string MaskSensitive(string? value)
    {
        var normalized = NormalizeCedula(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.Length <= 4
            ? new string('*', normalized.Length)
            : $"{new string('*', normalized.Length - 4)}{normalized[^4..]}";
    }

    private static string NormalizeDisplay(string? value)
    {
        return Regex.Replace(value?.Trim() ?? string.Empty, @"\s+", " ");
    }

    private static string NormalizeKey(string? value)
    {
        var display = NormalizeDisplay(value);
        if (string.IsNullOrWhiteSpace(display))
        {
            return string.Empty;
        }

        var formD = display.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();
    }
}

internal sealed class NormalizedWorkbook
{
    private readonly Dictionary<string, NormalizedSheet> _sheets;

    private NormalizedWorkbook(Dictionary<string, NormalizedSheet> sheets)
    {
        _sheets = sheets;
        Worksheets = new NormalizedWorksheetCollection(_sheets);
    }

    public NormalizedWorksheetCollection Worksheets { get; }

    public static NormalizedWorkbook Load(string path)
    {
        using var document = SpreadsheetDocument.Open(path, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not found.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(item => item.InnerText)
            .ToList() ?? [];
        var sheets = new Dictionary<string, NormalizedSheet>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? [])
        {
            if (sheet.Id?.Value is null || sheet.Name?.Value is null)
            {
                continue;
            }

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
            var rows = new List<NormalizedRow>();

            foreach (var openXmlRow in worksheetPart.Worksheet.Descendants<Row>())
            {
                var values = new Dictionary<int, string>();
                foreach (var openXmlCell in openXmlRow.Elements<Cell>())
                {
                    var columnIndex = GetColumnIndex(openXmlCell.CellReference?.Value);
                    if (columnIndex <= 0)
                    {
                        continue;
                    }

                    values[columnIndex] = GetCellText(openXmlCell, sharedStrings);
                }

                if (values.Count > 0)
                {
                    rows.Add(new NormalizedRow((int)(openXmlRow.RowIndex?.Value ?? 0), values));
                }
            }

            sheets[sheet.Name.Value] = new NormalizedSheet(sheet.Name.Value, rows);
        }

        return new NormalizedWorkbook(sheets);
    }

    private static string GetCellText(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        if (cell.DataType?.Value == CellValues.SharedString
            && int.TryParse(cell.CellValue?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex)
            && sharedIndex >= 0
            && sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        return cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var columnName = Regex.Replace(cellReference.ToUpperInvariant(), @"[^A-Z]", string.Empty);
        var sum = 0;

        foreach (var letter in columnName)
        {
            sum *= 26;
            sum += letter - 'A' + 1;
        }

        return sum;
    }
}

internal sealed class NormalizedWorksheetCollection(Dictionary<string, NormalizedSheet> sheets)
{
    public bool Contains(string name)
    {
        return sheets.ContainsKey(name);
    }

    public bool TryGetWorksheet(string name, out NormalizedSheet sheet)
    {
        return sheets.TryGetValue(name, out sheet!);
    }
}

internal sealed class NormalizedSheet(string name, IReadOnlyCollection<NormalizedRow> rows)
{
    private readonly Dictionary<int, NormalizedRow> _rowsByNumber = rows.ToDictionary(row => row.RowNumber());

    public string Name { get; } = name;
    public IReadOnlyCollection<NormalizedRow> Rows { get; } = rows;

    public NormalizedRange? RangeUsed()
    {
        var usedRows = Rows
            .Where(row => row.CellsUsed().Any(cell => !string.IsNullOrWhiteSpace(cell.GetFormattedString())))
            .OrderBy(row => row.RowNumber())
            .ToList();

        return usedRows.Count == 0 ? null : new NormalizedRange(usedRows);
    }

    public NormalizedRow Row(int rowNumber)
    {
        return _rowsByNumber.TryGetValue(rowNumber, out var row)
            ? row
            : new NormalizedRow(rowNumber, new Dictionary<int, string>());
    }
}

internal sealed class NormalizedRange(IReadOnlyList<NormalizedRow> rows)
{
    public NormalizedRow FirstRowUsed()
    {
        return rows.First();
    }

    public NormalizedRow LastRowUsed()
    {
        return rows.Last();
    }
}

internal sealed class NormalizedRow(int rowNumber, IReadOnlyDictionary<int, string> values)
{
    public int RowNumber()
    {
        return rowNumber;
    }

    public NormalizedCell Cell(int columnNumber)
    {
        return new NormalizedCell(columnNumber, values.TryGetValue(columnNumber, out var value) ? value : string.Empty);
    }

    public IEnumerable<NormalizedCell> CellsUsed()
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .OrderBy(item => item.Key)
            .Select(item => new NormalizedCell(item.Key, item.Value));
    }
}

internal sealed class NormalizedCell(int columnNumber, string value)
{
    public NormalizedCellAddress Address { get; } = new(columnNumber);

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public string GetFormattedString()
    {
        return value;
    }

    public bool TryGetValue<T>(out T typedValue)
    {
        object? parsed = null;
        var targetType = typeof(T);

        if (targetType == typeof(string))
        {
            parsed = value;
        }
        else if (targetType == typeof(double)
            && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            parsed = doubleValue;
        }
        else if (targetType == typeof(decimal)
            && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
        {
            parsed = decimalValue;
        }
        else if (targetType == typeof(DateTime)
            && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial > 1
            && serial < 80000)
        {
            try
            {
                parsed = DateTime.FromOADate(serial);
            }
            catch (ArgumentException)
            {
                parsed = null;
            }
        }

        if (parsed is T result)
        {
            typedValue = result;
            return true;
        }

        typedValue = default!;
        return false;
    }
}

internal sealed record NormalizedCellAddress(int ColumnNumber);

internal sealed class CliOptions
{
    public string InputPath { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public string Mode { get; init; } = "legacy";
    public string ConnectionName { get; init; } = "DefaultConnection";
    public bool Confirm { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var mode = GetOption(args, "--mode") ?? "legacy";
        var normalizedMode = IsNormalizedMode(mode);
        var importMode = IsImportMode(mode);
        var defaultInput = normalizedMode || importMode
            ? (File.Exists(@"C:\Temp\Dataverse_Import_Normalizado_v2.xlsx")
                ? @"C:\Temp\Dataverse_Import_Normalizado_v2.xlsx"
                : @"C:\Users\jo.diaz\Downloads\Dataverse_Import_Normalizado_v2.xlsx")
            : (File.Exists(@"C:\Temp\BD-FZ.xlsx")
                ? @"C:\Temp\BD-FZ.xlsx"
                : @"C:\Users\jo.diaz\Downloads\BD-FZ.xlsx");
        var defaultOutput = importMode
            ? Path.Combine(Environment.CurrentDirectory, "docs", "migration-import-report")
            : normalizedMode
            ? Path.Combine(Environment.CurrentDirectory, "docs", "migration-normalized-report")
            : Path.Combine(Environment.CurrentDirectory, "docs", "migration-report");

        var input = GetOption(args, "--source")
            ?? GetOption(args, "--input")
            ?? GetOption(args, "-i")
            ?? defaultInput;

        var output = GetOption(args, "--output")
            ?? GetOption(args, "-o")
            ?? defaultOutput;

        return new CliOptions
        {
            InputPath = Path.GetFullPath(input),
            OutputDirectory = Path.GetFullPath(output),
            Mode = mode,
            ConnectionName = GetOption(args, "--connectionName") ?? "DefaultConnection",
            Confirm = HasFlag(args, "--confirm")
        };
    }

    private static bool IsNormalizedMode(string mode)
    {
        return mode.Equals("normalized", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("validate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImportMode(string mode)
    {
        return mode.Equals("import-catalogs", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("import-colaboradores", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("assign-leaders", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("full-import", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return index + 1 < args.Count ? args[index + 1] : null;
        }

        return null;
    }

    private static bool HasFlag(IEnumerable<string> args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed record DiagnosticIssue(
    int RowNumber,
    string Severity,
    string Field,
    string Code,
    string Message,
    string? NoEmpleado,
    string? CedulaMasked);

internal sealed record NormalizedIssue(
    string Sheet,
    int RowNumber,
    string Severity,
    string Field,
    string Code,
    string Message,
    string? NoEmpleado,
    string? CedulaMasked);

internal sealed class NormalizedColaboradorResult(
    int rowNumber,
    string noEmpleado,
    string cedulaMasked,
    string nombreCompleto,
    string idEmpresa,
    string idDepartamento,
    string idCargo,
    string rawContract,
    string rawStatus)
{
    public int RowNumber { get; } = rowNumber;
    public string NoEmpleado { get; } = noEmpleado;
    public string CedulaMasked { get; } = cedulaMasked;
    public string NombreCompleto { get; } = nombreCompleto;
    public string IdEmpresa { get; } = idEmpresa;
    public string IdDepartamento { get; } = idDepartamento;
    public string IdCargo { get; } = idCargo;
    public string RawContract { get; } = rawContract;
    public string RawStatus { get; } = rawStatus;
    public string? MappedContract { get; set; }
    public string? MappedStatus { get; set; }
    public string PendingLeaderId { get; set; } = string.Empty;
    public string PendingLeaderText { get; set; } = string.Empty;
    public HashSet<string> BlockerCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> WarningCodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsBlocked => BlockerCodes.Count > 0;
}

internal sealed record NormalizedLeaderPending(
    int RowNumber,
    string NoEmpleado,
    string LiderInmediato,
    string IdLiderInmediato,
    string Reason);

internal sealed record NormalizedDepartment(
    string IdDepartamento,
    string IdEmpresa,
    string Empresa,
    string Departamento);

internal sealed record NormalizedPosition(
    string IdCargo,
    string IdDepartamento,
    string Empresa,
    string Departamento,
    string Cargo);

internal sealed record MigrationRow(
    int RowNumber,
    string EmployeeId,
    string Cedula,
    string Empresa,
    string Departamento,
    string Cargo,
    string? Contract,
    string? Status);

internal sealed record ImportedPerson(
    int RowNumber,
    string NormalizedFullName,
    string FullName,
    string EmployeeId);

internal sealed class LeaderAccumulator(string displayName)
{
    public string DisplayName { get; } = displayName;
    public int Count { get; set; }
    public List<int> Rows { get; } = [];
}

internal sealed record LeaderPreviewRow(
    string LiderInmediato,
    int Occurrences,
    string MatchStatus,
    string? SuggestedMatch,
    string? SuggestedNoEmpleado,
    string SourceRows);

internal sealed class CatalogPreviewRow(
    string catalogType,
    string empresa,
    string? departamento,
    string? cargo,
    int sourceRowsCount)
{
    public string CatalogType { get; } = catalogType;
    public string Empresa { get; } = empresa;
    public string? Departamento { get; } = departamento;
    public string? Cargo { get; } = cargo;
    public int SourceRowsCount { get; set; } = sourceRowsCount;
}

internal sealed record DuplicateSummary(
    string Value,
    int Count,
    IReadOnlyCollection<int> Rows);

internal sealed class NormalizedMigrationSummary
{
    public DateTimeOffset GeneratedAt { get; init; }
    public string SourceFile { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public IReadOnlyCollection<string> MissingSheets { get; init; } = [];
    public int Empresas { get; init; }
    public int Departamentos { get; init; }
    public int Cargos { get; init; }
    public int Lideres { get; init; }
    public int Colaboradores { get; init; }
    public int Validaciones { get; init; }
    public int DuplicadosId { get; init; }
    public int DuplicadosCedula { get; init; }
    public int ReadyColaboradores { get; init; }
    public int BlockedColaboradores { get; init; }
    public int ColaboradoresConAdvertencias { get; init; }
    public int CriticalErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int CatalogValidationIssues { get; init; }
    public IReadOnlyDictionary<string, int> ErrorCodes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> WarningCodes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, string> Reports { get; init; } = new Dictionary<string, string>();
}

internal sealed class MigrationSummary
{
    public DateTimeOffset GeneratedAt { get; init; }
    public string SourceFile { get; init; } = string.Empty;
    public string SheetName { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public int TotalRowsRead { get; init; }
    public int UniqueCompanies { get; init; }
    public int UniqueDepartments { get; init; }
    public int UniquePositions { get; init; }
    public int OperationalActive { get; init; }
    public int Terminated { get; init; }
    public int CriticalErrorCount { get; init; }
    public int WarningCount { get; init; }
    public IReadOnlyDictionary<string, int> CountsByCompany { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> CountsByStatus { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> CountsByContractType { get; init; } = new Dictionary<string, int>();
    public IReadOnlyCollection<DuplicateSummary> DuplicateEmployeeIds { get; init; } = [];
    public IReadOnlyCollection<DuplicateSummary> DuplicateCedulas { get; init; } = [];
    public IReadOnlyCollection<int> MissingRequiredFieldRows { get; init; } = [];
    public IReadOnlyCollection<int> InvalidDateRows { get; init; } = [];
    public int EventualWithoutContractExpiration { get; init; }
    public int LicenseWarnings { get; init; }
    public int UnresolvedLeaderCount { get; init; }
    public IReadOnlyCollection<string> IgnoredColumnsDetected { get; init; } = [];
    public IReadOnlyCollection<string> MissingRelevantColumns { get; init; } = [];
    public IReadOnlyDictionary<string, string> Reports { get; init; } = new Dictionary<string, string>();
}
