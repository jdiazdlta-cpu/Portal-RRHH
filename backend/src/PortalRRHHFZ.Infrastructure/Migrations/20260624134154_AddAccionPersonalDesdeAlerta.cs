using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalRRHHFZ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccionPersonalDesdeAlerta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlertaOrigenId",
                table: "AccionesPersonal",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccionesPersonal_AlertaOrigenId",
                table: "AccionesPersonal",
                column: "AlertaOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccionesPersonal_Alertas_AlertaOrigenId",
                table: "AccionesPersonal",
                column: "AlertaOrigenId",
                principalTable: "Alertas",
                principalColumn: "AlertaId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccionesPersonal_Alertas_AlertaOrigenId",
                table: "AccionesPersonal");

            migrationBuilder.DropIndex(
                name: "IX_AccionesPersonal_AlertaOrigenId",
                table: "AccionesPersonal");

            migrationBuilder.DropColumn(
                name: "AlertaOrigenId",
                table: "AccionesPersonal");
        }
    }
}
