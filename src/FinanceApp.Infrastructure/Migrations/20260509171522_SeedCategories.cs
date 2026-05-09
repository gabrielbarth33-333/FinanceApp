using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("0a4f6705-e9a9-4176-af3b-61c7c82510a6"), "Saúde" },
                    { new Guid("22e58162-c197-4053-afe7-3f196c1588d7"), "Educação" },
                    { new Guid("3c0a2e0b-a481-4969-aa3c-e634856aff8b"), "Lazer" },
                    { new Guid("40e2f533-d1f6-4bae-9b3b-ebacbef7f0b9"), "Investimentos" },
                    { new Guid("785a055d-c8ac-4903-89ec-285ae98a715b"), "Alimentação" },
                    { new Guid("79c1c1e9-ae9f-425f-84f1-28d5abb13864"), "Salário" },
                    { new Guid("95e52813-82b5-400c-9ba6-44228c754352"), "Transporte" },
                    { new Guid("ae1486ca-a148-43c9-873f-3d70222c11d7"), "Moradia" },
                    { new Guid("c989c51b-1694-421f-b4ce-6f2bdff58688"), "Outros" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("0a4f6705-e9a9-4176-af3b-61c7c82510a6"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("22e58162-c197-4053-afe7-3f196c1588d7"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("3c0a2e0b-a481-4969-aa3c-e634856aff8b"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("40e2f533-d1f6-4bae-9b3b-ebacbef7f0b9"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("785a055d-c8ac-4903-89ec-285ae98a715b"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("79c1c1e9-ae9f-425f-84f1-28d5abb13864"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("95e52813-82b5-400c-9ba6-44228c754352"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("ae1486ca-a148-43c9-873f-3d70222c11d7"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("c989c51b-1694-421f-b4ce-6f2bdff58688"));
        }
    }
}
