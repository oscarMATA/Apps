using Microsoft.EntityFrameworkCore;
using PersonalFinance.Web.Data.Entities;

namespace PersonalFinance.Web.Data;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AppCredential> AppCredentials => Set<AppCredential>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<RecurringExpense> RecurringExpenses => Set<RecurringExpense>();

    public DbSet<PaymentTypeMonthlyLimit> PaymentTypeMonthlyLimits => Set<PaymentTypeMonthlyLimit>();

    public DbSet<CategoryMonthlyBudget> CategoryMonthlyBudgets => Set<CategoryMonthlyBudget>();

    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();

    public DbSet<SavingsContribution> SavingsContributions => Set<SavingsContribution>();

    public DbSet<BudgetAlertDelivery> BudgetAlertDeliveries => Set<BudgetAlertDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppCredential>(entity =>
        {
            entity.HasKey(credential => credential.Id);
            entity.Property(credential => credential.PasswordHash).IsRequired();
            entity.Property(credential => credential.SecurityStamp).HasMaxLength(64).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint("CK_AppCredentials_SingleOwner", "Id = 1"));
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .UseCollation("NOCASE")
                .IsRequired();

            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(expense => expense.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(expense => expense.PaymentType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(expense => expense.ExpenseDate);
            entity.HasIndex(expense => new { expense.PaymentType, expense.ExpenseDate });
            entity.HasIndex(expense => new { expense.CategoryId, expense.ExpenseDate });
            entity.HasIndex(expense => new { expense.RecurringExpenseId, expense.ExpenseDate })
                .IsUnique()
                .HasFilter("RecurringExpenseId IS NOT NULL");
            entity.HasOne(expense => expense.Category)
                .WithMany(category => category.Expenses)
                .HasForeignKey(expense => expense.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(expense => expense.RecurringExpense)
                .WithMany(template => template.GeneratedExpenses)
                .HasForeignKey(expense => expense.RecurringExpenseId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Expenses_AmountInCentavos_Positive",
                "AmountInCentavos > 0"));
        });

        modelBuilder.Entity<RecurringExpense>(entity =>
        {
            entity.Property(template => template.Description).HasMaxLength(500).IsRequired();
            entity.Property(template => template.PaymentType).HasConversion<string>().HasMaxLength(30);
            entity.Property(template => template.Frequency).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(template => template.Category)
                .WithMany(category => category.RecurringExpenses)
                .HasForeignKey(template => template.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(template => template.NextDueDate);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_RecurringExpenses_AmountInCentavos_Positive",
                "AmountInCentavos > 0"));
        });

        modelBuilder.Entity<PaymentTypeMonthlyLimit>(entity =>
        {
            entity.Property(limit => limit.PaymentType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(limit => new { limit.Year, limit.Month, limit.PaymentType }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_PaymentTypeMonthlyLimits_Month", "Month BETWEEN 1 AND 12");
                table.HasCheckConstraint("CK_PaymentTypeMonthlyLimits_AmountInCentavos_Positive", "AmountInCentavos > 0");
            });
        });

        modelBuilder.Entity<CategoryMonthlyBudget>(entity =>
        {
            entity.HasIndex(budget => new { budget.CategoryId, budget.Year, budget.Month }).IsUnique();
            entity.HasOne(budget => budget.Category)
                .WithMany(category => category.MonthlyBudgets)
                .HasForeignKey(budget => budget.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_CategoryMonthlyBudgets_Month", "Month BETWEEN 1 AND 12");
                table.HasCheckConstraint("CK_CategoryMonthlyBudgets_AmountInCentavos_Positive", "AmountInCentavos > 0");
            });
        });

        modelBuilder.Entity<SavingsGoal>(entity =>
        {
            entity.Property(goal => goal.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SavingsGoals_MonthlyTargetInCentavos_Positive",
                "MonthlyTargetInCentavos > 0"));
        });

        modelBuilder.Entity<SavingsContribution>(entity =>
        {
            entity.Property(contribution => contribution.Description).HasMaxLength(500);
            entity.HasIndex(contribution => new { contribution.SavingsGoalId, contribution.ContributionDate });
            entity.HasOne(contribution => contribution.SavingsGoal)
                .WithMany(goal => goal.Contributions)
                .HasForeignKey(contribution => contribution.SavingsGoalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SavingsContributions_AmountInCentavos_Positive",
                "AmountInCentavos > 0"));
        });

        modelBuilder.Entity<BudgetAlertDelivery>(entity =>
        {
            entity.Property(delivery => delivery.LastFailure).HasMaxLength(2000);
            entity.HasOne(delivery => delivery.PaymentTypeMonthlyLimit)
                .WithMany(limit => limit.AlertDeliveries)
                .HasForeignKey(delivery => delivery.PaymentTypeMonthlyLimitId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(delivery => delivery.CategoryMonthlyBudget)
                .WithMany(budget => budget.AlertDeliveries)
                .HasForeignKey(delivery => delivery.CategoryMonthlyBudgetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(delivery => new { delivery.PaymentTypeMonthlyLimitId, delivery.ThresholdPercent })
                .IsUnique()
                .HasFilter("PaymentTypeMonthlyLimitId IS NOT NULL");
            entity.HasIndex(delivery => new { delivery.CategoryMonthlyBudgetId, delivery.ThresholdPercent })
                .IsUnique()
                .HasFilter("CategoryMonthlyBudgetId IS NOT NULL");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_BudgetAlertDeliveries_ExactlyOneBudget",
                    "(PaymentTypeMonthlyLimitId IS NOT NULL AND CategoryMonthlyBudgetId IS NULL) OR " +
                    "(PaymentTypeMonthlyLimitId IS NULL AND CategoryMonthlyBudgetId IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_BudgetAlertDeliveries_ThresholdPercent",
                    "ThresholdPercent IN (85, 100)");
            });
        });
    }
}
