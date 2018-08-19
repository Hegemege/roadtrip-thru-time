using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawnScript : MonoBehaviour
{
    void Awake()
    {
        GameManager.Instance.SpawnCar(gameObject);

        Destroy(gameObject);
    }
}
