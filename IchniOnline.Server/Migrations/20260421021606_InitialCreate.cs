using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IchniOnline.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    song_name = table.Column<string>(type: "text", nullable: false),
                    illustrate_url = table.Column<string>(type: "text", nullable: false),
                    illustrator = table.Column<string>(type: "text", nullable: false),
                    composer = table.Column<string>(type: "text", nullable: false),
                    level_designer = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<string>(type: "text", nullable: false),
                    level_color = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    release_time = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beatmap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    permission = table.Column<int>(type: "integer", nullable: false),
                    password_hashed = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "taptap_inter_auth",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    taptap_open_id = table.Column<string>(type: "text", nullable: false),
                    taptap_union_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taptap_inter_auth", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "play_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    beatmap_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    perfect_count = table.Column<long>(type: "bigint", nullable: false),
                    good_count = table.Column<long>(type: "bigint", nullable: false),
                    bad_count = table.Column<long>(type: "bigint", nullable: false),
                    miss_count = table.Column<long>(type: "bigint", nullable: false),
                    max_combo = table.Column<long>(type: "bigint", nullable: false),
                    time = table.Column<long>(type: "bigint", nullable: false),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_play_data", x => x.id);
                    table.ForeignKey(
                        name: "FK_play_data_beatmap_beatmap_id",
                        column: x => x.beatmap_id,
                        principalTable: "beatmap",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_play_data_game_user_user_id",
                        column: x => x.user_id,
                        principalTable: "game_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_play_data_beatmap_id",
                table: "play_data",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "IX_play_data_user_id",
                table: "play_data",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "play_data");

            migrationBuilder.DropTable(
                name: "taptap_inter_auth");

            migrationBuilder.DropTable(
                name: "beatmap");

            migrationBuilder.DropTable(
                name: "game_user");
        }
    }
}
