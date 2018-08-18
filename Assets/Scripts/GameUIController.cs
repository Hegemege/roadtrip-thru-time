using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIController : MonoBehaviour
{
    public RectTransform EnergyBar;

    public GameObject RecordOverlay;
    public GameObject RewindOverlay;
    public GameObject PlayOverlay;
    public GameObject StopOverlay;

    public float FlashingSpeed;
    private float _flashingTime;

    private UIOverlayState _state;
    private bool _visible;

    void Awake()
    {
        RecordOverlay.SetActive(true);
        RewindOverlay.SetActive(false);
        PlayOverlay.SetActive(false);
        StopOverlay.SetActive(false);

        _state = UIOverlayState.Record;
        _visible = true;
    }

    void Update()
    {
        if (!GameManager.Instance.ActiveCar) return;

        var dt = Time.deltaTime;

        _flashingTime += dt;
        if (_flashingTime > FlashingSpeed && _state == UIOverlayState.Record || _state == UIOverlayState.Play)
        {
            SetUIState(_state, !_visible);
        }

        var energyT = GameManager.Instance.ActiveCar.Energy / GameManager.Instance.SpawnEnergy;
        EnergyBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, energyT * 790);
    }

    public void SetUIState(UIOverlayState state, bool activeValue = true)
    {
        _state = state;
        _visible = activeValue;
        _flashingTime = 0f;

        RecordOverlay.SetActive(false);
        RewindOverlay.SetActive(false);
        PlayOverlay.SetActive(false);
        StopOverlay.SetActive(false);

        switch (state)
        {
            case UIOverlayState.Record:
                RecordOverlay.SetActive(_visible);
                break;
            case UIOverlayState.Rewind:
                RewindOverlay.SetActive(_visible);
                break;
            case UIOverlayState.Play:
                PlayOverlay.SetActive(_visible);
                break;
            case UIOverlayState.Stop:
                StopOverlay.SetActive(_visible);
                break;
        }
    }
}
