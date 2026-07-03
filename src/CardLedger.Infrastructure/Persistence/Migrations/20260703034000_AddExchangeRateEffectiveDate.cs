using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateEffectiveDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveDate",
                table: "exchange_rates",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE exchange_rates
                SET "EffectiveDate" = "RecordDate"
                WHERE "EffectiveDate" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EffectiveDate",
                table: "exchange_rates",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_exchange_rates_CurrencyCode_RecordDate",
                table: "exchange_rates");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_CurrencyCode_EffectiveDate",
                table: "exchange_rates",
                columns: new[] { "CurrencyCode", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_RecordDate",
                table: "exchange_rates",
                column: "RecordDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exchange_rates_RecordDate",
                table: "exchange_rates");

            migrationBuilder.DropIndex(
                name: "IX_exchange_rates_CurrencyCode_EffectiveDate",
                table: "exchange_rates");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "exchange_rates");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_CurrencyCode_RecordDate",
                table: "exchange_rates",
                columns: new[] { "CurrencyCode", "RecordDate" },
                unique: true);
        }
    }
}
