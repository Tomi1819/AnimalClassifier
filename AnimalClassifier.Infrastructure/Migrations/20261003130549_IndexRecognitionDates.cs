using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimalClassifier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndexRecognitionDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AnimalRecognitionLogs_DateRecognized",
                table: "AnimalRecognitionLogs",
                column: "DateRecognized");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnimalRecognitionLogs_DateRecognized",
                table: "AnimalRecognitionLogs");
        }
    }
}
