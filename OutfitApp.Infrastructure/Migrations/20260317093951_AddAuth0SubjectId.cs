using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OutfitApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuth0SubjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Auth0SubjectId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Auth0SubjectId",
                table: "Users");
        }
    }
}
