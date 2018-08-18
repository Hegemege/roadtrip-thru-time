using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //Singleton
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }

    // Globals
    [HideInInspector]
    public TimeManager TimeManager;

    [HideInInspector]
    public CarController ActiveCar;

    public bool RecordSnapshots;
    public bool Rewinding;

    public bool AllowTimeRewind;
    public float SpawnEnergy;
    public float AccelerationEnergyConsumption;

    // Self references
    public Camera Camera;
    public CarPool CarPool;

    // Other
    public GameObject GameUI;

    void Awake()
    {
        // Setup singleton
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _instance = this;

        // Self references
        TimeManager = GetComponent<TimeManager>();

        // Instantiate game ui
        var ui = Instantiate(GameUI);
        ui.transform.parent = transform;

        // Start the level
        // TODO: Check if not in menu or other funny business
        StartLevel();
    }

    void Update()
    {
        var carExists = ActiveCar != null;
        // Rewinding input
        if (Input.GetKeyDown(KeyCode.Space) && carExists && AllowTimeRewind)
        {
            Rewinding = true;
            ActiveCar.PlayerControlled = false;
        }

        if (Input.GetKeyUp(KeyCode.Space) && carExists && AllowTimeRewind)
        {
            Rewinding = false;
            TimeManager.CutTimeline(ActiveCar);

            var oldCar = ActiveCar;

            var newCar = CarPool.GetPooledObject();
            newCar.gameObject.SetActive(true);
            SetNewCar(newCar, ActiveCar.transform);
            newCar.component.Energy = ActiveCar.Energy;

            newCar.component.ApplySnapshot(oldCar.GetTimeSnapshot());
        }

        // Update time states - record only if car exists and not rewinding time
        RecordSnapshots = carExists && !Rewinding && AllowTimeRewind;
    }

    public void StartLevel()
    {
        // Spawn a new car at car spawn
        var carSpawn = GameObject.Find("CarSpawn");

        var newCar = CarPool.GetPooledObject();
        newCar.gameObject.SetActive(true);

        SetNewCar(newCar, carSpawn.transform);
        newCar.component.Energy = SpawnEnergy;

        // Destroy spawn, no use for it anymore
        GameObject.Destroy(carSpawn);
    }

    public void SetNewCar(Component<CarController> newCar, Transform spawnTransform)
    {
        newCar.gameObject.transform.position = spawnTransform.position;
        newCar.gameObject.transform.rotation = spawnTransform.rotation;

        newCar.component.Reset();

        newCar.component.PlayerControlled = true;
        ActiveCar = newCar.component;

        // Set camera to follow the car
        Camera.transform.parent = newCar.gameObject.transform;
    }

    public void EndLevel()
    {
        // Reuse the car 
        ActiveCar.gameObject.SetActive(false);
    }
}