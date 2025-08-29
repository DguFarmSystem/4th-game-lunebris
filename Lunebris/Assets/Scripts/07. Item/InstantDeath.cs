using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantDeath : Item
{
    private void Awake()
    {
        itemID = 5;
        itemName = "InstantDeath";
        applyCondition = true;
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
                CancelInvoke("DestroyObject");
                gameObject.SetActive(false);
            }
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
