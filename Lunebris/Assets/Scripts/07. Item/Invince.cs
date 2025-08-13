using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Invince : Item
{   
    private void Awake()
    {
        itemID = 2;
        itemName = "Invince";
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
                // 아이템을 플레이어에게 전달 (보유 상태로 변경)
                Player.Player player = other.GetComponent<Player.Player>();
                if (player != null)
                {
                    player.GiveInvincibilityItem(); // 보유 상태 부여
                }

                CancelInvoke(nameof(DestroyObject));
                gameObject.SetActive(false);
            }
        }
        /*
        if (other.CompareTag("Player"))
        {
            bool wasPickedUp = Inventory.instance.Add(this);
            if (wasPickedUp)
            {
                CancelInvoke("DestroyObject");
                gameObject.SetActive(false);
            }
        }
        */
    }

    public override void ApplyEffect()
    {
        /*
        Player.Player player = FindObjectOfType<Player.Player>();
        if (player != null)
        {
            player.ActivateTimedInvincibility();
        }
        */
    }


    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
