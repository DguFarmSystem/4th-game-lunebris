using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotParent; // GridLayoutGroup 들어있는 부모
    private Slot[] slots;

    void Start()
    {
        slots = slotParent.GetComponentsInChildren<Slot>();
        Inventory.instance.onItemChangedCallback += UpdateUI;
        UpdateUI();
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < Inventory.instance.items.Count && Inventory.instance.items[i] != null)
            // i번째 슬롯에 대응되는 i번째 아이템이 실제로 존재하면(인덱스 유효 + null 아님)
            {
                slots[i].SetItem(Inventory.instance.items[i]);
                // 해당 슬롯에 그 아이템의 아이콘을 세팅하고 보이게 함.
            }
            else
            {
                slots[i].ClearSlot();
                // 해당 위치에 아이템이 없으면 아이콘을 지우고 슬롯을 빈 상태로 표시.
            }
        }
    }
}
