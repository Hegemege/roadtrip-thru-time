using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public RectTransform EnergyBar;

    public GameObject RecordOverlay;
    public GameObject RewindOverlay;
    public GameObject PlayOverlay;
    public GameObject StopOverlay;

    public GameObject InfoFrameOverlay;
    public GameObject FailFuelText;
    public GameObject FailTimelineText;
    public GameObject SuccessText;

    public Text RewindsText;

    public float FlashingSpeed;
    private float _flashingTime;

    private UIOverlayState _state;
    private bool _visible;
    private int _previousRewindsCount;

    private float _levelEndTimer;

    void Awake()
    {
        RecordOverlay.SetActive(true);
        RewindOverlay.SetActive(false);
        PlayOverlay.SetActive(false);
        StopOverlay.SetActive(false);

        InfoFrameOverlay.SetActive(false);
        FailFuelText.SetActive(false);
        FailTimelineText.SetActive(false);
        SuccessText.SetActive(false);

        _state = UIOverlayState.Record;
        _visible = true;
    }

    void Update()
    {
        if (GameManager.Instance.LevelEnded && !GameManager.Instance.ExitingLevel)
        {
            _levelEndTimer += Time.deltaTime;
            if (_levelEndTimer > 1f)
            {
                InfoFrameOverlay.SetActive(true);
                if (GameManager.Instance.LevelEndState == LevelEndState.Success) SuccessText.SetActive(true);
                if (GameManager.Instance.LevelEndState == LevelEndState.FailFuel) FailFuelText.SetActive(true);
                if (GameManager.Instance.LevelEndState == LevelEndState.FailTimeline) FailTimelineText.SetActive(true);

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    GameManager.Instance.LevelEndInput = 1;
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    GameManager.Instance.LevelEndInput = 2;
                }
            }
            SetUIState(UIOverlayState.Stop);
            return;
        }
        else
        {
            InfoFrameOverlay.SetActive(false);
            FailFuelText.SetActive(false);
            FailTimelineText.SetActive(false);
            SuccessText.SetActive(false);
        }

        if (!GameManager.Instance.ActiveCar) return;

        var dt = Time.deltaTime;

        _flashingTime += dt;
        if (_flashingTime > FlashingSpeed && _state == UIOverlayState.Record || _state == UIOverlayState.Play)
        {
            SetUIState(_state, !_visible);
        }

        var energyT = GameManager.Instance.ActiveCar.Energy / GameManager.Instance.SpawnEnergy;
        EnergyBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, energyT * 790);

        if (GameManager.Instance.RewindCount != _previousRewindsCount)
        {
            _previousRewindsCount = GameManager.Instance.RewindCount;
            RewindsText.text = _previousRewindsCount.ToString();
        }
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
