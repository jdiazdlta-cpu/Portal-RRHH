using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

internal sealed class NormalizedImportRunner(CliOptions options)
{
    private const string Actor = "MigrationAnalyzer";

    private readonly List<string[]> _catalogRows =
    [
        ["Stage", "Entity", "SourceId", "Name", "ParentSourceId", "DbId", "Action", "Message"]
    ];

    private readonly List<string[]> _collaboratorRows =
    [
        ["RowNumber", "NoEmpleado", "CedulaMasked", "NombreCompleto", "DbId", "Action", "Message"]
    ];

    private readonly List<string[]> _skippedRows =
    [
        ["Stage", "RowNumber", "NoEmpleado", "CedulaMasked", "Code", "Message"]
    ];

    private readonly List<string[]> _warningRows =
    [
        ["Stage", "RowNumber", "NoEmpleado", "CedulaMasked", "Code", "Message"]
    ];

    private readonly List<string[]> _leaderRows =
    [
        ["RowNumber", "NoEmpleado", "ID_LIDER_INMEDIATO", "Action", "Message"]
    ];

    private readonly List<string[]> _errorRows =
    [
        ["Stage", "RowNumber", "NoEmpleado", "CedulaMasked", "Code", "Message"]
    ];

    private readonly Dictionary<string, int> _empresaDbIdsBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _departamentoDbIdsBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _cargoDbIdsBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _colaboradorDbIdsByNoEmpleado = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _colaboradorDbIdsByCedula = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _blockedRows = [];

    private int _temporaryId = -1;
    private NormalizedMigrationSummary? _validationSummary;

    public ImportSummary Run()
    {
        Directory.CreateDirectory(options.OutputDirectory);

        var summary = new ImportSummary
        {
            GeneratedAt = DateTimeOffset.Now,
            SourceFile = options.InputPath,
            OutputDirectory = options.OutputDirectory,
            Mode = options.Mode,
            Confirmed = options.Confirm,
            ConnectionName = options.ConnectionName
        };

        RunValidation(summary);

        if (options.Mode.Equals("full-import", StringComparison.OrdinalIgnoreCase)
            && !options.Confirm)
        {
            AddWarning("full-import", 0, null, null, "ConfirmRequired", "full-import is running in simulation mode because --confirm was not provided.");
        }

        if (IsMutationMode(options.Mode) && !options.Confirm)
        {
            AddWarning(options.Mode, 0, null, null, "SimulationOnly", "No SQL Server changes will be made without --confirm.");
        }

        var workbook = NormalizedWorkbook.Load(options.InputPath);
        var connectionString = ReadConnectionString(options.ConnectionName);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        try
        {
            if (ShouldRunCatalogs(options.Mode))
            {
                ProcessCatalogs(workbook, connection, summary);
            }

            if (ShouldRunCollaborators(options.Mode))
            {
                ProcessCollaborators(workbook, connection, summary);
            }

            if (ShouldRunLeaders(options.Mode))
            {
                ProcessLeaders(workbook, connection, summary);
            }
        }
        catch (Exception exception)
        {
            AddError(options.Mode, 0, null, null, "ImportFailed", exception.Message);
            summary.Errores++;
        }

        summary.Advertencias = _warningRows.Count - 1;
        summary.Errores += _errorRows.Count - 1;
        summary.Reports = new Dictionary<string, string>
        {
            ["summary"] = Path.Combine(options.OutputDirectory, "import-summary.json"),
            ["catalogs"] = Path.Combine(options.OutputDirectory, "import-catalogs-result.csv"),
            ["colaboradores"] = Path.Combine(options.OutputDirectory, "import-colaboradores-result.csv"),
            ["skipped"] = Path.Combine(options.OutputDirectory, "import-skipped.csv"),
            ["warnings"] = Path.Combine(options.OutputDirectory, "import-warnings.csv"),
            ["leaders"] = Path.Combine(options.OutputDirectory, "import-leaders-result.csv"),
            ["errors"] = Path.Combine(options.OutputDirectory, "import-errors.csv")
        };

        WriteReports(summary);
        return summary;
    }

