using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinance.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RecurringExpenseId",
                table: "Expenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringExpenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AmountInCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirstDueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    NextDueDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpenses", x => x.Id);
                    table.CheckConstraint("CK_RecurringExpenses_AmountInCentavos_Positive", "AmountInCentavos > 0");
                    table.ForeignKey(
                        name: "FK_RecurringExpenses_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_RecurringExpenseId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "RecurringExpenseId", "ExpenseDate" },
                unique: true,
                filter: "RecurringExpenseId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_CategoryId",
                table: "RecurringExpenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenses_NextDueDate",
                table: "RecurringExpenses",
                column: "NextDueDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_RecurringExpenses_RecurringExpenseId",
                table: "Expenses",
                column: "RecurringExpenseId",
                principalTable: "RecurringExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_RecurringExpenses_RecurringExpenseId",
                table: "Expenses");

            migrationBuilder.DropTable(
                name: "RecurringExpenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_RecurringExpenseId_ExpenseDate",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RecurringExpenseId",
                table: "Expenses");
        }
    }
}
