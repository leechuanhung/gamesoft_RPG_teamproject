
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxSlots = 20;

    private readonly List<InventorySlot> slots =
        new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int MaxSlots => Mathf.Max(1, maxSlots);

    public event Action OnInventoryChanged;

    public bool TryAdd(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int maxStack = item.MaxStack;
        long capacity = 0;

        // 같은 아이템이 들어 있는 슬롯의 여유 공간
        foreach (var slot in slots)
        {
            if (slot.Item == item)
                capacity += Math.Max(
                    0, maxStack - slot.Quantity
                );
        }

        // 빈 슬롯에 보관할 수 있는 수량
        capacity += (long)Math.Max(
            0, MaxSlots - slots.Count
        ) * maxStack;

        // 전부 들어가지 않으면 아무것도 추가하지 않음
        if (capacity < amount)
            return false;

        int remaining = amount;

        // 기존 슬롯부터 채우기
        foreach (var slot in slots)
        {
            if (slot.Item != item) continue;

            int space = Math.Max(
                0, maxStack - slot.Quantity
            );

            int add = Math.Min(space, remaining);

            slot.Add(add);
            remaining -= add;

            if (remaining == 0)
                break;
        }

        // 남은 수량을 새 슬롯에 저장
        while (remaining > 0)
        {
            int add = Math.Min(maxStack, remaining);

            slots.Add(new InventorySlot(item, add));
            remaining -= add;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetQuantity(item) < amount)
            return false;

        int remaining = amount;

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].Item != item)
                continue;

            int remove = Math.Min(
                slots[i].Quantity, remaining
            );

            slots[i].Remove(remove);
            remaining -= remove;

            if (slots[i].Quantity == 0)
                slots.RemoveAt(i);

            if (remaining == 0)
                break;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetQuantity(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (var slot in slots)
        {
            if (slot.Item == item)
                total += slot.Quantity;
        }

        return total;
    }

    public void Clear()
    {
        slots.Clear();
        OnInventoryChanged?.Invoke();
    }
}
