namespace BPInventoryOps.Api.Entities;

public class RestockItem
{
    public int Id { get; set; }

    public int RestockEventId { get; set; }

    public int ProductId { get; set; }

    public int QuantityReceived { get; set; }

    public RestockEvent RestockEvent { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
