using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AhelpHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maid_ahelp_messages",
                columns: table => new
                {
                    maid_ahelp_messages_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    sender_is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    admin_only = table.Column<bool>(type: "boolean", nullable: false),
                    play_sound = table.Column<bool>(type: "boolean", nullable: false),
                    from_webhook = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maid_ahelp_messages", x => x.maid_ahelp_messages_id);
                    table.ForeignKey(
                        name: "FK_maid_ahelp_messages_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_maid_ahelp_messages_player_user_id_maid_ahelp_messages_id",
                table: "maid_ahelp_messages",
                columns: new[] { "player_user_id", "maid_ahelp_messages_id" });

            migrationBuilder.CreateIndex(
                name: "IX_maid_ahelp_messages_round_id",
                table: "maid_ahelp_messages",
                column: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maid_ahelp_messages");
        }
    }
}
