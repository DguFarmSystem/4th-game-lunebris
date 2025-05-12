//System
using System.Collections.Generic;

// Engine
using UnityEngine;

[DisallowMultipleComponent]
public class PoolManager : MonoBehaviour
{
    [Header("풀링에 넣을 프리팹")]
    [SerializeField] private GameObject prefab;

    List<GameObject> pools;

    private void Awake()
    {
        pools = new List<GameObject>();
    }

    //Get Moethod
    public GameObject Get()
    {
        //For Retrun
        GameObject select = null;

        foreach (GameObject item in pools)
        {
            //Find Object in pool
            if (!item.activeSelf)
            {
                //select item
                select = item;

                //setactive true
                select.SetActive(true);

                //break;
                break;
            }
        }

        //If not find -> Instaniate
        if (!select)
        {
            //instantiate
            select = Instantiate(prefab, transform);

            //add pool
            pools.Add(select);
        }

        //return object
        return select;
    }

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }
}