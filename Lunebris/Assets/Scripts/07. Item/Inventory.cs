using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Singleton
    public static Inventory instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory found!");
            return;
        }
        instance = this;
    }
    #endregion

    KeyCode[] itemKeys = { KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V };

    void Update()
    {
        for (int i = 0; i < itemKeys.Length; i++)
        {
            if (Input.GetKeyDown(itemKeys[i]))
            {
                UseItemAtIndex(i);
            }
        }
    }

    public void UseItemAtIndex(int index)
    {
        if (index < items.Count)
        {
            Item item = items[index];
            if (!item.applyCondition)
                UseItem(item);
            else
                Debug.Log("사용할 수 없는 아이템 입니다.");
        }
        else
        {
            Debug.Log("해당 슬롯에 아이템이 없습니다.");
        }
    }


    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public int space = 8;  // 인벤토리 아이템 슬롯의 수
    public List<Item> items = new List<Item>();  // 인벤토리에 현재 있는 아이템 리스트

    public bool Add(Item item)
    {
        if (items.Count >= space)
        {
            Debug.Log("Not enough room.");
            return false;
        }
        items.Add(item);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        return true;
    }

    public void Remove(Item item)
    {
        items.Remove(item);
        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }
    
    public void UseItem(Item item)
    {
        if (items.Contains(item))
        {
            item.ApplyEffect();      // 효과 적용
            Remove(item);            // 인벤토리에서 제거
            Debug.Log(item.itemName + " 아이템을 사용했습니다.");
        }
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(item => item.itemName == itemName);
    }

    public Item GetItem(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }
}
