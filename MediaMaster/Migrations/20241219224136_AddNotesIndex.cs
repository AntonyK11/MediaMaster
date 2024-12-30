using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaMaster.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Medias_Notes",
                table: "Medias",
                column: "Notes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Medias_Notes",
                table: "Medias");
        }
    }
}
