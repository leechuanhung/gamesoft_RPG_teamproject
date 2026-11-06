
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Inventory/Item"
)]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int maxStack = 99;

    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MaxStack => Mathf.Max(1, maxStack);

    // Items made in code (JinaItems) rather than as assets fill these in here.
    protected void Setup(string name, string text, Sprite image, int stack)
    {
        itemName = name; description = text; icon = image; maxStack = stack;
    }
}
