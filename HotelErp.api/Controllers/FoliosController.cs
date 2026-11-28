using System.Security.Claims;
using HotelErp.Api.DTOs;
using HotelErp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelErp.Api.Controllers;

[ApiController]
[Authorize]
public class FoliosController : ControllerBase
{
    private readonly FolioService _folios;

    public FoliosController(FolioService folios) => _folios = folios;

    [HttpGet("/api/folios/{id:int}")]
    public async Task<ActionResult<FolioDto>> Get(int id)
    {
        var dto = await _folios.GetAsync(id);
        if (dto is null) return NotFound(new { message = "Folio not found" });
        return dto;
    }

    [HttpGet("/api/stays/{stayId:int}/folio")]
    public async Task<ActionResult<FolioDto>> GetByStay(int stayId)
    {
        var dto = await _folios.GetByStayAsync(stayId);
        if (dto is null) return NotFound(new { message = "Folio not found for this stay" });
        return dto;
    }

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("/api/folios/{id:int}/charges")]
    public async Task<ActionResult<FolioDto>> AddCharge(int id, ChargeRequest req) =>
        ToAction(await _folios.AddChargeAsync(id, req, UserId()));

    [Authorize(Roles = "Manager,FrontDesk")]
    [HttpPost("/api/folios/{id:int}/payments")]
    public async Task<ActionResult<FolioDto>> AddPayment(int id, PaymentRequest req) =>
        ToAction(await _folios.AddPaymentAsync(id, req, UserId()));

    [Authorize(Roles = "Manager")]
    [HttpPost("/api/folios/{id:int}/adjustments")]
    public async Task<ActionResult<FolioDto>> AddAdjustment(int id, AdjustmentRequest req) =>
        ToAction(await _folios.AddAdjustmentAsync(id, req, UserId()));

    // ---- helpers ----

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult<FolioDto> ToAction(ServiceResult<FolioDto> result)
    {
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });
        return result.Value!;
    }
}