using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasCommerce.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class DeletedByAndAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Images",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Images",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "UserMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserMessages",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "WebsiteBanners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WebsiteBanners",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "WebsiteFeature",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WebsiteFeature",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "WebsiteServices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WebsiteServices",
                type: "datetime2",
                nullable: true);

            /////////*****/////////

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "WebsiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WebsiteSettings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
