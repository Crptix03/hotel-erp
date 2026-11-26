using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelErp.api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIdDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GuestIdDocuments_IdType_IdNumber_IssuingCountry",
                table: "GuestIdDocuments",
                columns: new[] { "IdType", "IdNumber", "IssuingCountry" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuestIdDocuments_IdType_IdNumber_IssuingCountry",
                table: "GuestIdDocuments");
        }
    }
}
