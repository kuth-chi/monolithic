using Monolithic.Api.Modules.Sales.Contracts;

namespace Monolithic.Api.Modules.Sales.Application;

public interface IQuotationService
{
    Task<IReadOnlyList<QuotationDto>> GetByBusinessAsync(Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default);
    Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> CreateAsync(CreateQuotationRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<QuotationDto> UpdateAsync(Guid id, UpdateQuotationRequest request, CancellationToken ct = default);
    Task SendAsync(Guid id, CancellationToken ct = default);
    Task<SalesOrderDto> ConvertToOrderAsync(Guid id, ConvertQuotationRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
}

public interface ISalesOrderService
{
    Task<IReadOnlyList<SalesOrderDto>> GetByBusinessAsync(Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default);
    Task<SalesOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<SalesOrderDto> UpdateAsync(Guid id, UpdateSalesOrderRequest request, CancellationToken ct = default);
    Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
}

public interface ISalesInvoiceService
{
    Task<IReadOnlyList<SalesInvoiceDto>> GetByBusinessAsync(Guid businessId, Guid? customerId = null, string? status = null, CancellationToken ct = default);
    Task<SalesInvoiceDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SalesInvoiceDto>> GetOverdueAsync(Guid businessId, Guid? customerId = null, CancellationToken ct = default);
    Task<ArDashboardDto> GetDashboardAsync(Guid businessId, CancellationToken ct = default);
    Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task SendAsync(Guid id, CancellationToken ct = default);
    Task<SalesInvoicePaymentDto> RecordPaymentAsync(Guid id, RecordSalesPaymentRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
    Task RefreshOverdueStatusAsync(Guid businessId, CancellationToken ct = default);
}

public interface IArCreditNoteService
{
    Task<IReadOnlyList<ArCreditNoteDto>> GetByBusinessAsync(Guid businessId, Guid? customerId = null, CancellationToken ct = default);
    Task<ArCreditNoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ArCreditNoteDto> CreateAsync(CreateArCreditNoteRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default);
    Task<ArCreditNoteApplicationDto> ApplyAsync(Guid creditNoteId, ApplyArCreditNoteRequest request, Guid appliedByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
}
