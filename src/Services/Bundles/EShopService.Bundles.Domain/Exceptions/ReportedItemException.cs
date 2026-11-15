namespace EShopService.Bundles.Domain.Exceptions;

public sealed class ReportedItemException : DomainException
{
    public Guid ItemId { get; }

    public ReportedItemException(Guid itemId)
        : base("bundle.item_reported", $"The artwork '{itemId}' has been reported and removed from the bundle.")
    {
        ItemId = itemId;
    }
}