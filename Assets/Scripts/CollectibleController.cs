using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleController : MonoBehaviour
{
    void Awake()
    {
        GameManager.Instance.CollectiblesLeft += 1;
    }
}
