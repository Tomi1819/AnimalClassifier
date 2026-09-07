using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimalClassifier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecognitionHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FramesProcessed",
                table: "AnimalRecognitionLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AnimalRecognitionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "PredictionScore",
                table: "AnimalRecognitionLogs",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FramesProcessed",
                table: "AnimalRecognitionLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AnimalRecognitionLogs");

            migrationBuilder.DropColumn(
                name: "PredictionScore",
                table: "AnimalRecognitionLogs");
        }
    }
}
