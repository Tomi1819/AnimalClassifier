using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimalClassifier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnimalRecognitionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId",
                table: "AnimalRecognitionLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "AnimalRecognitionLogs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "AnimalRecognitionLogs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalRecognitionLogs_UserId1",
                table: "AnimalRecognitionLogs",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId",
                table: "AnimalRecognitionLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId1",
                table: "AnimalRecognitionLogs",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId",
                table: "AnimalRecognitionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId1",
                table: "AnimalRecognitionLogs");

            migrationBuilder.DropIndex(
                name: "IX_AnimalRecognitionLogs_UserId1",
                table: "AnimalRecognitionLogs");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "AnimalRecognitionLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ImagePath",
                table: "AnimalRecognitionLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddForeignKey(
                name: "FK_AnimalRecognitionLogs_AspNetUsers_UserId",
                table: "AnimalRecognitionLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
