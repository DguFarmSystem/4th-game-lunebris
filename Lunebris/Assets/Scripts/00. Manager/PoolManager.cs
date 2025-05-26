//System
using System.Collections.Generic;

// Engine
using UnityEngine;

[DisallowMultipleComponent]
public class PoolManager : MonoBehaviour
{
    //Prfabs GameObject
    public GameObject[] prefabs;

    [SerializeField] List<GameObject>[] pools;

    private void Awake()
    {
        //Initialize
        pools = new List<GameObject>[prefabs.Length];

        for (int i = 0; i < pools.Length; i++)
        {
            pools[i] = new List<GameObject>();
        }
    }

    //Get Moethod
    public GameObject Get(int index)
    {
        //For Retrun
        GameObject select = null;

        foreach (GameObject item in pools[index])
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

        //If not find
        if (!select)
        {
            //instantiate
            select = Instantiate(prefabs[index], transform);

            //add pool
            pools[index].Add(select);
        }

        //return object
        return select;
    }
}