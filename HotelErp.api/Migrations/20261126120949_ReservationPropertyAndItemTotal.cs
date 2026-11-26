using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelErp.api.Migrations
{
    /// <inheritdoc />
    public partial class ReservationPropertyAndItemTotal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "ReservationItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PropertyId_CheckInDate_CheckOutDate",
                table: "Reservations",
                columns: new[] { "PropertyId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Properties_PropertyId",
                table: "Reservations",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Properties_PropertyId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PropertyId_CheckInDate_CheckOutDate",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "ReservationItems");
        }
    }
}
