using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaMaster.Migrations
{
    /// <inheritdoc />
    public sealed partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Medias",
                columns: table => new
                {
                    MediaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Uri = table.Column<string>(type: "TEXT", nullable: false),
                    Modified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Added = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.MediaId);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Shorthand = table.Column<string>(type: "TEXT", nullable: false),
                    FirstParentReferenceName = table.Column<string>(type: "TEXT", nullable: false),
                    Flags = table.Column<int>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<int>(type: "INTEGER", nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: false),
                    Argb = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "MediaTags",
                columns: table => new
                {
                    MediaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTags", x => new { x.MediaId, x.TagId });
                    table.ForeignKey(
                        name: "FK_MediaTags_Medias_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Medias",
                        principalColumn: "MediaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagTags",
                columns: table => new
                {
                    ChildrenTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentsTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTags", x => new { x.ChildrenTagId, x.ParentsTagId });
                    table.ForeignKey(
                        name: "FK_TagTags_Tags_ChildrenTagId",
                        column: x => x.ChildrenTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagTags_Tags_ParentsTagId",
                        column: x => x.ParentsTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Added",
                table: "Medias",
                column: "Added");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_IsArchived",
                table: "Medias",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_IsFavorite",
                table: "Medias",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Modified",
                table: "Medias",
                column: "Modified");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Name",
                table: "Medias",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_Uri",
                table: "Medias",
                column: "Uri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaTags_TagId",
                table: "MediaTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagTags_ParentsTagId",
                table: "TagTags",
                column: "ParentsTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaTags");

            migrationBuilder.DropTable(
                name: "TagTags");

            migrationBuilder.DropTable(
                name: "Medias");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
