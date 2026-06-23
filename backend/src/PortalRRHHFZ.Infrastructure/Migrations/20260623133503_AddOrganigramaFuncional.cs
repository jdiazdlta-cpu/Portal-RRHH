using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalRRHHFZ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganigramaFuncional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColaboradorAprobadorId",
                table: "SolicitudAprobaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartamentoResponsableId",
                table: "SolicitudAprobaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DepartamentoResponsables",
                columns: table => new
                {
                    DepartamentoResponsableId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorResponsableId = table.Column<int>(type: "int", nullable: false),
                    UsuarioResponsableId = table.Column<int>(type: "int", nullable: true),
                    TipoResponsable = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    PuedeAprobarSolicitudes = table.Column<bool>(type: "bit", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartamentoResponsables", x => x.DepartamentoResponsableId);
                    table.ForeignKey(
                        name: "FK_DepartamentoResponsables_Colaboradores_ColaboradorResponsableId",
                        column: x => x.ColaboradorResponsableId,
                        principalTable: "Colaboradores",
                        principalColumn: "ColaboradorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartamentoResponsables_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartamentoResponsables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartamentoResponsables_Usuarios_UsuarioResponsableId",
                        column: x => x.UsuarioResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganigramaHistorialCambios",
                columns: table => new
                {
                    OrganigramaHistorialCambioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Entidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntidadId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganigramaHistorialCambios", x => x.OrganigramaHistorialCambioId);
                    table.ForeignKey(
                        name: "FK_OrganigramaHistorialCambios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organigramas",
                columns: table => new
                {
                    OrganigramaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organigramas", x => x.OrganigramaId);
                    table.ForeignKey(
                        name: "FK_Organigramas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganigramaNodos",
                columns: table => new
                {
                    OrganigramaNodoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganigramaId = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    DepartamentoId = table.Column<int>(type: "int", nullable: true),
                    CargoId = table.Column<int>(type: "int", nullable: true),
                    NodoPadreId = table.Column<int>(type: "int", nullable: true),
                    NombreNodo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    EsRolOperativo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganigramaNodos", x => x.OrganigramaNodoId);
                    table.ForeignKey(
                        name: "FK_OrganigramaNodos_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "CargoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganigramaNodos_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "DepartamentoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganigramaNodos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "EmpresaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganigramaNodos_OrganigramaNodos_NodoPadreId",
                        column: x => x.NodoPadreId,
                        principalTable: "OrganigramaNodos",
                        principalColumn: "OrganigramaNodoId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganigramaNodos_Organigramas_OrganigramaId",
                        column: x => x.OrganigramaId,
                        principalTable: "Organigramas",
                        principalColumn: "OrganigramaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAprobaciones_ColaboradorAprobadorId",
                table: "SolicitudAprobaciones",
                column: "ColaboradorAprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudAprobaciones_DepartamentoResponsableId",
                table: "SolicitudAprobaciones",
                column: "DepartamentoResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoResponsables_ColaboradorResponsableId",
                table: "DepartamentoResponsables",
                column: "ColaboradorResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoResponsables_DepartamentoId_PuedeAprobarSolicitudes_IsActive",
                table: "DepartamentoResponsables",
                columns: new[] { "DepartamentoId", "PuedeAprobarSolicitudes", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoResponsables_EmpresaId_DepartamentoId_TipoResponsable_IsActive",
                table: "DepartamentoResponsables",
                columns: new[] { "EmpresaId", "DepartamentoId", "TipoResponsable", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentoResponsables_UsuarioResponsableId",
                table: "DepartamentoResponsables",
                column: "UsuarioResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaHistorialCambios_Entidad_EntidadId_Fecha",
                table: "OrganigramaHistorialCambios",
                columns: new[] { "Entidad", "EntidadId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaHistorialCambios_UsuarioId",
                table: "OrganigramaHistorialCambios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaNodos_CargoId",
                table: "OrganigramaNodos",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaNodos_DepartamentoId",
                table: "OrganigramaNodos",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaNodos_EmpresaId",
                table: "OrganigramaNodos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaNodos_NodoPadreId",
                table: "OrganigramaNodos",
                column: "NodoPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganigramaNodos_OrganigramaId_Nivel_Orden",
                table: "OrganigramaNodos",
                columns: new[] { "OrganigramaId", "Nivel", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_Organigramas_EmpresaId_IsActive",
                table: "Organigramas",
                columns: new[] { "EmpresaId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudAprobaciones_Colaboradores_ColaboradorAprobadorId",
                table: "SolicitudAprobaciones",
                column: "ColaboradorAprobadorId",
                principalTable: "Colaboradores",
                principalColumn: "ColaboradorId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudAprobaciones_DepartamentoResponsables_DepartamentoResponsableId",
                table: "SolicitudAprobaciones",
                column: "DepartamentoResponsableId",
                principalTable: "DepartamentoResponsables",
                principalColumn: "DepartamentoResponsableId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudAprobaciones_Colaboradores_ColaboradorAprobadorId",
                table: "SolicitudAprobaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudAprobaciones_DepartamentoResponsables_DepartamentoResponsableId",
                table: "SolicitudAprobaciones");

            migrationBuilder.DropTable(
                name: "DepartamentoResponsables");

            migrationBuilder.DropTable(
                name: "OrganigramaHistorialCambios");

            migrationBuilder.DropTable(
                name: "OrganigramaNodos");

            migrationBuilder.DropTable(
                name: "Organigramas");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudAprobaciones_ColaboradorAprobadorId",
                table: "SolicitudAprobaciones");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudAprobaciones_DepartamentoResponsableId",
                table: "SolicitudAprobaciones");

            migrationBuilder.DropColumn(
                name: "ColaboradorAprobadorId",
                table: "SolicitudAprobaciones");

            migrationBuilder.DropColumn(
                name: "DepartamentoResponsableId",
                table: "SolicitudAprobaciones");
        }
    }
}
