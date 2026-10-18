using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalFinance.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTypeMonthlyLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountInCentavos = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTypeMonthlyLimits", x => x.Id);
                    table.CheckConstraint("CK_PaymentTypeMonthlyLimits_AmountInCentavos_Positive", "AmountInCentavos > 0");
                    table.CheckConstraint("CK_PaymentTypeMonthlyLimits_Month", "Month BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "SavingsGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    MonthlyTargetInCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsGoals", x => x.Id);
                    table.CheckConstraint("CK_SavingsGoals_MonthlyTargetInCentavos_Positive", "MonthlyTargetInCentavos > 0");
                });

            migrationBuilder.CreateTable(
                name: "CategoryMonthlyBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountInCentavos = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMonthlyBudgets", x => x.Id);
                    table.CheckConstraint("CK_CategoryMonthlyBudgets_AmountInCentavos_Positive", "AmountInCentavos > 0");
                    table.CheckConstraint("CK_CategoryMonthlyBudgets_Month", "Month BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_CategoryMonthlyBudgets_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AmountInCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("CK_Expenses_AmountInCentavos_Positive", "AmountInCentavos > 0");
                    table.ForeignKey(
                        name: "FK_Expenses_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavingsContributions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SavingsGoalId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountInCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    ContributionDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsContributions", x => x.Id);
                    table.CheckConstraint("CK_SavingsContributions_AmountInCentavos_Positive", "AmountInCentavos > 0");
                    table.ForeignKey(
                        name: "FK_SavingsContributions_SavingsGoals_SavingsGoalId",
                        column: x => x.SavingsGoalId,
                        principalTable: "SavingsGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetAlertDeliveries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PaymentTypeMonthlyLimitId = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryMonthlyBudgetId = table.Column<int>(type: "INTEGER", nullable: true),
                    ThresholdPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastFailure = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAlertDeliveries", x => x.Id);
                    table.CheckConstraint("CK_BudgetAlertDeliveries_ExactlyOneBudget", "(PaymentTypeMonthlyLimitId IS NOT NULL AND CategoryMonthlyBudgetId IS NULL) OR (PaymentTypeMonthlyLimitId IS NULL AND CategoryMonthlyBudgetId IS NOT NULL)");
                    table.CheckConstraint("CK_BudgetAlertDeliveries_ThresholdPercent", "ThresholdPercent IN (85, 100)");
                    table.ForeignKey(
                        name: "FK_BudgetAlertDeliveries_CategoryMonthlyBudgets_CategoryMonthlyBudgetId",
                        column: x => x.CategoryMonthlyBudgetId,
                        principalTable: "CategoryMonthlyBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetAlertDeliveries_PaymentTypeMonthlyLimits_PaymentTypeMonthlyLimitId",
                        column: x => x.PaymentTypeMonthlyLimitId,
                        principalTable: "PaymentTypeMonthlyLimits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAlertDeliveries_CategoryMonthlyBudgetId_ThresholdPercent",
                table: "BudgetAlertDeliveries",
                columns: new[] { "CategoryMonthlyBudgetId", "ThresholdPercent" },
                unique: true,
                filter: "CategoryMonthlyBudgetId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAlertDeliveries_PaymentTypeMonthlyLimitId_ThresholdPercent",
                table: "BudgetAlertDeliveries",
                columns: new[] { "PaymentTypeMonthlyLimitId", "ThresholdPercent" },
                unique: true,
                filter: "PaymentTypeMonthlyLimitId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryMonthlyBudgets_CategoryId_Year_Month",
                table: "CategoryMonthlyBudgets",
                columns: new[] { "CategoryId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId_ExpenseDate",
                table: "Expenses",
                columns: new[] { "CategoryId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PaymentType_ExpenseDate",
                table: "Expenses",
                columns: new[] { "PaymentType", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTypeMonthlyLimits_Year_Month_PaymentType",
                table: "PaymentTypeMonthlyLimits",
                columns: new[] { "Year", "Month", "PaymentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsContributions_SavingsGoalId_ContributionDate",
                table: "SavingsContributions",
                columns: new[] { "SavingsGoalId", "ContributionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAlertDeliveries");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "SavingsContributions");

            migrationBuilder.DropTable(
                name: "CategoryMonthlyBudgets");

            migrationBuilder.DropTable(
                name: "PaymentTypeMonthlyLimits");

            migrationBuilder.DropTable(
                name: "SavingsGoals");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
