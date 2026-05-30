using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtlasCommerce.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class UserMessageAdminReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "UserMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAt",
                table: "UserMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepliedBy",
                table: "UserMessages",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "UserMessages");

            migrationBuilder.DropColumn(
                name: "RepliedAt",
                table: "UserMessages");

            migrationBuilder.DropColumn(
                name: "RepliedBy",
                table: "UserMessages");
        }
    }
}
