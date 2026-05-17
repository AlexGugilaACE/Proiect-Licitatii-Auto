using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoAuction.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionPartyConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Auctions_AuctionId",
                table: "Transactions");

            migrationBuilder.AddColumn<bool>(
                name: "BuyerConfirmed",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SellerConfirmed",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Auctions_AuctionId",
                table: "Transactions",
                column: "AuctionId",
                principalTable: "Auctions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Auctions_AuctionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BuyerConfirmed",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SellerConfirmed",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Auctions_AuctionId",
                table: "Transactions",
                column: "AuctionId",
                principalTable: "Auctions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