    private void RunValidation(ImportSummary summary)
    {
        var validationDirectory = Path.Combine(Path.GetTempPath(), "PortalRRHHFZ.MigrationAnalyzer", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(validationDirectory);

        try
        {
            var analyzer = new NormalizedMigrationAnalyzer(options.InputPath, validationDirectory);
            _validationSummary = analyzer.Run();
            summary.ColaboradoresBloqueadosNoImportados = _validationSummary.BlockedColaboradores;

            foreach (var item in _validationSummary.WarningCodes.OrderBy(item => item.Key))
            {
                AddWarning("validate", 0, null, null, item.Key, $"{item.Value} warning(s) detected during normalized validation.");
            }

            foreach (var item in _validationSummary.ErrorCodes.OrderBy(item => item.Key))
            {
                AddSkipped("validate", 0, null, null, item.Key, $"{item.Value} blocking validation issue(s) detected.");
            }

            var blockedPath = Path.Combine(validationDirectory, "normalized-blocked-colaboradores.csv");
            foreach (var rowNumber in ReadBlockedRows(blockedPath))
            {
                _blockedRows.Add(rowNumber);
            }
        }
        finally
        {
            TryDeleteDirectory(validationDirectory);
        }
    }

    private void ProcessCatalogs(NormalizedWorkbook workbook, SqlConnection connection, ImportSummary summary)
    {
        using var transaction = options.Confirm ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null;

        try
        {
            ImportEmpresas(workbook, connection, transaction, summary);
            ImportDepartamentos(workbook, connection, transaction, summary);
            ImportCargos(workbook, connection, transaction, summary);
            transaction?.Commit();
        }
        catch (Exception exception)
        {
            transaction?.Rollback();
            AddError("import-catalogs", 0, null, null, "CatalogImportFailed", exception.Message);
            throw;
        }
    }

    private void ImportEmpresas(NormalizedWorkbook workbook, SqlConnection connection, SqlTransaction? transaction, ImportSummary summary)
    {
        var existing = LoadIdByName(connection, transaction, "Empresas", "EmpresaId", "Nombre");
        if (!workbook.Worksheets.TryGetWorksheet("Empresas", out var sheet))
        {
            AddError("import-catalogs", 0, null, null, "MissingSheet", "Worksheet Empresas was not found.");
            return;
        }

        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            var sourceId = GetString(row, headers, "ID_EMPRESA");
            var name = GetString(row, headers, "EMPRESA");
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(name))
            {
                AddError("import-catalogs", rowNumber, null, null, "InvalidEmpresa", "ID_EMPRESA and EMPRESA are required.");
                continue;
            }

            var key = NormalizeKey(name);
            if (existing.TryGetValue(key, out var existingId))
            {
                _empresaDbIdsBySourceId[sourceId] = existingId;
                AddCatalog("import-catalogs", "Empresa", sourceId, name, string.Empty, existingId, "existing", "Empresa already exists.");
                continue;
            }

            if (!options.Confirm)
            {
                var simulatedId = NextTemporaryId();
                _empresaDbIdsBySourceId[sourceId] = simulatedId;
                existing[key] = simulatedId;
                summary.EmpresasSimuladas++;
                AddCatalog("import-catalogs", "Empresa", sourceId, name, string.Empty, simulatedId, "simulated-insert", "Empresa would be inserted with --confirm.");
                continue;
            }

            var id = ExecuteScalarInt(
                connection,
                transaction,
                """
                INSERT INTO Empresas (Nombre, Ruc, CreatedAt, CreatedBy, IsActive)
                OUTPUT INSERTED.EmpresaId
                VALUES (@Nombre, NULL, SYSUTCDATETIME(), @CreatedBy, 1);
                """,
                new Dictionary<string, object?> { ["@Nombre"] = name, ["@CreatedBy"] = Actor });

            _empresaDbIdsBySourceId[sourceId] = id;
            existing[key] = id;
            summary.EmpresasInsertadas++;
            AddCatalog("import-catalogs", "Empresa", sourceId, name, string.Empty, id, "inserted", "Empresa inserted.");
        }
    }

    private void ImportDepartamentos(NormalizedWorkbook workbook, SqlConnection connection, SqlTransaction? transaction, ImportSummary summary)
    {
        var existing = LoadDepartmentIds(connection, transaction);
        if (!workbook.Worksheets.TryGetWorksheet("Departamentos", out var sheet))
        {
            AddError("import-catalogs", 0, null, null, "MissingSheet", "Worksheet Departamentos was not found.");
            return;
        }

        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            var sourceId = GetString(row, headers, "ID_DEPARTAMENTO");
            var companySourceId = GetString(row, headers, "ID_EMPRESA");
            var name = GetString(row, headers, "DEPARTAMENTO");
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(companySourceId) || string.IsNullOrWhiteSpace(name))
            {
                AddError("import-catalogs", rowNumber, null, null, "InvalidDepartamento", "ID_DEPARTAMENTO, ID_EMPRESA and DEPARTAMENTO are required.");
                continue;
            }

            if (!_empresaDbIdsBySourceId.TryGetValue(companySourceId, out var empresaId))
            {
                AddError("import-catalogs", rowNumber, null, null, "EmpresaNotResolved", $"ID_EMPRESA '{companySourceId}' was not resolved.");
                continue;
            }

            var key = BuildScopedKey(empresaId, name);
            if (existing.TryGetValue(key, out var existingId))
            {
                _departamentoDbIdsBySourceId[sourceId] = existingId;
                AddCatalog("import-catalogs", "Departamento", sourceId, name, companySourceId, existingId, "existing", "Departamento already exists.");
                continue;
            }

            if (!options.Confirm)
            {
                var simulatedId = NextTemporaryId();
                _departamentoDbIdsBySourceId[sourceId] = simulatedId;
                existing[key] = simulatedId;
                summary.DepartamentosSimulados++;
                AddCatalog("import-catalogs", "Departamento", sourceId, name, companySourceId, simulatedId, "simulated-insert", "Departamento would be inserted with --confirm.");
                continue;
            }

            var id = ExecuteScalarInt(
                connection,
                transaction,
                """
                INSERT INTO Departamentos (EmpresaId, Nombre, CreatedAt, CreatedBy, IsActive)
                OUTPUT INSERTED.DepartamentoId
                VALUES (@EmpresaId, @Nombre, SYSUTCDATETIME(), @CreatedBy, 1);
                """,
                new Dictionary<string, object?> { ["@EmpresaId"] = empresaId, ["@Nombre"] = name, ["@CreatedBy"] = Actor });

            _departamentoDbIdsBySourceId[sourceId] = id;
            existing[key] = id;
            summary.DepartamentosInsertados++;
            AddCatalog("import-catalogs", "Departamento", sourceId, name, companySourceId, id, "inserted", "Departamento inserted.");
        }
    }

    private void ImportCargos(NormalizedWorkbook workbook, SqlConnection connection, SqlTransaction? transaction, ImportSummary summary)
    {
        var existing = LoadCargoIds(connection, transaction);
        if (!workbook.Worksheets.TryGetWorksheet("Cargos", out var sheet))
        {
            AddError("import-catalogs", 0, null, null, "MissingSheet", "Worksheet Cargos was not found.");
            return;
        }

        var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
        for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsEmpty(row, headers))
            {
                continue;
            }

            var sourceId = GetString(row, headers, "ID_CARGO");
            var departmentSourceId = GetString(row, headers, "ID_DEPARTAMENTO");
            var name = GetString(row, headers, "CARGO");
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(departmentSourceId) || string.IsNullOrWhiteSpace(name))
            {
                AddError("import-catalogs", rowNumber, null, null, "InvalidCargo", "ID_CARGO, ID_DEPARTAMENTO and CARGO are required.");
                continue;
            }

            if (!_departamentoDbIdsBySourceId.TryGetValue(departmentSourceId, out var departamentoId))
            {
                AddError("import-catalogs", rowNumber, null, null, "DepartamentoNotResolved", $"ID_DEPARTAMENTO '{departmentSourceId}' was not resolved.");
                continue;
            }

            var key = BuildScopedKey(departamentoId, name);
            if (existing.TryGetValue(key, out var existingId))
            {
                _cargoDbIdsBySourceId[sourceId] = existingId;
                AddCatalog("import-catalogs", "Cargo", sourceId, name, departmentSourceId, existingId, "existing", "Cargo already exists.");
                continue;
            }

            if (!options.Confirm)
            {
                var simulatedId = NextTemporaryId();
                _cargoDbIdsBySourceId[sourceId] = simulatedId;
                existing[key] = simulatedId;
                summary.CargosSimulados++;
                AddCatalog("import-catalogs", "Cargo", sourceId, name, departmentSourceId, simulatedId, "simulated-insert", "Cargo would be inserted with --confirm.");
                continue;
            }

            var id = ExecuteScalarInt(
                connection,
                transaction,
                """
                INSERT INTO Cargos (DepartamentoId, Nombre, CreatedAt, CreatedBy, IsActive)
                OUTPUT INSERTED.CargoId
                VALUES (@DepartamentoId, @Nombre, SYSUTCDATETIME(), @CreatedBy, 1);
                """,
                new Dictionary<string, object?> { ["@DepartamentoId"] = departamentoId, ["@Nombre"] = name, ["@CreatedBy"] = Actor });

            _cargoDbIdsBySourceId[sourceId] = id;
            existing[key] = id;
            summary.CargosInsertados++;
            AddCatalog("import-catalogs", "Cargo", sourceId, name, departmentSourceId, id, "inserted", "Cargo inserted.");
        }
    }

    private void ProcessCollaborators(NormalizedWorkbook workbook, SqlConnection connection, ImportSummary summary)
    {
        EnsureCatalogMapsForCollaborators(workbook, connection);
        LoadCollaboratorMaps(connection);

        var tipoContratoIds = LoadIdByName(connection, null, "TiposContrato", "TipoContratoId", "Nombre");
        var estatusIds = LoadEstatusIds(connection);
        var motivoSalidaIds = LoadIdByName(connection, null, "MotivosSalida", "MotivoSalidaId", "Nombre");
        var noAplicaMotivoId = motivoSalidaIds.TryGetValue(NormalizeKey("No aplica"), out var motivoNoAplica) ? motivoNoAplica : (int?)null;

        if (!workbook.Worksheets.TryGetWorksheet("Colaboradores_Import", out var sheet))
        {
            AddError("import-colaboradores", 0, null, null, "MissingSheet", "Worksheet Colaboradores_Import was not found.");
            return;
        }

        using var transaction = options.Confirm ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null;

        try
        {
            var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
            for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                if (IsEmpty(row, headers))
                {
                    continue;
                }

                var noEmpleado = GetString(row, headers, "ID COLABORADOR");
                var cedula = GetString(row, headers, "CEDULA");
                var cedulaMasked = MaskSensitive(cedula);
                var fullName = BuildFullName(
                    GetString(row, headers, "PNOMBRE"),
                    GetString(row, headers, "SNOMBRE"),
                    GetString(row, headers, "PAPELLIDO"),
                    GetString(row, headers, "SAPELLIDO"));

                if (_blockedRows.Contains(rowNumber))
                {
                    AddSkipped("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "BlockedValidation", "Collaborator is blocked by normalized validation and was not imported.");
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, null, "blocked", "Blocked by validation.");
                    continue;
                }

                if (_colaboradorDbIdsByNoEmpleado.ContainsKey(noEmpleado)
                    || _colaboradorDbIdsByCedula.ContainsKey(NormalizeCedula(cedula)))
                {
                    summary.ColaboradoresSaltadosExistentes++;
                    AddSkipped("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "SkippedExisting", "Collaborator already exists by NoEmpleado or Cedula.");
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, null, "skipped-existing", "Collaborator already exists by NoEmpleado or Cedula.");
                    continue;
                }

                var idEmpresa = GetString(row, headers, "ID_EMPRESA");
                var idDepartamento = GetString(row, headers, "ID_DEPARTAMENTO");
                var idCargo = GetString(row, headers, "ID_CARGO");
                if (!_empresaDbIdsBySourceId.TryGetValue(idEmpresa, out var empresaId)
                    || !_departamentoDbIdsBySourceId.TryGetValue(idDepartamento, out var departamentoId)
                    || !_cargoDbIdsBySourceId.TryGetValue(idCargo, out var cargoId))
                {
                    AddError("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "CatalogNotResolved", "Empresa, Departamento or Cargo source ID was not resolved.");
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, null, "error", "Catalog source ID was not resolved.");
                    continue;
                }

                var contratoNombre = MapContract(GetString(row, headers, "TIPO DE CONTRATO"));
                var estatusCodigo = NormalizeKey(GetString(row, headers, "ESTATUS"));
                var estatusNombre = MapStatus(GetString(row, headers, "ESTATUS"));
                if (contratoNombre is null
                    || !tipoContratoIds.TryGetValue(NormalizeKey(contratoNombre), out var tipoContratoId)
                    || estatusNombre is null
                    || !estatusIds.TryGetValue(estatusCodigo, out var estatusId))
                {
                    AddError("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "CatalogValueNotResolved", "TipoContrato or Estatus could not be resolved.");
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, null, "error", "TipoContrato or Estatus could not be resolved.");
                    continue;
                }

                var fechaIngreso = TryGetDate(row, headers, "FECHA DE INGRESO");
                if (fechaIngreso is null)
                {
                    AddError("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "MissingFechaIngreso", "FECHA DE INGRESO could not be resolved.");
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, null, "error", "FECHA DE INGRESO could not be resolved.");
                    continue;
                }

                var motivoSalidaId = ResolveMotivoSalida(row, headers, motivoSalidaIds, noAplicaMotivoId, estatusNombre, rowNumber, noEmpleado, cedulaMasked);
                AddImportWarnings(row, headers, rowNumber, noEmpleado, cedulaMasked, contratoNombre, estatusNombre);

                if (!options.Confirm)
                {
                    var simulatedId = NextTemporaryId();
                    _colaboradorDbIdsByNoEmpleado[noEmpleado] = simulatedId;
                    _colaboradorDbIdsByCedula[NormalizeCedula(cedula)] = simulatedId;
                    summary.ColaboradoresSimulados++;
                    AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, simulatedId, "simulated-insert", "Collaborator would be inserted with --confirm.");
                    continue;
                }

                var hasLicense = ParseBoolean(GetString(row, headers, "LICENCIA DE CONDUCIR"));
                var id = ExecuteScalarInt(
                    connection,
                    transaction,
                    """
                    INSERT INTO Colaboradores
                    (
                        NoEmpleado, Cedula, FechaVencimientoCedula, SeguroSocial,
                        PrimerNombre, SegundoNombre, PrimerApellido, SegundoApellido,
                        Sexo, Telefono, Email, FechaNacimiento, Direccion,
                        EmpresaId, DepartamentoId, CargoId, JefeInmediatoId,
                        FechaIngreso, TipoContratoId, FechaVencimientoContrato, FechaVencimientoPeriodoProbatorio,
                        TieneLicencia, NumeroLicencia, TipoLicencia, FechaVencimientoLicencia,
                        EstatusId, Salario, Viaticos, GastosRepresentacion,
                        FechaSalida, MotivoSalidaId, Vacante, UltimaVacacion,
                        CreatedAt, CreatedBy, IsActive
                    )
                    OUTPUT INSERTED.ColaboradorId
                    VALUES
                    (
                        @NoEmpleado, @Cedula, @FechaVencimientoCedula, @SeguroSocial,
                        @PrimerNombre, @SegundoNombre, @PrimerApellido, @SegundoApellido,
                        @Sexo, @Telefono, @Email, @FechaNacimiento, @Direccion,
                        @EmpresaId, @DepartamentoId, @CargoId, NULL,
                        @FechaIngreso, @TipoContratoId, @FechaVencimientoContrato, NULL,
                        @TieneLicencia, NULL, @TipoLicencia, @FechaVencimientoLicencia,
                        @EstatusId, @Salario, @Viaticos, @GastosRepresentacion,
                        @FechaSalida, @MotivoSalidaId, @Vacante, @UltimaVacacion,
                        SYSUTCDATETIME(), @CreatedBy, 1
                    );
                    """,
                    new Dictionary<string, object?>
                    {
                        ["@NoEmpleado"] = noEmpleado,
                        ["@Cedula"] = cedula,
                        ["@FechaVencimientoCedula"] = TryGetDate(row, headers, "VCED"),
                        ["@SeguroSocial"] = NullIfEmpty(GetString(row, headers, "SEGURO SOCIAL")),
                        ["@PrimerNombre"] = GetString(row, headers, "PNOMBRE"),
                        ["@SegundoNombre"] = NullIfEmpty(GetString(row, headers, "SNOMBRE")),
                        ["@PrimerApellido"] = GetString(row, headers, "PAPELLIDO"),
                        ["@SegundoApellido"] = NullIfEmpty(GetString(row, headers, "SAPELLIDO")),
                        ["@Sexo"] = NullIfEmpty(GetString(row, headers, "SEXO")),
                        ["@Telefono"] = NullIfEmpty(GetString(row, headers, "TELEFONO")),
                        ["@Email"] = NullIfEmpty(GetString(row, headers, "EMAIL")),
                        ["@FechaNacimiento"] = TryGetDate(row, headers, "FECHA DE NACIMIENTO"),
                        ["@Direccion"] = NullIfEmpty(GetString(row, headers, "DIRECCION")),
                        ["@EmpresaId"] = empresaId,
                        ["@DepartamentoId"] = departamentoId,
                        ["@CargoId"] = cargoId,
                        ["@FechaIngreso"] = fechaIngreso.Value,
                        ["@TipoContratoId"] = tipoContratoId,
                        ["@FechaVencimientoContrato"] = TryGetDate(row, headers, "VENCIMIENTO DE CONTRATO"),
                        ["@TieneLicencia"] = hasLicense,
                        ["@TipoLicencia"] = hasLicense ? NullIfEmpty(GetString(row, headers, "TIPO DE LICENCIA")) : null,
                        ["@FechaVencimientoLicencia"] = hasLicense ? TryGetDate(row, headers, "VLIC") : null,
                        ["@EstatusId"] = estatusId,
                        ["@Salario"] = TryGetDecimal(row, headers, "SALARIO"),
                        ["@Viaticos"] = TryGetDecimal(row, headers, "VIATICOS"),
                        ["@GastosRepresentacion"] = TryGetDecimal(row, headers, "GASTOS DE REPRESENTACION"),
                        ["@FechaSalida"] = TryGetDate(row, headers, "FECHA DE SALIDA"),
                        ["@MotivoSalidaId"] = motivoSalidaId,
                        ["@Vacante"] = ParseBoolean(GetString(row, headers, "VACANTE")),
                        ["@UltimaVacacion"] = TryGetDate(row, headers, "ULTIMA VACACIONES"),
                        ["@CreatedBy"] = Actor
                    });

                _colaboradorDbIdsByNoEmpleado[noEmpleado] = id;
                _colaboradorDbIdsByCedula[NormalizeCedula(cedula)] = id;
                summary.ColaboradoresInsertados++;
                AddCollaborator(rowNumber, noEmpleado, cedulaMasked, fullName, id, "inserted", "Collaborator inserted.");
            }

            transaction?.Commit();
        }
        catch (Exception exception)
        {
            transaction?.Rollback();
            AddError("import-colaboradores", 0, null, null, "CollaboratorImportFailed", exception.Message);
            throw;
        }
    }

    private void ProcessLeaders(NormalizedWorkbook workbook, SqlConnection connection, ImportSummary summary)
    {
        LoadCollaboratorMaps(connection, preserveExisting: !options.Confirm && _colaboradorDbIdsByNoEmpleado.Count > 0);

        if (!workbook.Worksheets.TryGetWorksheet("Colaboradores_Import", out var sheet))
        {
            AddError("assign-leaders", 0, null, null, "MissingSheet", "Worksheet Colaboradores_Import was not found.");
            return;
        }

        using var transaction = options.Confirm ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null;

        try
        {
            var headers = BuildHeaderMap(sheet, out var firstDataRow, out var lastRow);
            for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                if (IsEmpty(row, headers) || _blockedRows.Contains(rowNumber))
                {
                    continue;
                }

                var noEmpleado = GetString(row, headers, "ID COLABORADOR");
                var leaderNoEmpleado = GetString(row, headers, "ID_LIDER_INMEDIATO");
                if (string.IsNullOrWhiteSpace(leaderNoEmpleado))
                {
                    continue;
                }

                if (string.Equals(noEmpleado, leaderNoEmpleado, StringComparison.OrdinalIgnoreCase))
                {
                    summary.JefesPendientes++;
                    AddLeader(rowNumber, noEmpleado, leaderNoEmpleado, "pending-self", "JefeInmediatoId was not assigned because it points to the same collaborator.");
                    continue;
                }

                if (!_colaboradorDbIdsByNoEmpleado.TryGetValue(noEmpleado, out var colaboradorId)
                    || !_colaboradorDbIdsByNoEmpleado.TryGetValue(leaderNoEmpleado, out var leaderId))
                {
                    summary.JefesPendientes++;
                    AddLeader(rowNumber, noEmpleado, leaderNoEmpleado, "pending", "Collaborator or leader does not exist in SQL Server yet.");
                    continue;
                }

                if (!options.Confirm)
                {
                    summary.JefesSimulados++;
                    AddLeader(rowNumber, noEmpleado, leaderNoEmpleado, "simulated-assign", "JefeInmediatoId would be assigned with --confirm.");
                    continue;
                }

                var affected = ExecuteNonQuery(
                    connection,
                    transaction,
                    """
                    UPDATE Colaboradores
                    SET JefeInmediatoId = @JefeInmediatoId,
                        UpdatedAt = SYSUTCDATETIME(),
                        UpdatedBy = @UpdatedBy
                    WHERE ColaboradorId = @ColaboradorId
                      AND ColaboradorId <> @JefeInmediatoId;
                    """,
                    new Dictionary<string, object?>
                    {
                        ["@JefeInmediatoId"] = leaderId,
                        ["@ColaboradorId"] = colaboradorId,
                        ["@UpdatedBy"] = Actor
                    });

                if (affected > 0)
                {
                    summary.JefesAsignados++;
                    AddLeader(rowNumber, noEmpleado, leaderNoEmpleado, "assigned", "JefeInmediatoId assigned.");
                }
            }

            transaction?.Commit();
        }
        catch (Exception exception)
        {
            transaction?.Rollback();
            AddError("assign-leaders", 0, null, null, "AssignLeadersFailed", exception.Message);
            throw;
        }
    }

    private void EnsureCatalogMapsForCollaborators(NormalizedWorkbook workbook, SqlConnection connection)
    {
        if (_empresaDbIdsBySourceId.Count > 0
            && _departamentoDbIdsBySourceId.Count > 0
            && _cargoDbIdsBySourceId.Count > 0)
        {
            return;
        }

        var companyByName = LoadIdByName(connection, null, "Empresas", "EmpresaId", "Nombre");
        var departmentByKey = LoadDepartmentIds(connection, null);
        var cargoByKey = LoadCargoIds(connection, null);

        if (workbook.Worksheets.TryGetWorksheet("Empresas", out var empresas))
        {
            var headers = BuildHeaderMap(empresas, out var firstDataRow, out var lastRow);
            for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = empresas.Row(rowNumber);
                var sourceId = GetString(row, headers, "ID_EMPRESA");
                var name = GetString(row, headers, "EMPRESA");
                if (!string.IsNullOrWhiteSpace(sourceId) && companyByName.TryGetValue(NormalizeKey(name), out var id))
                {
                    _empresaDbIdsBySourceId[sourceId] = id;
                }
            }
        }

        if (workbook.Worksheets.TryGetWorksheet("Departamentos", out var departamentos))
        {
            var headers = BuildHeaderMap(departamentos, out var firstDataRow, out var lastRow);
            for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = departamentos.Row(rowNumber);
                var sourceId = GetString(row, headers, "ID_DEPARTAMENTO");
                var companySourceId = GetString(row, headers, "ID_EMPRESA");
                var name = GetString(row, headers, "DEPARTAMENTO");
                if (!string.IsNullOrWhiteSpace(sourceId)
                    && _empresaDbIdsBySourceId.TryGetValue(companySourceId, out var companyId)
                    && departmentByKey.TryGetValue(BuildScopedKey(companyId, name), out var id))
                {
                    _departamentoDbIdsBySourceId[sourceId] = id;
                }
            }
        }

        if (workbook.Worksheets.TryGetWorksheet("Cargos", out var cargos))
        {
            var headers = BuildHeaderMap(cargos, out var firstDataRow, out var lastRow);
            for (var rowNumber = firstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = cargos.Row(rowNumber);
                var sourceId = GetString(row, headers, "ID_CARGO");
                var departmentSourceId = GetString(row, headers, "ID_DEPARTAMENTO");
                var name = GetString(row, headers, "CARGO");
                if (!string.IsNullOrWhiteSpace(sourceId)
                    && _departamentoDbIdsBySourceId.TryGetValue(departmentSourceId, out var departmentId)
                    && cargoByKey.TryGetValue(BuildScopedKey(departmentId, name), out var id))
                {
                    _cargoDbIdsBySourceId[sourceId] = id;
                }
            }
        }
    }

    private void LoadCollaboratorMaps(SqlConnection connection, bool preserveExisting = false)
    {
        if (!preserveExisting)
        {
            _colaboradorDbIdsByNoEmpleado.Clear();
            _colaboradorDbIdsByCedula.Clear();
        }

        using var command = new SqlCommand("SELECT ColaboradorId, NoEmpleado, Cedula FROM Colaboradores;", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var noEmpleado = reader.GetString(1);
            var cedula = reader.GetString(2);
            if (!string.IsNullOrWhiteSpace(noEmpleado))
            {
                _colaboradorDbIdsByNoEmpleado[noEmpleado] = id;
            }

            var normalizedCedula = NormalizeCedula(cedula);
            if (!string.IsNullOrWhiteSpace(normalizedCedula))
            {
                _colaboradorDbIdsByCedula[normalizedCedula] = id;
            }
        }
    }

    private int? ResolveMotivoSalida(
        NormalizedRow row,
        IReadOnlyDictionary<string, int> headers,
        IReadOnlyDictionary<string, int> motivoSalidaIds,
        int? noAplicaMotivoId,
        string estatusNombre,
        int rowNumber,
        string noEmpleado,
        string cedulaMasked)
    {
        var motivo = GetString(row, headers, "MOTIVO DE SALIDA");
        if (string.IsNullOrWhiteSpace(motivo))
        {
            if (estatusNombre == "Cesante")
            {
                AddWarning("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "TerminatedWithoutExitReason", "Cesante without MOTIVO DE SALIDA will use MotivoSalida = No aplica.");
                return noAplicaMotivoId;
            }

            return null;
        }

        if (motivoSalidaIds.TryGetValue(NormalizeKey(motivo), out var id))
        {
            return id;
        }

        AddWarning("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "ExitReasonNotFound", $"MOTIVO DE SALIDA '{motivo}' does not exist in SQL Server; No aplica will be used when available.");
        return noAplicaMotivoId;
    }

    private void AddImportWarnings(
        NormalizedRow row,
        IReadOnlyDictionary<string, int> headers,
        int rowNumber,
        string noEmpleado,
        string cedulaMasked,
        string contratoNombre,
        string estatusNombre)
    {
        if (contratoNombre == "Eventual" && TryGetDate(row, headers, "VENCIMIENTO DE CONTRATO") is null)
        {
            AddWarning("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "EventualWithoutContractExpiration", "Eventual collaborator has no contract expiration date; no fake date will be used.");
        }

        if (estatusNombre == "Cesante" && TryGetDate(row, headers, "FECHA DE SALIDA") is null)
        {
            AddWarning("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "TerminatedWithoutExitDate", "Cesante collaborator has no FECHA DE SALIDA; import is allowed.");
        }

        if (ParseBoolean(GetString(row, headers, "LICENCIA DE CONDUCIR")))
        {
            AddWarning("import-colaboradores", rowNumber, noEmpleado, cedulaMasked, "MissingLicenseNumber", "NumeroLicencia will be imported as null because the source does not provide it.");
        }
    }

    private static Dictionary<string, int> LoadIdByName(SqlConnection connection, SqlTransaction? transaction, string table, string idColumn, string nameColumn)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var command = CreateCommand(connection, transaction, $"SELECT {idColumn}, {nameColumn} FROM {table};");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[NormalizeKey(reader.GetString(1))] = reader.GetInt32(0);
        }

        return result;
    }

    private static Dictionary<string, int> LoadDepartmentIds(SqlConnection connection, SqlTransaction? transaction)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var command = CreateCommand(connection, transaction, "SELECT DepartamentoId, EmpresaId, Nombre FROM Departamentos;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[BuildScopedKey(reader.GetInt32(1), reader.GetString(2))] = reader.GetInt32(0);
        }

        return result;
    }

    private static Dictionary<string, int> LoadCargoIds(SqlConnection connection, SqlTransaction? transaction)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var command = CreateCommand(connection, transaction, "SELECT CargoId, DepartamentoId, Nombre FROM Cargos;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[BuildScopedKey(reader.GetInt32(1), reader.GetString(2))] = reader.GetInt32(0);
        }

        return result;
    }

    private static Dictionary<string, int> LoadEstatusIds(SqlConnection connection)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var command = new SqlCommand("SELECT EstatusId, Codigo, Nombre FROM EstatusColaborador;", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[NormalizeKey(reader.GetString(1))] = reader.GetInt32(0);
            result[NormalizeKey(reader.GetString(2))] = reader.GetInt32(0);
        }

        return result;
    }

    private static string ReadConnectionString(string connectionName)
    {
        foreach (var candidate in GetAppSettingsCandidates())
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(candidate));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
                && connectionStrings.TryGetProperty(connectionName, out var connectionString)
                && !string.IsNullOrWhiteSpace(connectionString.GetString()))
            {
                return connectionString.GetString()!;
            }
        }

        throw new InvalidOperationException($"Connection string '{connectionName}' was not found in PortalRRHHFZ.Api appsettings files.");
    }

    private static IEnumerable<string> GetAppSettingsCandidates()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            yield return Path.Combine(current.FullName, "backend", "src", "PortalRRHHFZ.Api", "appsettings.Development.json");
            yield return Path.Combine(current.FullName, "backend", "src", "PortalRRHHFZ.Api", "appsettings.json");
            yield return Path.Combine(current.FullName, "src", "PortalRRHHFZ.Api", "appsettings.Development.json");
            yield return Path.Combine(current.FullName, "src", "PortalRRHHFZ.Api", "appsettings.json");
            current = current.Parent;
        }
    }

    private static IEnumerable<int> ReadBlockedRows(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var firstComma = line.IndexOf(',', StringComparison.Ordinal);
            var firstValue = firstComma >= 0 ? line[..firstComma] : line;
            if (int.TryParse(firstValue.Trim('"'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber))
            {
                yield return rowNumber;
            }
        }
    }

    private void WriteReports(ImportSummary summary)
    {
        WriteJson(Path.Combine(options.OutputDirectory, "import-summary.json"), summary);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-catalogs-result.csv"), _catalogRows);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-colaboradores-result.csv"), _collaboratorRows);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-skipped.csv"), _skippedRows);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-warnings.csv"), _warningRows);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-leaders-result.csv"), _leaderRows);
        WriteCsv(Path.Combine(options.OutputDirectory, "import-errors.csv"), _errorRows);
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

    private static void WriteCsv(string path, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private void AddCatalog(string stage, string entity, string sourceId, string name, string parentSourceId, int dbId, string action, string message)
    {
        _catalogRows.Add([stage, entity, sourceId, name, parentSourceId, dbId.ToString(CultureInfo.InvariantCulture), action, message]);
    }

    private void AddCollaborator(int rowNumber, string noEmpleado, string cedulaMasked, string nombreCompleto, int? dbId, string action, string message)
    {
        _collaboratorRows.Add([rowNumber.ToString(CultureInfo.InvariantCulture), noEmpleado, cedulaMasked, nombreCompleto, dbId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, action, message]);
    }

    private void AddSkipped(string stage, int rowNumber, string? noEmpleado, string? cedulaMasked, string code, string message)
    {
        _skippedRows.Add([stage, rowNumber.ToString(CultureInfo.InvariantCulture), noEmpleado ?? string.Empty, cedulaMasked ?? string.Empty, code, message]);
    }

    private void AddWarning(string stage, int rowNumber, string? noEmpleado, string? cedulaMasked, string code, string message)
    {
        _warningRows.Add([stage, rowNumber.ToString(CultureInfo.InvariantCulture), noEmpleado ?? string.Empty, cedulaMasked ?? string.Empty, code, message]);
    }

    private void AddLeader(int rowNumber, string noEmpleado, string idLiderInmediato, string action, string message)
    {
        _leaderRows.Add([rowNumber.ToString(CultureInfo.InvariantCulture), noEmpleado, idLiderInmediato, action, message]);
    }

    private void AddError(string stage, int rowNumber, string? noEmpleado, string? cedulaMasked, string code, string message)
    {
        _errorRows.Add([stage, rowNumber.ToString(CultureInfo.InvariantCulture), noEmpleado ?? string.Empty, cedulaMasked ?? string.Empty, code, message]);
    }

    private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction? transaction, string commandText, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        var command = new SqlCommand(commandText, connection, transaction);
        if (parameters is null)
        {
            return command;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
        }

        return command;
    }

    private static int ExecuteScalarInt(SqlConnection connection, SqlTransaction? transaction, string commandText, IReadOnlyDictionary<string, object?> parameters)
    {
        using var command = CreateCommand(connection, transaction, commandText, parameters);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static int ExecuteNonQuery(SqlConnection connection, SqlTransaction? transaction, string commandText, IReadOnlyDictionary<string, object?> parameters)
    {
        using var command = CreateCommand(connection, transaction, commandText, parameters);
        return command.ExecuteNonQuery();
    }

    private int NextTemporaryId()
    {
        return _temporaryId--;
    }

    private static bool ShouldRunCatalogs(string mode)
    {
        return mode.Equals("import-catalogs", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("full-import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRunCollaborators(string mode)
    {
        return mode.Equals("import-colaboradores", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("full-import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRunLeaders(string mode)
    {
        return mode.Equals("assign-leaders", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("full-import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMutationMode(string mode)
    {
        return ShouldRunCatalogs(mode) || ShouldRunCollaborators(mode) || ShouldRunLeaders(mode);
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

    private static decimal? TryGetDecimal(NormalizedRow row, IReadOnlyDictionary<string, int> headers, string columnName)
    {
        var raw = GetString(row, headers, columnName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw
            .Replace("B/.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var invariantValue))
        {
            return invariantValue;
        }

        return decimal.TryParse(raw, NumberStyles.Number | NumberStyles.Currency, CultureInfo.GetCultureInfo("es-PA"), out var localValue)
            ? localValue
            : null;
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

    private static string BuildFullName(params string[] values)
    {
        return NormalizeDisplay(string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))));
    }

    private static string BuildScopedKey(int parentId, string name)
    {
        return $"{parentId}|{NormalizeKey(name)}";
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
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

        var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        var alphaNumeric = Regex.Replace(withoutDiacritics, @"[^A-Z0-9]+", " ");
        return Regex.Replace(alphaNumeric.Trim(), @"\s+", " ");
    }

    private static bool IsReasonableDate(DateTime value)
    {
        return value.Year is >= 1900 and <= 2100;
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed class ImportSummary
{
    public DateTimeOffset GeneratedAt { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
    public string ConnectionName { get; set; } = "DefaultConnection";
    public int EmpresasInsertadas { get; set; }
    public int DepartamentosInsertados { get; set; }
    public int CargosInsertados { get; set; }
    public int ColaboradoresInsertados { get; set; }
    public int EmpresasSimuladas { get; set; }
    public int DepartamentosSimulados { get; set; }
    public int CargosSimulados { get; set; }
    public int ColaboradoresSimulados { get; set; }
    public int ColaboradoresSaltadosExistentes { get; set; }
    public int ColaboradoresBloqueadosNoImportados { get; set; }
    public int JefesAsignados { get; set; }
    public int JefesSimulados { get; set; }
    public int JefesPendientes { get; set; }
    public int Advertencias { get; set; }
    public int Errores { get; set; }
    public Dictionary<string, string> Reports { get; set; } = [];
}
