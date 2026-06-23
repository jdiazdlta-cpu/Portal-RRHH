using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalRRHHFZ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccionesPersonal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccionesPersonal",
                columns: table => new
                {
                    AccionPersonalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    TipoAccion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: true),
                    FechaEfectiva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Justificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NombreColaboradorSnapshot = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    NoEmpleadoSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CedulaSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmpresaActualId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoActualId = table.Column<int>(type: "int", nullable: true),
                    CargoActualId = table.Column<int>(type: "int", nullable: true),
                    JefeActualId = table.Column<int>(type: "int", nullable: true),
                    TipoContratoActualId = table.Column<int>(type: "int", nullable: true),
                    EstatusActualId = table.Column<int>(type: "int", nullable: true),
                    SalarioActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ViaticosActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GastosRepresentacionActual = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiasVacaciones = table.Column<int>(type: "int", nullable: true),
                    FechaInicioVacaciones = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinVacaciones = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodoVacacionesDesde = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodoVacacionesHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuienReemplaza = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    TipoContratoNuevoId = table.Column<int>(type: "int", nullable: true),
                    FechaInicioContrato = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinContrato = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EsReemplazo = table.Column<bool>(type: "bit", nullable: true),
                    EsPosicionNueva = table.Column<bool>(type: "bit", nullable: true),
                    SalarioNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ViaticosNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GastosRepresentacionNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OtrosBeneficios = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SalarioAnterior = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioNuevoAjuste = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AjustePorMes = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MotivoAjuste = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CargoNuevoId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoNuevoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaNuevaId = table.Column<int>(type: "int", nullable: true),
                    JefeNuevoId = table.Column<int>(type: "int", nullable: true),
                    CargoTrasladoActualId = table.Column<int>(type: "int", nullable: true),
                    CargoTrasladoNuevoId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoTrasladoActualId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoTrasladoNuevoId = table.Column<int>(type: "int", nullable: true),
                    EmpresaTrasladoActualId = table.Column<int>(type: "int", nullable: true),
                    EmpresaTrasladoNuevaId = table.Column<int>(type: "int", nullable: true),
                    JefeTrasladoNuevoId = table.Column<int>(type: "int", nullable: true),
                    TipoLicenciaAccion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LicenciaRemunerada = table.Column<bool>(type: "bit", nullable: true),
                    FechaInicioLicencia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinLicencia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EspecificacionLicencia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TipoFinalizacion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FechaSalida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoSalidaId = table.Column<int>(type: "int", nullable: true),
                    MenosDeDosAnios = table.Column<bool>(type: "bit", nullable: true),
                    TerminacionPeriodoPrueba = table.Column<bool>(type: "bit", nullable: true),
                    CausaJustificada = table.Column<bool>(type: "bit", nullable: true),
                    MutuoAcuerdo = table.Column<bool>(type: "bit", nullable: true),
                    RenovacionExtensionContrato = table.Column<bool>(type: "bit", nullable: true),
                    ContinuidadLaboral = table.Column<bool>(type: "bit", nullable: true),
                    LoRecomienda = table.Column<bool>(type: "bit", nullable: true),
                    Puntualidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Honestidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TrabajoEquipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Productividad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Iniciativa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RespetoJefe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RespetoCompaneros = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ejecutada = table.Column<bool>(type: "bit", nullable: false),
                    FechaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EjecutadaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ResultadoEjecucion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorEjecucion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccionesPersonal", x => x.AccionPersonalId);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Cargos_CargoActualId",
                        column: x => x.CargoActualId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Cargos_CargoNuevoId",
                        column: x => x.CargoNuevoId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Cargos_CargoTrasladoActualId",
                        column: x => x.CargoTrasladoActualId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Cargos_CargoTrasladoNuevoId",
                        column: x => x.CargoTrasladoNuevoId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Colaboradores_JefeActualId",
                        column: x => x.JefeActualId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Colaboradores_JefeNuevoId",
                        column: x => x.JefeNuevoId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Colaboradores_JefeTrasladoNuevoId",
                        column: x => x.JefeTrasladoNuevoId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Departamentos_DepartamentoActualId",
                        column: x => x.DepartamentoActualId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Departamentos_DepartamentoNuevoId",
                        column: x => x.DepartamentoNuevoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Departamentos_DepartamentoTrasladoActualId",
                        column: x => x.DepartamentoTrasladoActualId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Departamentos_DepartamentoTrasladoNuevoId",
                        column: x => x.DepartamentoTrasladoNuevoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Empresas_EmpresaActualId",
                        column: x => x.EmpresaActualId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Empresas_EmpresaNuevaId",
                        column: x => x.EmpresaNuevaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Empresas_EmpresaTrasladoActualId",
                        column: x => x.EmpresaTrasladoActualId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Empresas_EmpresaTrasladoNuevaId",
                        column: x => x.EmpresaTrasladoNuevaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_EstatusColaborador_EstatusActualId",
                        column: x => x.EstatusActualId,
                        principalTable: "EstatusColaborador",
                        principalColumn: "EstatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_MotivosSalida_MotivoSalidaId",
                        column: x => x.MotivoSalidaId,
                        principalTable: "MotivosSalida",
                        principalColumn: "MotivoSalidaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "SolicitudId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_TiposContrato_TipoContratoActualId",
                        column: x => x.TipoContratoActualId,
                        principalTable: "TiposContrato",
                        principalColumn: "TipoContratoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_TiposContrato_TipoContratoNuevoId",
                        column: x => x.TipoContratoNuevoId,
                        principalTable: "TiposContrato",
                        principalColumn: "TipoContratoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionesPersonal_Usuarios_EjecutadaPorUsuarioId",
                        column: x => x.EjecutadaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccionPersonalCambiosAplicados",
                columns: table => new
                {
                    AccionPersonalCambioAplicadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccionPersonalId = table.Column<int>(type: "int", nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccionPersonalCambiosAplicados", x => x.AccionPersonalCambioAplicadoId);
                    table.ForeignKey(
                        name: "FK_AccionPersonalCambiosAplicados_AccionesPersonal_AccionPersonalId",
                        column: x => x.AccionPersonalId,
                        principalTable: "AccionesPersonal",
                        principalColumn: "AccionPersonalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccionPersonalCambiosAplicados_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_CargoActualId",
                table: "AccionesPersonal",
                column: "CargoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_CargoNuevoId",
                table: "AccionesPersonal",
                column: "CargoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_CargoTrasladoActualId",
                table: "AccionesPersonal",
                column: "CargoTrasladoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_CargoTrasladoNuevoId",
                table: "AccionesPersonal",
                column: "CargoTrasladoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_ColaboradorId",
                table: "AccionesPersonal",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_DepartamentoActualId",
                table: "AccionesPersonal",
                column: "DepartamentoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_DepartamentoNuevoId",
                table: "AccionesPersonal",
                column: "DepartamentoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_DepartamentoTrasladoActualId",
                table: "AccionesPersonal",
                column: "DepartamentoTrasladoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_DepartamentoTrasladoNuevoId",
                table: "AccionesPersonal",
                column: "DepartamentoTrasladoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EjecutadaPorUsuarioId",
                table: "AccionesPersonal",
                column: "EjecutadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EmpresaActualId",
                table: "AccionesPersonal",
                column: "EmpresaActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EmpresaNuevaId",
                table: "AccionesPersonal",
                column: "EmpresaNuevaId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EmpresaTrasladoActualId",
                table: "AccionesPersonal",
                column: "EmpresaTrasladoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EmpresaTrasladoNuevaId",
                table: "AccionesPersonal",
                column: "EmpresaTrasladoNuevaId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_EstatusActualId",
                table: "AccionesPersonal",
                column: "EstatusActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_JefeActualId",
                table: "AccionesPersonal",
                column: "JefeActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_JefeNuevoId",
                table: "AccionesPersonal",
                column: "JefeNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_JefeTrasladoNuevoId",
                table: "AccionesPersonal",
                column: "JefeTrasladoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_MotivoSalidaId",
                table: "AccionesPersonal",
                column: "MotivoSalidaId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_SolicitudId",
                table: "AccionesPersonal",
                column: "SolicitudId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_TipoAccion_Ejecutada",
                table: "AccionesPersonal",
                columns: new[] { "TipoAccion", "Ejecutada" });

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_TipoContratoActualId",
                table: "AccionesPersonal",
                column: "TipoContratoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_TipoContratoNuevoId",
                table: "AccionesPersonal",
                column: "TipoContratoNuevoId");

            migrationBuilder.CreateIndex(
                name: "IX_AccionPersonalCambiosAplicados_AccionPersonalId_Fecha",
                table: "AccionPersonalCambiosAplicados",
                columns: new[] { "AccionPersonalId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_AccionPersonalCambiosAplicados_UsuarioId",
                table: "AccionPersonalCambiosAplicados",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccionPersonalCambiosAplicados");

            migrationBuilder.DropTable(
                name: "AccionesPersonal");
        }
    }
}
