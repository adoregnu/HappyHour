using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyHour.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActorMovie",
                columns: table => new
                {
                    ActorsKey = table.Column<int>(type: "int", nullable: false),
                    MoviesKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorMovie", x => new { x.ActorsKey, x.MoviesKey });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActorNames",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<int>(type: "int", nullable: false),
                    NameKey = table.Column<long>(type: "bigint", nullable: true),
                    ActorKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActorNames", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ThumbKey = table.Column<int>(type: "int", nullable: true),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateBirth = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Key);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GenreMovie",
                columns: table => new
                {
                    GenresKey = table.Column<int>(type: "int", nullable: false),
                    MoviesKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenreMovie", x => new { x.GenresKey, x.MoviesKey });
                    table.ForeignKey(
                        name: "FK_GenreMovie_MovieGenres_GenresKey",
                        column: x => x.GenresKey,
                        principalTable: "MovieGenres",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<byte[]>(type: "MediumBlob", nullable: true),
                    Hash = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorKey = table.Column<int>(type: "int", nullable: true),
                    MovieKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Images_Actors_ActorKey",
                        column: x => x.ActorKey,
                        principalTable: "Actors",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LogoKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Labels_Images_LogoKey",
                        column: x => x.LogoKey,
                        principalTable: "Images",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Makers",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LogoKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Makers", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Makers_Images_LogoKey",
                        column: x => x.LogoKey,
                        principalTable: "Images",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LabelMaker",
                columns: table => new
                {
                    LabelsKey = table.Column<int>(type: "int", nullable: false),
                    MakersKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelMaker", x => new { x.LabelsKey, x.MakersKey });
                    table.ForeignKey(
                        name: "FK_LabelMaker_Labels_LabelsKey",
                        column: x => x.LabelsKey,
                        principalTable: "Labels",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabelMaker_Makers_MakersKey",
                        column: x => x.MakersKey,
                        principalTable: "Makers",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PID = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AltPID = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Censored = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateReleased = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CoverKey = table.Column<int>(type: "int", nullable: true),
                    MakerKey = table.Column<int>(type: "int", nullable: true),
                    LabelKey = table.Column<int>(type: "int", nullable: true),
                    SeriesKey = table.Column<int>(type: "int", nullable: true),
                    VideoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Movies_Images_CoverKey",
                        column: x => x.CoverKey,
                        principalTable: "Images",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_Movies_Labels_LabelKey",
                        column: x => x.LabelKey,
                        principalTable: "Labels",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_Movies_Makers_MakerKey",
                        column: x => x.MakerKey,
                        principalTable: "Makers",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_Movies_Series_SeriesKey",
                        column: x => x.SeriesKey,
                        principalTable: "Series",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LongTexts",
                columns: table => new
                {
                    Key = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(type: "varchar(4906)", maxLength: 4906, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovieKey = table.Column<int>(type: "int", nullable: true),
                    Lang = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTexts", x => x.Key);
                    table.ForeignKey(
                        name: "FK_LongTexts_Movies_MovieKey",
                        column: x => x.MovieKey,
                        principalTable: "Movies",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Key = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Rate = table.Column<float>(type: "float", nullable: false),
                    SiteUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovieKey = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Key);
                    table.ForeignKey(
                        name: "FK_Ratings_Movies_MovieKey",
                        column: x => x.MovieKey,
                        principalTable: "Movies",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShortTexts",
                columns: table => new
                {
                    Key = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Text = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GenreKey = table.Column<int>(type: "int", nullable: true),
                    LabelKey = table.Column<int>(type: "int", nullable: true),
                    MakerKey = table.Column<int>(type: "int", nullable: true),
                    MovieKey = table.Column<int>(type: "int", nullable: true),
                    SeriesKey = table.Column<int>(type: "int", nullable: true),
                    Lang = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortTexts", x => x.Key);
                    table.ForeignKey(
                        name: "FK_ShortTexts_Labels_LabelKey",
                        column: x => x.LabelKey,
                        principalTable: "Labels",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_ShortTexts_Makers_MakerKey",
                        column: x => x.MakerKey,
                        principalTable: "Makers",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_ShortTexts_MovieGenres_GenreKey",
                        column: x => x.GenreKey,
                        principalTable: "MovieGenres",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_ShortTexts_Movies_MovieKey",
                        column: x => x.MovieKey,
                        principalTable: "Movies",
                        principalColumn: "Key");
                    table.ForeignKey(
                        name: "FK_ShortTexts_Series_SeriesKey",
                        column: x => x.SeriesKey,
                        principalTable: "Series",
                        principalColumn: "Key");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ActorMovie_MoviesKey",
                table: "ActorMovie",
                column: "MoviesKey");

            migrationBuilder.CreateIndex(
                name: "IX_ActorNames_ActorKey",
                table: "ActorNames",
                column: "ActorKey");

            migrationBuilder.CreateIndex(
                name: "IX_ActorNames_NameKey",
                table: "ActorNames",
                column: "NameKey");

            migrationBuilder.CreateIndex(
                name: "IX_Actors_ThumbKey",
                table: "Actors",
                column: "ThumbKey");

            migrationBuilder.CreateIndex(
                name: "IX_GenreMovie_MoviesKey",
                table: "GenreMovie",
                column: "MoviesKey");

            migrationBuilder.CreateIndex(
                name: "IX_Images_ActorKey",
                table: "Images",
                column: "ActorKey");

            migrationBuilder.CreateIndex(
                name: "IX_Images_MovieKey",
                table: "Images",
                column: "MovieKey");

            migrationBuilder.CreateIndex(
                name: "IX_LabelMaker_MakersKey",
                table: "LabelMaker",
                column: "MakersKey");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_LogoKey",
                table: "Labels",
                column: "LogoKey");

            migrationBuilder.CreateIndex(
                name: "IX_LongTexts_MovieKey",
                table: "LongTexts",
                column: "MovieKey");

            migrationBuilder.CreateIndex(
                name: "IX_Makers_LogoKey",
                table: "Makers",
                column: "LogoKey");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_CoverKey",
                table: "Movies",
                column: "CoverKey");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_LabelKey",
                table: "Movies",
                column: "LabelKey");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_MakerKey",
                table: "Movies",
                column: "MakerKey");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_SeriesKey",
                table: "Movies",
                column: "SeriesKey");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_MovieKey",
                table: "Ratings",
                column: "MovieKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShortTexts_GenreKey",
                table: "ShortTexts",
                column: "GenreKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShortTexts_LabelKey",
                table: "ShortTexts",
                column: "LabelKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShortTexts_MakerKey",
                table: "ShortTexts",
                column: "MakerKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShortTexts_MovieKey",
                table: "ShortTexts",
                column: "MovieKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShortTexts_SeriesKey",
                table: "ShortTexts",
                column: "SeriesKey");

            migrationBuilder.AddForeignKey(
                name: "FK_ActorMovie_Actors_ActorsKey",
                table: "ActorMovie",
                column: "ActorsKey",
                principalTable: "Actors",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorMovie_Movies_MoviesKey",
                table: "ActorMovie",
                column: "MoviesKey",
                principalTable: "Movies",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActorNames_Actors_ActorKey",
                table: "ActorNames",
                column: "ActorKey",
                principalTable: "Actors",
                principalColumn: "Key");

            migrationBuilder.AddForeignKey(
                name: "FK_ActorNames_ShortTexts_NameKey",
                table: "ActorNames",
                column: "NameKey",
                principalTable: "ShortTexts",
                principalColumn: "Key");

            migrationBuilder.AddForeignKey(
                name: "FK_Actors_Images_ThumbKey",
                table: "Actors",
                column: "ThumbKey",
                principalTable: "Images",
                principalColumn: "Key");

            migrationBuilder.AddForeignKey(
                name: "FK_GenreMovie_Movies_MoviesKey",
                table: "GenreMovie",
                column: "MoviesKey",
                principalTable: "Movies",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Movies_MovieKey",
                table: "Images",
                column: "MovieKey",
                principalTable: "Movies",
                principalColumn: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Actors_ActorKey",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_Images_Movies_MovieKey",
                table: "Images");

            migrationBuilder.DropTable(
                name: "ActorMovie");

            migrationBuilder.DropTable(
                name: "ActorNames");

            migrationBuilder.DropTable(
                name: "GenreMovie");

            migrationBuilder.DropTable(
                name: "LabelMaker");

            migrationBuilder.DropTable(
                name: "LongTexts");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "ShortTexts");

            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "Makers");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
