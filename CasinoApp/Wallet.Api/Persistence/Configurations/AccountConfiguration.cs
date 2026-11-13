using CasinoApp.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CasinoApp.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", t =>
            t.HasCheckConstraint("ck_accounts_balance_non_negative", "\"BalanceCents\" >= 0"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PlayerName)
            .HasMaxLength(Account.MaxNameLength)
            .IsRequired();

        builder.HasIndex(a => a.PlayerName).IsUnique();

        builder.Property<uint>("Version").IsRowVersion();
    }
}