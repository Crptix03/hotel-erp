namespace HotelErp.Api.Models;

public class FolioLine
{
    public int Id { get; set; }
    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;
    public FolioLineType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}