using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasCommerce.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class OrderIsPickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPickup",
                table: "Orders",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPickup",
                table: "Orders");
        }
    }
}
