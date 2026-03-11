using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutfitApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClothingDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "ClothingItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fit",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Material",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Occasion",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pattern",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Style",
                table: "ClothingItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingItems_Occasion",
                table: "ClothingItems",
                column: "Occasion");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingItems_Style",
                table: "ClothingItems",
                column: "Style");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClothingItems_Occasion",
                table: "ClothingItems");

            migrationBuilder.DropIndex(
                name: "IX_ClothingItems_Style",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Fit",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Occasion",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Style",
                table: "ClothingItems");
        }
    }
}
