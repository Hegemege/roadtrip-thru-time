using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitZoneController : MonoBehaviour
{
    public GameObject Bubble;
    public string NextScene;

    void Update()
    {
        var showSphere = GameManager.Instance.CollectiblesLeft > 0;
        Bubble.SetActive(showSphere);
    }
}
