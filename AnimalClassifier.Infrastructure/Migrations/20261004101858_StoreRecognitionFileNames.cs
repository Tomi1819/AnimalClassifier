using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimalClassifier.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreRecognitionFileNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "AnimalRecognitionLogs",
                newName: "FileName");

            // The column held the path the file was served under, which ends
            // in its name: /uploads/{userId}/{fileName}.
            migrationBuilder.Sql(
                """
                UPDATE AnimalRecognitionLogs
                SET FileName = RIGHT(FileName, CHARINDEX('/', REVERSE(FileName)) - 1)
                WHERE FileName LIKE '%/%'
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back under the path uploads were served under by default.
            migrationBuilder.Sql(
                """
                UPDATE AnimalRecognitionLogs
                SET FileName = CONCAT('/uploads/', UserId, '/', FileName)
                """);

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "AnimalRecognitionLogs",
                newName: "ImagePath");
        }
    }
}
