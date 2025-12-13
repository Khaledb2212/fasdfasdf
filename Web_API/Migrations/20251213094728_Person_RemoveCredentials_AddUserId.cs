using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_API.Migrations
{
    /// <inheritdoc />
    public partial class Person_RemoveCredentials_AddUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "People");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "People");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "People");

            // 1) Add UserId as NULLABLE first (NO default "")
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "People",
                type: "nvarchar(450)",
                nullable: true);

            // 2) Give every existing row a UNIQUE placeholder value
            // This avoids duplicates and lets the UNIQUE index be created.
            migrationBuilder.Sql(@"
        UPDATE [People]
        SET [UserId] = CONCAT('LEGACY_', CAST([PersonID] AS nvarchar(50)))
        WHERE [UserId] IS NULL OR [UserId] = ''
    ");

            // 3) Now make it NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "People",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            // 4) Create the UNIQUE index
            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId",
                unique: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_UserId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "People");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "People",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "Role",
                table: "People",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "People",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
