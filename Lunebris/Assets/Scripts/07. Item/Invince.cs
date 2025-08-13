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
            Player.Player player = other.GetComponent<Player.Player>();
            if (player != null)
            {
                player.GiveInvincibilityItem();
            }

            CancelInvoke(nameof(DestroyObject));
            gameObject.SetActive(false);
        }
    }

    public override void ApplyEffect()//효과 적용
    {

    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}
