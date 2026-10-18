namespace PersonalFinance.Web.Data.Entities;

/// <summary>The single local owner account. PasswordHash is produced by ASP.NET Core's PasswordHasher.</summary>
public sealed class AppCredential
{
    public int Id { get; set; } = 1;

    public string PasswordHash { get; set; } = string.Empty;

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAtUtc { get; set; }

    public DateTime PasswordChangedAtUtc { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutUntilUtc { get; set; }
}
