using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Modules.Sales.Application;
using Monolithic.Api.Modules.Sales.Customers.Application;

namespace Monolithic.Api.Modules.Sales;

public static class SalesModuleRegistration
{
    public static IServiceCollection AddSalesModule(this IServiceCollection services)
    {
        // ── Customers ────────────────────────────────────────────────────────────
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerContactService, CustomerContactService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();

        // ── Quotations & Sales Orders ────────────────────────────────────────
        services.AddScoped<IQuotationService, QuotationService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();

        // ── Accounts Receivable — Invoices & Credit Notes ────────────────────
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<IArCreditNoteService, ArCreditNoteService>();

        return services;
    }
}
