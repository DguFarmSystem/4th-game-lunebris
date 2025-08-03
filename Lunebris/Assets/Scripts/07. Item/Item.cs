using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;


public abstract class Item : MonoBehaviour
{
    public int itemID;
    public Sprite icon;
    public string itemName;

    public abstract void DestroyAfterTime(); //시간 지난 후 파괴
    public abstract void ApplyEffect(); //아이템 실행

    private void Start()
    {
        DestroyAfterTime();
    }
}
