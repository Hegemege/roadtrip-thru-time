using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintUIController : MonoBehaviour
{
    private bool _spawned;

    public GameObject Frame;

    void Update()
    {
        if (GameManager.Instance.CollectiblesLeft == 0 && !_spawned)
        {
            _spawned = true;
            GameManager.Instance.AllowTimeRewind = true;
            Frame.SetActive(true);
            Destroy(gameObject, 5f);
        }

        if (_spawned && Input.GetKeyDown(KeyCode.Space))
        {
            Destroy(gameObject, 0.5f);
        }
    }
}
