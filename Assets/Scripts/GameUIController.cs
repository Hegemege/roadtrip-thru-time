using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIController : MonoBehaviour
{
    public RectTransform EnergyBar;


    void Update()
    {
        if (!GameManager.Instance.ActiveCar) return;

        var energyT = GameManager.Instance.ActiveCar.Energy / GameManager.Instance.SpawnEnergy;
        EnergyBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, energyT * 790);
    }
}
