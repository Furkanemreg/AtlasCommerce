using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasCommerce.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class UnitPriceForOrderEtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "OrderItems",
                newName: "UnitPriceInclTax");

            migrationBuilder.AddColumn<int>(
                name: "OTVRate",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TaxRate",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceExclTax",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OTVRate",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPriceExclTax",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "UnitPriceInclTax",
                table: "OrderItems",
                newName: "UnitPrice");
        }
    }
}
