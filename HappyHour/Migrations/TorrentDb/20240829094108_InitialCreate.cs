using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyHour.Migrations.TorrentDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Torrents",
                columns: table => new
                {
                    Key = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PID = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CoverPath = table.Column<string>(type: "TEXT", nullable: true),
                    StatusCode = table.Column<char>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Torrents", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Magnets",
                columns: table => new
                {
                    Key = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    MagnetUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TorrentKey = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Magnets", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Magnets_Torrents_TorrentKey",
                        column: x => x.TorrentKey,
                        principalTable: "Torrents",
                        principalColumn: "Key");
                });

            migrationBuilder.CreateTable(
                name: "Strings",
                columns: table => new
                {
                    Key = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    TorrentKey = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strings", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Strings_Torrents_TorrentKey",
                        column: x => x.TorrentKey,
                        principalTable: "Torrents",
                        principalColumn: "Key");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Magnets_TorrentKey",
                table: "Magnets",
                column: "TorrentKey");

            migrationBuilder.CreateIndex(
                name: "IX_Strings_TorrentKey",
                table: "Strings",
                column: "TorrentKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Magnets");

            migrationBuilder.DropTable(
                name: "Strings");

            migrationBuilder.DropTable(
                name: "Torrents");
        }
    }
}
