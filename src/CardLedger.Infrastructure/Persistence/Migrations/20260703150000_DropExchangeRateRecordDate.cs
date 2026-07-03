using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropExchangeRateRecordDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exchange_rates_RecordDate",
                table: "exchange_rates");

            migrationBuilder.DropColumn(
                name: "RecordDate",
                table: "exchange_rates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RecordDate",
                table: "exchange_rates",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2025, 12, 31));

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_RecordDate",
                table: "exchange_rates",
                column: "RecordDate");
        }
    }
}
