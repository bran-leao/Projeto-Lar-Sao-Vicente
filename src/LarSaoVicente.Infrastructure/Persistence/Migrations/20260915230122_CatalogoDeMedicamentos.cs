using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LarSaoVicente.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoDeMedicamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Medicamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommercialName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ActiveIngredient = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Form = table.Column<int>(type: "int", nullable: true),
                    UnitsPerPackage = table.Column<int>(type: "int", nullable: true),
                    PackageUnit = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NormalizedSearchKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntradasDeMedicamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryNotIdentified = table.Column<bool>(type: "bit", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    ReceivedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IdentifiedByScan = table.Column<bool>(type: "bit", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntradasDeMedicamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntradasDeMedicamento_Medicamentos_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medicamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntradasDeMedicamento_ExpiryDate",
                table: "EntradasDeMedicamento",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_EntradasDeMedicamento_MedicationId_Status",
                table: "EntradasDeMedicamento",
                columns: new[] { "MedicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EntradasDeMedicamento_Status",
                table: "EntradasDeMedicamento",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_ActiveIngredient",
                table: "Medicamentos",
                column: "ActiveIngredient");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_Barcode",
                table: "Medicamentos",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_NormalizedSearchKey",
                table: "Medicamentos",
                column: "NormalizedSearchKey");

            migrationBuilder.CreateIndex(
                name: "IX_Medicamentos_Status",
                table: "Medicamentos",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntradasDeMedicamento");

            migrationBuilder.DropTable(
                name: "Medicamentos");
        }
    }
}
