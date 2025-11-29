using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSwipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                table: "Books");

            migrationBuilder.AddColumn<Guid>(
                name: "language_id",
                table: "Books",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "BookSwipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    swipe_type = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp", nullable: false),
                    modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookSwipes", x => x.id);
                    table.ForeignKey(
                        name: "FK_BookSwipes_Books_book_id",
                        column: x => x.book_id,
                        principalTable: "Books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookSwipes_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp", nullable: false),
                    modified = table.Column<DateTime>(type: "timestamp", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_language_id",
                table: "Books",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "IX_BookSwipes_book_id",
                table: "BookSwipes",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "IX_BookSwipes_user_id_book_id",
                table: "BookSwipes",
                columns: new[] { "user_id", "book_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Languages_language_id",
                table: "Books",
                column: "language_id",
                principalTable: "Languages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Languages_language_id",
                table: "Books");

            migrationBuilder.DropTable(
                name: "BookSwipes");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_Books_language_id",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "language_id",
                table: "Books");

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "Books",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
