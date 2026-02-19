using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Domain;
using BusinessDomain = Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Identity.Infrastructure.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    // Identity
    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Employee> Employees => Set<Employee>();

    // Business
    public DbSet<BusinessDomain.Business> Businesses => Set<BusinessDomain.Business>();

    public DbSet<BusinessDomain.Industry> Industries => Set<BusinessDomain.Industry>();

    public DbSet<BusinessDomain.BusinessIndustry> BusinessIndustries => Set<BusinessDomain.BusinessIndustry>();

    public DbSet<BusinessDomain.BusinessContact> BusinessContacts => Set<BusinessDomain.BusinessContact>();

    public DbSet<BusinessDomain.Currency> Currencies => Set<BusinessDomain.Currency>();

    public DbSet<BusinessDomain.ExchangeRate> ExchangeRates => Set<BusinessDomain.ExchangeRate>();

    public DbSet<BusinessDomain.ChartOfAccount> ChartOfAccounts => Set<BusinessDomain.ChartOfAccount>();

    public DbSet<BusinessDomain.Vendor> Vendors => Set<BusinessDomain.Vendor>();

    public DbSet<BusinessDomain.EstimatePurchaseOrder> EstimatePurchaseOrders => Set<BusinessDomain.EstimatePurchaseOrder>();

    public DbSet<BusinessDomain.EstimatePurchaseOrderItem> EstimatePurchaseOrderItems => Set<BusinessDomain.EstimatePurchaseOrderItem>();

    public DbSet<BusinessDomain.PurchaseOrder> PurchaseOrders => Set<BusinessDomain.PurchaseOrder>();

    public DbSet<BusinessDomain.PurchaseOrderItem> PurchaseOrderItems => Set<BusinessDomain.PurchaseOrderItem>();

    public DbSet<BusinessDomain.VendorBill> VendorBills => Set<BusinessDomain.VendorBill>();

    public DbSet<BusinessDomain.VendorBillItem> VendorBillItems => Set<BusinessDomain.VendorBillItem>();

    public DbSet<BusinessDomain.VendorBillPayment> VendorBillPayments => Set<BusinessDomain.VendorBillPayment>();

    public DbSet<BusinessDomain.CostingSetup> CostingSetups => Set<BusinessDomain.CostingSetup>();

    public DbSet<BusinessDomain.CostLedgerEntry> CostLedgerEntries => Set<BusinessDomain.CostLedgerEntry>();

    public DbSet<BusinessDomain.BankAccountBase> BankAccounts => Set<BusinessDomain.BankAccountBase>();

    public DbSet<BusinessDomain.BusinessBankAccount> BusinessBankAccounts => Set<BusinessDomain.BusinessBankAccount>();

    public DbSet<BusinessDomain.VendorBankAccount> VendorBankAccounts => Set<BusinessDomain.VendorBankAccount>();

    public DbSet<BusinessDomain.CustomerBankAccount> CustomerBankAccounts => Set<BusinessDomain.CustomerBankAccount>();

    // Customers
    public DbSet<BusinessDomain.Customer> Customers => Set<BusinessDomain.Customer>();

    public DbSet<BusinessDomain.CustomerContact> CustomerContacts => Set<BusinessDomain.CustomerContact>();

    public DbSet<BusinessDomain.CustomerAddress> CustomerAddresses => Set<BusinessDomain.CustomerAddress>();

    // Accounts Payable — vendor management & payment
    public DbSet<BusinessDomain.VendorCreditTerm> VendorCreditTerms => Set<BusinessDomain.VendorCreditTerm>();
    public DbSet<BusinessDomain.VendorClass> VendorClasses => Set<BusinessDomain.VendorClass>();
    public DbSet<BusinessDomain.VendorProfile> VendorProfiles => Set<BusinessDomain.VendorProfile>();
    public DbSet<BusinessDomain.ApPaymentSession> ApPaymentSessions => Set<BusinessDomain.ApPaymentSession>();
    public DbSet<BusinessDomain.ApPaymentSessionLine> ApPaymentSessionLines => Set<BusinessDomain.ApPaymentSessionLine>();
    public DbSet<BusinessDomain.ApCreditNote> ApCreditNotes => Set<BusinessDomain.ApCreditNote>();
    public DbSet<BusinessDomain.ApCreditNoteApplication> ApCreditNoteApplications => Set<BusinessDomain.ApCreditNoteApplication>();
    public DbSet<BusinessDomain.ApPaymentSchedule> ApPaymentSchedules => Set<BusinessDomain.ApPaymentSchedule>();

    // Multi-business / multi-branch
    public DbSet<BusinessDomain.BusinessLicense> BusinessLicenses => Set<BusinessDomain.BusinessLicense>();
    public DbSet<BusinessDomain.BusinessOwnership> BusinessOwnerships => Set<BusinessDomain.BusinessOwnership>();
    public DbSet<BusinessDomain.BusinessBranch> BusinessBranches => Set<BusinessDomain.BusinessBranch>();
    public DbSet<BusinessDomain.BranchEmployee> BranchEmployees => Set<BusinessDomain.BranchEmployee>();
    public DbSet<BusinessDomain.BusinessSetting> BusinessSettings => Set<BusinessDomain.BusinessSetting>();
    public DbSet<BusinessDomain.BusinessMedia> BusinessMedia => Set<BusinessDomain.BusinessMedia>();
    public DbSet<BusinessDomain.BusinessHoliday> BusinessHolidays => Set<BusinessDomain.BusinessHoliday>();
    public DbSet<BusinessDomain.AttendancePolicy> AttendancePolicies => Set<BusinessDomain.AttendancePolicy>();

    // General Ledger
    public DbSet<BusinessDomain.JournalEntry> JournalEntries => Set<BusinessDomain.JournalEntry>();

    public DbSet<BusinessDomain.JournalEntryLine> JournalEntryLines => Set<BusinessDomain.JournalEntryLine>();

    public DbSet<BusinessDomain.JournalEntryAuditLog> JournalEntryAuditLogs => Set<BusinessDomain.JournalEntryAuditLog>();

    // Inventory
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();

    public DbSet<Stock> Stocks => Set<Stock>();

    public DbSet<InventoryItemVariant> InventoryItemVariants => Set<InventoryItemVariant>();

    public DbSet<InventoryItemVariantAttribute> InventoryItemVariantAttributes => Set<InventoryItemVariantAttribute>();

    public DbSet<InventoryItemImage> InventoryItemImages => Set<InventoryItemImage>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rename Identity tables to follow convention
        modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("AspNetRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("AspNetUserClaims");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("AspNetRoleClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("AspNetUserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("AspNetUserTokens");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("AspNetUserRoles");

        // Configure Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure RolePermission (composite key + relationships)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserPermission (composite key + relationships)
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PermissionId });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(e => e.UserPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Contact and Employee inheritance (TPH - Table Per Hierarchy)
        modelBuilder.Entity<ApplicationUser>()
            .HasDiscriminator<string>("UserType")
            .HasValue<ApplicationUser>("User")
            .HasValue<Contact>("Contact")
            .HasValue<Employee>("Employee");

        // Configure Contact
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.Property(e => e.JobTitle).HasMaxLength(120);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.HasOne(e => e.PrimaryBusiness)
                .WithMany()
                .HasForeignKey(e => e.PrimaryBusinessId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.JobTitle).HasMaxLength(120);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Employees)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.BusinessId, e.EmployeeNumber }).IsUnique();
        });

        // Configure Industry
        modelBuilder.Entity<BusinessDomain.Industry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure Business
        modelBuilder.Entity<BusinessDomain.Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ShortName).HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.ShortDescription).HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(e => e.LocalName).HasMaxLength(200);
            entity.Property(e => e.VatTin).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            
            entity.HasOne(e => e.PrimaryContact)
                .WithMany()
                .HasForeignKey(e => e.PrimaryContactId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.OwnerEmployee)
                .WithMany()
                .HasForeignKey(e => e.OwnerEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure BusinessIndustry (many-to-many)
        modelBuilder.Entity<BusinessDomain.BusinessIndustry>(entity =>
        {
            entity.HasKey(e => new { e.BusinessId, e.IndustryId });
            entity.HasOne(e => e.Business)
                .WithMany(b => b.BusinessIndustries)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Industry)
                .WithMany(i => i.BusinessIndustries)
                .HasForeignKey(e => e.IndustryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BusinessContact (many-to-many with additional properties)
        modelBuilder.Entity<BusinessDomain.BusinessContact>(entity =>
        {
            entity.HasKey(e => new { e.BusinessId, e.ContactId });
            entity.Property(e => e.Role).HasMaxLength(100);
            
            entity.HasOne(e => e.Business)
                .WithMany(b => b.BusinessContacts)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.BusinessContacts)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Vendor
        modelBuilder.Entity<BusinessDomain.Vendor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Vendors)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure VendorCreditTerm
        modelBuilder.Entity<BusinessDomain.VendorCreditTerm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EarlyPayDiscountPercent).HasPrecision(8, 4);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.Name }).IsUnique();
        });

        // Configure VendorClass
        modelBuilder.Entity<BusinessDomain.VendorClass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ColorHex).HasMaxLength(10);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.Code }).IsUnique();
        });

        // Configure VendorProfile (shared PK = VendorId)
        modelBuilder.Entity<BusinessDomain.VendorProfile>(entity =>
        {
            entity.HasKey(e => e.VendorId);
            entity.Property(e => e.DefaultVatPercent).HasPrecision(8, 4);
            entity.Property(e => e.VatRegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.CreditLimitBase).HasPrecision(18, 2);
            entity.Property(e => e.PreferredPaymentMethod).HasMaxLength(50);
            entity.Property(e => e.MinimumPaymentAmount).HasPrecision(18, 2);
            entity.Property(e => e.PerformanceRating).HasPrecision(3, 2);
            entity.Property(e => e.RelationshipNotes).HasMaxLength(2000);
            entity.Property(e => e.HoldReason).HasMaxLength(500);
            entity.Property(e => e.BlacklistReason).HasMaxLength(500);

            entity.HasOne(e => e.Vendor)
                .WithOne(v => v.Profile)
                .HasForeignKey<BusinessDomain.VendorProfile>(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreditTerm)
                .WithMany(t => t.VendorProfiles)
                .HasForeignKey(e => e.CreditTermId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(e => e.VendorClass)
                .WithMany(c => c.VendorProfiles)
                .HasForeignKey(e => e.VendorClassId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(e => e.PreferredBankAccount)
                .WithMany()
                .HasForeignKey(e => e.PreferredBankAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // Configure ApPaymentSession
        modelBuilder.Entity<BusinessDomain.ApPaymentSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.PaymentMode).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmountBase).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 8);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                .WithMany()
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BankAccount)
                .WithMany()
                .HasForeignKey(e => e.BankAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.VendorId, e.Status });
        });

        // Configure ApPaymentSessionLine
        modelBuilder.Entity<BusinessDomain.ApPaymentSessionLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllocatedAmount).HasPrecision(18, 2);
            entity.Property(e => e.BillAmountDueBefore).HasPrecision(18, 2);
            entity.Property(e => e.BillAmountDueAfter).HasPrecision(18, 2);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Lines)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VendorBill)
                .WithMany()
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VendorBillPayment)
                .WithMany()
                .HasForeignKey(e => e.VendorBillPaymentId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.SessionId, e.VendorBillId });
        });

        // Configure ApCreditNote
        modelBuilder.Entity<BusinessDomain.ApCreditNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreditNoteNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VendorReference).HasMaxLength(100);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmountBase).HasPrecision(18, 2);
            entity.Property(e => e.AmountApplied).HasPrecision(18, 2);
            entity.Property(e => e.AmountRemaining).HasPrecision(18, 2);
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 8);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.CreditNotes)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.OriginalVendorBill)
                .WithMany()
                .HasForeignKey(e => e.OriginalVendorBillId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.CreditNoteNumber }).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.VendorId, e.Status });
        });

        // Configure ApCreditNoteApplication
        modelBuilder.Entity<BusinessDomain.ApCreditNoteApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AppliedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.CreditNote)
                .WithMany(c => c.Applications)
                .HasForeignKey(e => e.CreditNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.VendorBill)
                .WithMany(b => b.CreditNoteApplications)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.CreditNoteId, e.VendorBillId });
        });

        // Configure ApPaymentSchedule
        modelBuilder.Entity<BusinessDomain.ApPaymentSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ScheduledAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.PaymentSchedules)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VendorBill)
                .WithMany(b => b.PaymentSchedules)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ExecutedSession)
                .WithMany()
                .HasForeignKey(e => e.ExecutedSessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.VendorId, e.Status, e.ScheduledDate });
        });

        // Configure Currency
        modelBuilder.Entity<BusinessDomain.Currency>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Symbol).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // Configure ExchangeRate
        modelBuilder.Entity<BusinessDomain.ExchangeRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromCurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.ToCurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Rate).HasPrecision(18, 8);
            entity.Property(e => e.Source).HasMaxLength(100);

            entity.HasOne(e => e.FromCurrency)
                .WithMany(c => c.ExchangeRatesFrom)
                .HasForeignKey(e => e.FromCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ToCurrency)
                .WithMany(c => c.ExchangeRatesTo)
                .HasForeignKey(e => e.ToCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.FromCurrencyCode, e.ToCurrencyCode, e.EffectiveDate });
        });

        // Configure ChartOfAccount
        modelBuilder.Entity<BusinessDomain.ChartOfAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AccountType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.AccountCategory).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.ChartOfAccounts)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ParentAccount)
                .WithMany(p => p.ChildAccounts)
                .HasForeignKey(e => e.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.AccountNumber }).IsUnique();
        });

        // Configure EstimatePurchaseOrder
        modelBuilder.Entity<BusinessDomain.EstimatePurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RfqNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.VendorQuoteReference).HasMaxLength(100);
            entity.Property(e => e.OrderDiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendor)
                .WithMany()
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.BusinessId, e.RfqNumber }).IsUnique();
        });

        // Configure EstimatePurchaseOrderItem
        modelBuilder.Entity<BusinessDomain.EstimatePurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.DiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.EstimatePurchaseOrder)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.EstimatePurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItemVariant)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemVariantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        // Configure PurchaseOrder
        modelBuilder.Entity<BusinessDomain.PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PoNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.InternalNotes).HasMaxLength(1000);
            entity.Property(e => e.HoldReason).HasMaxLength(500);
            entity.Property(e => e.OrderDiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.PurchaseOrders)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EstimatePurchaseOrder)
                .WithMany(e => e.PurchaseOrders)
                .HasForeignKey(e => e.EstimatePurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(e => e.ChartOfAccount)
                .WithMany(c => c.LinkedPurchaseOrders)
                .HasForeignKey(e => e.ChartOfAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.PoNumber }).IsUnique();
        });

        // Configure PurchaseOrderItem
        modelBuilder.Entity<BusinessDomain.PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.DiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem)
                .WithMany(ii => ii.PurchaseOrderItems)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItemVariant)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemVariantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        // Configure VendorBill
        modelBuilder.Entity<BusinessDomain.VendorBill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VendorInvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.InternalNotes).HasMaxLength(1000);
            entity.Property(e => e.OrderDiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.VendorBills)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.VendorBills)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(po => po.VendorBills)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(e => e.ChartOfAccount)
                .WithMany(c => c.LinkedVendorBills)
                .HasForeignKey(e => e.ChartOfAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.BillNumber }).IsUnique();
            // Index to query overdue bills efficiently
            entity.HasIndex(e => new { e.BusinessId, e.VendorId, e.DueDate, e.AmountDue });
        });

        // Configure VendorBillItem
        modelBuilder.Entity<BusinessDomain.VendorBillItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.DiscountType).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.VendorBill)
                .WithMany(b => b.Items)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PurchaseOrderItem)
                .WithMany(poi => poi.BillItems)
                .HasForeignKey(e => e.PurchaseOrderItemId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItemVariant)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemVariantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        // Configure VendorBillPayment
        modelBuilder.Entity<BusinessDomain.VendorBillPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.VendorBill)
                .WithMany(b => b.Payments)
                .HasForeignKey(e => e.VendorBillId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.BankAccount)
                .WithMany()
                .HasForeignKey(e => e.BankAccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // Configure CostingSetup
        modelBuilder.Entity<BusinessDomain.CostingSetup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostingMethod).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.OverheadPercentage).HasPrecision(8, 4);
            entity.Property(e => e.LandedCostPercentage).HasPrecision(8, 4);

            entity.HasOne(e => e.Business)
                .WithMany(b => b.CostingSetups)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // One costing setup per business (or per business+item)
            entity.HasIndex(e => new { e.BusinessId, e.InventoryItemId }).IsUnique();
        });

        // Configure CostLedgerEntry
        modelBuilder.Entity<BusinessDomain.CostLedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.UnitCost).HasPrecision(18, 6);
            entity.Property(e => e.TotalCost).HasPrecision(18, 6);
            entity.Property(e => e.AverageUnitCostAfter).HasPrecision(18, 6);
            entity.Property(e => e.StockValueAfter).HasPrecision(18, 6);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryItemVariant)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemVariantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.InventoryItemId, e.EntryDateUtc });
        });

        // Configure BankAccount (TPH for Business/Vendor/Customer)
        modelBuilder.Entity<BusinessDomain.BankAccountBase>(entity =>
        {
            entity.ToTable("BankAccounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AccountNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BankName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BranchName).HasMaxLength(200);
            entity.Property(e => e.SwiftCode).HasMaxLength(50);
            entity.Property(e => e.RoutingNumber).HasMaxLength(50);
            entity.Property(e => e.CurrencyCode).HasMaxLength(10).IsRequired();

            entity.HasDiscriminator<string>("OwnerType")
                .HasValue<BusinessDomain.BusinessBankAccount>("Business")
                .HasValue<BusinessDomain.VendorBankAccount>("Vendor")
                .HasValue<BusinessDomain.CustomerBankAccount>("Customer");
        });

        modelBuilder.Entity<BusinessDomain.BusinessBankAccount>(entity =>
        {
            entity.Property(e => e.BusinessId).IsRequired();
            entity.HasOne(e => e.Business)
                .WithMany(b => b.BankAccounts)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BusinessId, e.AccountNumber }).IsUnique();
        });

        modelBuilder.Entity<BusinessDomain.VendorBankAccount>(entity =>
        {
            entity.Property(e => e.VendorId).IsRequired();
            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.BankAccounts)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.VendorId, e.AccountNumber }).IsUnique();
        });

        modelBuilder.Entity<BusinessDomain.CustomerBankAccount>(entity =>
        {
            entity.Property(e => e.CustomerId).IsRequired();
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.BankAccounts)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CustomerId, e.AccountNumber }).IsUnique();
        });

        // ── Customer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerCode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.PaymentTerms).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(300);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.BusinessId, e.CustomerCode })
                .IsUnique()
                .HasFilter("[CustomerCode] <> ''");
        });

        // ── CustomerContact ───────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.CustomerContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.JobTitle).HasMaxLength(120);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Contacts)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CustomerId, e.IsPrimary });
        });

        // ── CustomerAddress ───────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.CustomerAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AddressType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AddressLine1).HasMaxLength(300).IsRequired();
            entity.Property(e => e.AddressLine2).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PostalCode).HasMaxLength(20);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Addresses)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
        });

        // ── BusinessLicense ───────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessLicense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Plan).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ExternalSubscriptionId).HasMaxLength(200);
            entity.HasIndex(e => e.OwnerId);
        });

        // ── BusinessOwnership ─────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessOwnership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.IsActive);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Ownerships)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.License)
                .WithMany(l => l.Ownerships)
                .HasForeignKey(e => e.LicenseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OwnerId, e.BusinessId }).IsUnique();
            entity.HasIndex(e => new { e.OwnerId, e.RevokedAtUtc });
        });

        // ── BusinessBranch ────────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessBranch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.PhoneNumber).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.TimezoneId).HasMaxLength(100);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Branches)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BusinessId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.IsHeadquarters });
        });

        // ── BranchEmployee ────────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BranchEmployee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.IsActive);
            entity.HasOne(e => e.Branch)
                .WithMany(b => b.BranchEmployees)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BranchId, e.EmployeeId });
        });

        // ── BusinessSetting ───────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.SecondaryColor).HasMaxLength(20);
            entity.Property(e => e.AccentColor).HasMaxLength(20);
            entity.Property(e => e.TimezoneId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayCurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Locale).HasMaxLength(20).IsRequired();
            entity.Property(e => e.HolidayCountryCode).HasMaxLength(5);
            entity.HasOne(e => e.Business)
                .WithOne(b => b.Settings)
                .HasForeignKey<BusinessDomain.BusinessSetting>(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.BusinessId).IsUnique();
        });

        // ── BusinessMedia ─────────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MediaType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.PublicUrl).HasMaxLength(2000);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.AltText).HasMaxLength(300);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Media)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BusinessId, e.MediaType, e.IsCurrent });
        });

        // ── BusinessHoliday ───────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.BusinessHoliday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.CountryCode).HasMaxLength(5);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.Holidays)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.BusinessId, e.ExternalId });
            entity.HasIndex(e => new { e.BusinessId, e.Date });
        });

        // ── AttendancePolicy ──────────────────────────────────────────────────
        modelBuilder.Entity<BusinessDomain.AttendancePolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Scope).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.RequiredHoursPerDay).HasPrecision(5, 2);
            entity.Property(e => e.ExcludedHolidayIds).HasMaxLength(2000);
            entity.HasOne(e => e.Business)
                .WithMany(b => b.AttendancePolicies)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Branch)
                .WithMany(br => br.AttendancePolicies)
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
            entity.HasIndex(e => new { e.BusinessId, e.BranchId, e.Scope });
        });

        // Configure InventoryItem
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasIndex(e => new { e.BusinessId, e.Sku }).IsUnique();
        });

        // Configure InventoryItemVariant
        modelBuilder.Entity<InventoryItemVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SkuSuffix).HasMaxLength(100);

            entity.HasOne(e => e.InventoryItem)
                .WithMany(i => i.Variants)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.InventoryItemId, e.Name }).IsUnique();
        });

        // Configure InventoryItemVariantAttribute
        modelBuilder.Entity<InventoryItemVariantAttribute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AttributeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AttributeValue).HasMaxLength(200).IsRequired();

            entity.HasOne(e => e.Variant)
                .WithMany(v => v.Attributes)
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Each attribute name is unique per variant
            entity.HasIndex(e => new { e.VariantId, e.AttributeName }).IsUnique();
        });

        // Configure InventoryItemImage
        modelBuilder.Entity<InventoryItemImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.AltText).HasMaxLength(500);

            entity.HasOne(e => e.InventoryItem)
                .WithMany(i => i.Images)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Variant FK is nullable (null = main image for all variants)
            entity.HasOne(e => e.Variant)
                .WithMany(v => v.Images)
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        // Configure InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.InventoryItem)
                .WithMany(ii => ii.Transactions)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WarehouseLocation)
                .WithMany()
                .HasForeignKey(e => e.WarehouseLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);

            entity.HasIndex(e => new { e.BusinessId, e.Code }).IsUnique();
        });

        // Configure WarehouseLocation
        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Zone).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Locations)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.WarehouseId, e.Code }).IsUnique();
        });

        // Configure Stock
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Each item can only have one stock record per location
            entity.HasIndex(e => new { e.InventoryItemId, e.WarehouseLocationId }).IsUnique();

            // QuantityAvailable is a computed property — not persisted
            entity.Ignore(e => e.QuantityAvailable);

            entity.HasOne(e => e.InventoryItem)
                .WithMany(ii => ii.Stocks)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.WarehouseLocation)
                .WithMany(wl => wl.Stocks)
                .HasForeignKey(e => e.WarehouseLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── General Ledger ────────────────────────────────────────────────────

        // Configure JournalEntry
        modelBuilder.Entity<BusinessDomain.JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryNumber).HasMaxLength(30).IsRequired();
            entity.Property(e => e.FiscalPeriod).HasMaxLength(7).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.SourceType).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.SourceDocumentReference).HasMaxLength(200);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.ExchangeRate).HasPrecision(18, 8);
            entity.Property(e => e.TotalDebits).HasPrecision(18, 2);
            entity.Property(e => e.TotalCredits).HasPrecision(18, 2);

            entity.HasOne(e => e.Business)
                .WithMany()
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing: reversal chain — only configure the FK side
            entity.HasOne(e => e.ReversalOfEntry)
                .WithMany()
                .HasForeignKey(e => e.ReversalOfEntryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // ReversedByEntryId is a plain nullable FK column (no navigation configured)
            entity.Property(e => e.ReversedByEntryId).IsRequired(false);

            entity.HasIndex(e => new { e.BusinessId, e.EntryNumber }).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.FiscalPeriod });
            entity.HasIndex(e => new { e.BusinessId, e.TransactionDate });
            entity.HasIndex(e => e.Status);
        });

        // Configure JournalEntryLine
        modelBuilder.Entity<BusinessDomain.JournalEntryLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DebitAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.DebitAmountBase).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmountBase).HasPrecision(18, 2);
            entity.Property(e => e.CostCenter).HasMaxLength(50);
            entity.Property(e => e.ProjectCode).HasMaxLength(50);
            entity.Property(e => e.LineDescription).HasMaxLength(300);

            entity.HasOne(e => e.JournalEntry)
                .WithMany(je => je.Lines)
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.JournalEntryId, e.LineNumber }).IsUnique();
            entity.HasIndex(e => e.AccountId);
        });

        // Configure JournalEntryAuditLog
        modelBuilder.Entity<BusinessDomain.JournalEntryAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserDisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Action).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.JournalEntry)
                .WithMany(je => je.AuditLogs)
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.JournalEntryId);
            entity.HasIndex(e => e.OccurredAtUtc);
        });
    }
}
