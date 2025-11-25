using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NammaOoru.Migrations
{
    /// <inheritdoc />
    public partial class MakeOtpUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtpVerifications_Users_UserId",
                table: "OtpVerifications");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "OtpVerifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_OtpVerifications_Users_UserId",
                table: "OtpVerifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OtpVerifications_Users_UserId",
                table: "OtpVerifications");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "OtpVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OtpVerifications_Users_UserId",
                table: "OtpVerifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
