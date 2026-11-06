
public class InventorySlot
{
    public ItemData Item { get; }
    public int Quantity { get; private set; }

    public InventorySlot(ItemData item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    internal void Add(int amount)
    {
        Quantity += amount;
    }

    internal void Remove(int amount)
    {
        Quantity -= amount;
    }
}
