using System.Globalization;
using HotelErp.Api.Data;
using HotelErp.Api.DTOs;
using HotelErp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelErp.Api.Services;

public class FolioService
{
    private readonly AppDbContext _db;

    public FolioService(AppDbContext db) => _db = db;

    // Charges minus payments. A positive number is what the guest still owes.
    public static decimal BalanceOf(IEnumerable<FolioLine> lines)
    {
        var all = lines.ToList();
        var charges = all.Where(l => l.Type != FolioLineType.Payment).Sum(l => l.Amount);
        var payments = all.Where(l => l.Type == FolioLineType.Payment).Sum(l => l.Amount);
        return charges - payments;
    }

    public async Task<FolioDto?> GetAsync(int folioId)
    {
        var folio = await _db.Folios.AsNoTracking().WithDetails().FirstOrDefaultAsync(f => f.Id == folioId);
        return folio is null ? null : FolioMapping.ToDto(folio);
    }

    public async Task<FolioDto?> GetByStayAsync(int stayId)
    {
        var folio = await _db.Folios.AsNoTracking().WithDetails().FirstOrDefaultAsync(f => f.StayId == stayId);
        return folio is null ? null : FolioMapping.ToDto(folio);
    }

    // ===== Service charge or tax =====
    public Task<ServiceResult<FolioDto>> AddChargeAsync(int folioId, ChargeRequest req, int userId) =>
        PostAsync(folioId, folio =>
        {
            if (!HasAtMostTwoDecimals(req.Amount))
                return (400, "Amounts can have at most 2 decimal places");

            var typeName = string.IsNullOrWhiteSpace(req.Type) ? nameof(FolioLineType.ServiceCharge) : req.Type;
            if (!Enum.TryParse<FolioLineType>(typeName, true, out var type) ||
                (type != FolioLineType.ServiceCharge && type != FolioLineType.Tax))
                return (400, "Type must be ServiceCharge or Tax");

            folio.Lines.Add(new FolioLine
            {
                Type = type,
                Description = req.Description.Trim(),
                Amount = req.Amount,
                PostedAt = DateTime.UtcNow,
                PostedById = userId
            });
            return null;
        });

    // ===== Payment =====
    public Task<ServiceResult<FolioDto>> AddPaymentAsync(int folioId, PaymentRequest req, int userId) =>
        PostAsync(folioId, folio =>
        {
            if (!HasAtMostTwoDecimals(req.Amount))
                return (400, "Amounts can have at most 2 decimal places");

            if (!Enum.TryParse<PaymentMethod>(req.Method, true, out var method) || !Enum.IsDefined(method))
                return (400, "Unknown payment method. Use Cash, Card, MobileMoney or BankTransfer");

            var due = BalanceOf(folio.Lines);
            if (req.Amount > due)
                return (409, $"The payment is more than the amount due ({Money(due)})");

            var note = string.IsNullOrWhiteSpace(req.Note) ? "" : $": {req.Note.Trim()}";
            folio.Lines.Add(new FolioLine
            {
                Type = FolioLineType.Payment,
                Description = $"Payment ({method}){note}",
                Amount = req.Amount,
                PaymentMethod = method,
                PostedAt = DateTime.UtcNow,
                PostedById = userId
            });
            return null;
        });

    // ===== Discount or correction (managers only, enforced in the controller) =====
    public Task<ServiceResult<FolioDto>> AddAdjustmentAsync(int folioId, AdjustmentRequest req, int userId) =>
        PostAsync(folioId, folio =>
        {
            if (req.Amount == 0)
                return (400, "The adjustment amount cannot be zero");
            if (!HasAtMostTwoDecimals(req.Amount))
                return (400, "Amounts can have at most 2 decimal places");

            if (BalanceOf(folio.Lines) + req.Amount < 0)
                return (409, "This adjustment would make the balance negative. Refunds are not supported");

            folio.Lines.Add(new FolioLine
            {
                Type = FolioLineType.Adjustment,
                Description = $"Adjustment: {req.Reason.Trim()}",
                Amount = req.Amount,
                PostedAt = DateTime.UtcNow,
                PostedById = userId
            });
            return null;
        });

    // Lock the folio, check it is open, let the caller add a line, recalculate and save
    private async Task<ServiceResult<FolioDto>> PostAsync(int folioId, Func<Folio, (int Code, string Message)?> change)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // One change at a time per folio, so two payments cannot both pass the "amount due" check
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(2, {folioId})");

        var folio = await _db.Folios.Include(f => f.Lines).FirstOrDefaultAsync(f => f.Id == folioId);
        if (folio is null) return Fail(404, "Folio not found");
        if (folio.Status != FolioStatus.Open) return Fail(409, "This folio is closed and cannot be changed");

        var error = change(folio);
        if (error is not null) return Fail(error.Value.Code, error.Value.Message);

        folio.Balance = decimal.Round(BalanceOf(folio.Lines), 2);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return ServiceResult<FolioDto>.Ok((await GetAsync(folioId))!);
    }

    private static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static ServiceResult<FolioDto> Fail(int statusCode, string message) =>
        ServiceResult<FolioDto>.Fail(statusCode, message);
}