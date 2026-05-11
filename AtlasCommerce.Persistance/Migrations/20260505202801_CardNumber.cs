using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasCommerce.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class CardNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CardLast4",
                table: "Payments",
                newName: "CardNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CardNumber",
                table: "Payments",
                newName: "CardLast4");
        }
    }
}
