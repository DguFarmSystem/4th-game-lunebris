using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class Reborn : Item
{
    private void Awake()
    {
        itemID = 3;
        itemName = "Reborn";
    }

    public override void DestroyAfterTime()//시간 지난 후 파괴
    {
        Invoke("DestroyObject", 30.0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool wasPickedUp = Inventory.instance.Add(this);
            if (wasPickedUp)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public override void ApplyEffect()//효과 적용
    {
        Player.Player player = GameObject.FindWithTag("Player").GetComponent<Player.Player>();
            
            Debug.Log("부활함");
            Destroy(gameObject); // 아이템 오브젝트 제거
         
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
