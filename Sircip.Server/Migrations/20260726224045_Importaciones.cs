using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sircip.Server.Migrations
{
    /// <inheritdoc />
    public partial class Importaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Importaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Periodo = table.Column<int>(type: "int", nullable: false),
                    FechaImportacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CantidadRegistros = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Importaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Importaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Importaciones_Periodo",
                table: "Importaciones",
                column: "Periodo",
                unique: true,
                filter: "[Estado] = 'Exitosa'");

            migrationBuilder.CreateIndex(
                name: "IX_Importaciones_UsuarioId",
                table: "Importaciones",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Importaciones");
        }
    }
}
