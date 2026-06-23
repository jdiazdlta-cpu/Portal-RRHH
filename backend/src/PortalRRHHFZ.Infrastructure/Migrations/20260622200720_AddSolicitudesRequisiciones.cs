using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalRRHHFZ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudesRequisiciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    SolicitudId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoSolicitud = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoSolicitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SolicitanteUsuarioId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoId = table.Column<int>(type: "int", nullable: true),
                    CargoId = table.Column<int>(type: "int", nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEfectiva = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Justificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.SolicitudId);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_SolicitanteUsuarioId",
                        column: x => x.SolicitanteUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequisicionesPersonal",
                columns: table => new
                {
                    RequisicionPersonalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    CargoSolicitado = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DepartamentoSolicitadoId = table.Column<int>(type: "int", nullable: true),
                    NumeroPlazas = table.Column<int>(type: "int", nullable: false),
                    DependenciaJerarquica = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PrincipalesResponsabilidades = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    FuncionesEspecificas = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    EquipoACargo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CentroTrabajo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Salario = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GastoRepresentacion = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioVariable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OtrosConceptos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EsPosicionNueva = table.Column<bool>(type: "bit", nullable: false),
                    EsReemplazo = table.Column<bool>(type: "bit", nullable: false),
                    ColaboradorReemplazadoId = table.Column<int>(type: "int", nullable: true),
                    NombrePersonaReemplazada = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    TipoContratoId = table.Column<int>(type: "int", nullable: true),
                    PeriodoPrueba = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FormacionRequerida = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FormacionComplementaria = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConocimientosTecnicos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConocimientosValorados = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IdiomaNivel = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AniosExperiencia = table.Column<int>(type: "int", nullable: true),
                    FuncionesExperiencia = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AreaSectorExperiencia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExperienciaValorable = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EdadMinima = table.Column<int>(type: "int", nullable: true),
                    EdadMaxima = table.Column<int>(type: "int", nullable: true),
                    SexoPreferido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CaracteristicasPersonales = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaAperturaProceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEntregaCandidatos = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SolicitadoPorTexto = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    AutorizadoPorTexto = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    FechaAutorizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisicionesPersonal", x => x.RequisicionPersonalId);
                    table.ForeignKey(
                        name: "FK_RequisicionesPersonal_Colaboradores_ColaboradorReemplazadoId",
                        column: x => x.ColaboradorReemplazadoId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicionesPersonal_Departamentos_DepartamentoSolicitadoId",
                        column: x => x.DepartamentoSolicitadoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicionesPersonal_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "SolicitudId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisicionesPersonal_TiposContrato_TipoContratoId",
                        column: x => x.TipoContratoId,
                        principalTable: "TiposContrato",
                        principalColumn: "TipoContratoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudAprobaciones",
                columns: table => new
                {
                    SolicitudAprobacionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Etapa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RolAprobador = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UsuarioAprobadorId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaDecision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudAprobaciones", x => x.SolicitudAprobacionId);
                    table.ForeignKey(
                        name: "FK_SolicitudAprobaciones_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "SolicitudId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudAprobaciones_Usuarios_UsuarioAprobadorId",
                        column: x => x.UsuarioAprobadorId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudHistorial",
                columns: table => new
                {
                    SolicitudHistorialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudHistorial", x => x.SolicitudHistorialId);
                    table.ForeignKey(
                        name: "FK_SolicitudHistorial_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "SolicitudId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudHistorial_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisicionesPersonal_ColaboradorReemplazadoId",
                table: "RequisicionesPersonal",
                column: "ColaboradorReemplazadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicionesPersonal_DepartamentoSolicitadoId",
                table: "RequisicionesPersonal",
                column: "DepartamentoSolicitadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisicionesPersonal_SolicitudId",
                table: "RequisicionesPersonal",
                column: "SolicitudId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequisicionesPersonal_TipoContratoId",
                table: "RequisicionesPersonal",
                column: "TipoContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAprobaciones_SolicitudId_Orden",
                table: "SolicitudAprobaciones",
                columns: new[] { "SolicitudId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAprobaciones_UsuarioAprobadorId",
                table: "SolicitudAprobaciones",
                column: "UsuarioAprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_CargoId",
                table: "Solicitudes",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_CodigoSolicitud",
                table: "Solicitudes",
                column: "CodigoSolicitud",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_ColaboradorId",
                table: "Solicitudes",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_DepartamentoId",
                table: "Solicitudes",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_EmpresaId",
                table: "Solicitudes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_SolicitanteUsuarioId",
                table: "Solicitudes",
                column: "SolicitanteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_TipoSolicitud_Estado",
                table: "Solicitudes",
                columns: new[] { "TipoSolicitud", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudHistorial_SolicitudId_Fecha",
                table: "SolicitudHistorial",
                columns: new[] { "SolicitudId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudHistorial_UsuarioId",
                table: "SolicitudHistorial",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequisicionesPersonal");

            migrationBuilder.DropTable(
                name: "SolicitudAprobaciones");

            migrationBuilder.DropTable(
                name: "SolicitudHistorial");

            migrationBuilder.DropTable(
                name: "Solicitudes");
        }
    }
}
