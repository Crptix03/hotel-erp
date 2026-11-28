using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelErp.api.Migrations
{
    /// <inheritdoc />
    public partial class FolioLineAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "FolioLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostedById",
                table: "FolioLines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FolioLines_PostedById",
                table: "FolioLines",
                column: "PostedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FolioLines_Users_PostedById",
                table: "FolioLines",
                column: "PostedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FolioLines_Users_PostedById",
                table: "FolioLines");

            migrationBuilder.DropIndex(
                name: "IX_FolioLines_PostedById",
                table: "FolioLines");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "FolioLines");

            migrationBuilder.DropColumn(
                name: "PostedById",
                table: "FolioLines");
        }
    }
}
