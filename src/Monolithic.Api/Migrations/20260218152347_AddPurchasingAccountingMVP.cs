using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolithic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasingAccountingMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAtUtc",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChartOfAccountId",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EstimatePurchaseOrderId",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HoldReason",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HoldStartedAtUtc",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnHold",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderDiscountAmount",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OrderDiscountType",
                table: "PurchaseOrders",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OrderDiscountValue",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFee",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountBase",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "PurchaseOrderItems",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryItemVariantId",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotalAfterDiscount",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotalBeforeDiscount",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAtUtc",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityBilled",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "PurchaseOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "Businesses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChartOfAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AccountCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsHeaderAccount = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartOfAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChartOfAccounts_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChartOfAccounts_ChartOfAccounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "ChartOfAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CostingSetups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CostingMethod = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    StandardCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    OverheadPercentage = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false),
                    LabourCostPerUnit = table.Column<decimal>(type: "TEXT", nullable: false),
                    LandedCostPercentage = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostingSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostingSetups_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostingSetups_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemVariantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntryType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    AverageUnitCostAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    StockQuantityAfter = table.Column<decimal>(type: "TEXT", nullable: false),
                    StockValueAfter = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    FifoLayerRemaining = table.Column<decimal>(type: "TEXT", nullable: true),
                    FifoSourceLayerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntryDateUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostLedgerEntries_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostLedgerEntries_InventoryItemVariants_InventoryItemVariantId",
                        column: x => x.InventoryItemVariantId,
                        principalTable: "InventoryItemVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostLedgerEntries_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "EstimatePurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RequestDateUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    QuoteReceivedDateUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    QuoteExpiryDateUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderDiscountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrderDiscountValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderDiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmountBase = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    VendorQuoteReference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimatePurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimatePurchaseOrders_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimatePurchaseOrders_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorBills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ChartOfAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BillNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VendorInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    BillDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderDiscountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrderDiscountValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderDiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAmountBase = table.Column<decimal>(type: "TEXT", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "TEXT", nullable: false),
                    AmountDue = table.Column<decimal>(type: "TEXT", nullable: false),
                    DaysOverdue = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    InternalNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBills_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBills_ChartOfAccounts_ChartOfAccountId",
                        column: x => x.ChartOfAccountId,
                        principalTable: "ChartOfAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorBills_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorBills_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ToCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_FromCurrencyCode",
                        column: x => x.FromCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_ToCurrencyCode",
                        column: x => x.ToCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstimatePurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EstimatePurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemVariantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotalBeforeDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotalAfterDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimatePurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimatePurchaseOrderItems_EstimatePurchaseOrders_EstimatePurchaseOrderId",
                        column: x => x.EstimatePurchaseOrderId,
                        principalTable: "EstimatePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EstimatePurchaseOrderItems_InventoryItemVariants_InventoryItemVariantId",
                        column: x => x.InventoryItemVariantId,
                        principalTable: "InventoryItemVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstimatePurchaseOrderItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorBillItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemVariantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotalBeforeDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotalAfterDiscount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBillItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBillItems_InventoryItemVariants_InventoryItemVariantId",
                        column: x => x.InventoryItemVariantId,
                        principalTable: "InventoryItemVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBillItems_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBillItems_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorBillItems_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorBillPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AmountBase = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBillPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBillPayments_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VendorBillPayments_VendorBills_VendorBillId",
                        column: x => x.VendorBillId,
                        principalTable: "VendorBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ChartOfAccountId",
                table: "PurchaseOrders",
                column: "ChartOfAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_EstimatePurchaseOrderId",
                table: "PurchaseOrders",
                column: "EstimatePurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_InventoryItemVariantId",
                table: "PurchaseOrderItems",
                column: "InventoryItemVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_BusinessId_AccountNumber",
                table: "ChartOfAccounts",
                columns: new[] { "BusinessId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_ParentAccountId",
                table: "ChartOfAccounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CostingSetups_BusinessId_InventoryItemId",
                table: "CostingSetups",
                columns: new[] { "BusinessId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostingSetups_InventoryItemId",
                table: "CostingSetups",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostLedgerEntries_BusinessId_InventoryItemId_EntryDateUtc",
                table: "CostLedgerEntries",
                columns: new[] { "BusinessId", "InventoryItemId", "EntryDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CostLedgerEntries_InventoryItemId",
                table: "CostLedgerEntries",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostLedgerEntries_InventoryItemVariantId",
                table: "CostLedgerEntries",
                column: "InventoryItemVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimatePurchaseOrderItems_EstimatePurchaseOrderId",
                table: "EstimatePurchaseOrderItems",
                column: "EstimatePurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimatePurchaseOrderItems_InventoryItemId",
                table: "EstimatePurchaseOrderItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimatePurchaseOrderItems_InventoryItemVariantId",
                table: "EstimatePurchaseOrderItems",
                column: "InventoryItemVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimatePurchaseOrders_BusinessId_RfqNumber",
                table: "EstimatePurchaseOrders",
                columns: new[] { "BusinessId", "RfqNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstimatePurchaseOrders_VendorId",
                table: "EstimatePurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyCode_ToCurrencyCode_EffectiveDate",
                table: "ExchangeRates",
                columns: new[] { "FromCurrencyCode", "ToCurrencyCode", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_ToCurrencyCode",
                table: "ExchangeRates",
                column: "ToCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillItems_InventoryItemId",
                table: "VendorBillItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillItems_InventoryItemVariantId",
                table: "VendorBillItems",
                column: "InventoryItemVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillItems_PurchaseOrderItemId",
                table: "VendorBillItems",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillItems_VendorBillId",
                table: "VendorBillItems",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillPayments_BankAccountId",
                table: "VendorBillPayments",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBillPayments_VendorBillId",
                table: "VendorBillPayments",
                column: "VendorBillId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_BusinessId_BillNumber",
                table: "VendorBills",
                columns: new[] { "BusinessId", "BillNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_BusinessId_VendorId_DueDate_AmountDue",
                table: "VendorBills",
                columns: new[] { "BusinessId", "VendorId", "DueDate", "AmountDue" });

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_ChartOfAccountId",
                table: "VendorBills",
                column: "ChartOfAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_PurchaseOrderId",
                table: "VendorBills",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBills_VendorId",
                table: "VendorBills",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_InventoryItemVariants_InventoryItemVariantId",
                table: "PurchaseOrderItems",
                column: "InventoryItemVariantId",
                principalTable: "InventoryItemVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_ChartOfAccounts_ChartOfAccountId",
                table: "PurchaseOrders",
                column: "ChartOfAccountId",
                principalTable: "ChartOfAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_EstimatePurchaseOrders_EstimatePurchaseOrderId",
                table: "PurchaseOrders",
                column: "EstimatePurchaseOrderId",
                principalTable: "EstimatePurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_InventoryItemVariants_InventoryItemVariantId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_ChartOfAccounts_ChartOfAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_EstimatePurchaseOrders_EstimatePurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "CostingSetups");

            migrationBuilder.DropTable(
                name: "CostLedgerEntries");

            migrationBuilder.DropTable(
                name: "EstimatePurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "VendorBillItems");

            migrationBuilder.DropTable(
                name: "VendorBillPayments");

            migrationBuilder.DropTable(
                name: "EstimatePurchaseOrders");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "VendorBills");

            migrationBuilder.DropTable(
                name: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ChartOfAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_EstimatePurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_InventoryItemVariantId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ChartOfAccountId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "EstimatePurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "HoldReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "HoldStartedAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsOnHold",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderDiscountAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderDiscountType",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderDiscountValue",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ShippingFee",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TotalAmountBase",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "InventoryItemVariantId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LineTotalAfterDiscount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LineTotalBeforeDiscount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "QuantityBilled",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "Businesses");
        }
    }
}
