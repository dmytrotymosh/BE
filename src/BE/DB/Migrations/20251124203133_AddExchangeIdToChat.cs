using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DB.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeIdToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "exchange_id",
                table: "Chats",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_exchange_id",
                table: "Chats",
                column: "exchange_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Exchanges_exchange_id",
                table: "Chats",
                column: "exchange_id",
                principalTable: "Exchanges",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Exchanges_exchange_id",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_exchange_id",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "exchange_id",
                table: "Chats");
        }
    }
}
