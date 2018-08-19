using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUIController : MonoBehaviour
{
    private bool _triggered;

    void Update()
    {
        if (_triggered) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _triggered = true;
            GameManager.Instance.LevelEndInput = 1;
            GameManager.Instance.EndLevel("Level_01");
        }
    }
}
