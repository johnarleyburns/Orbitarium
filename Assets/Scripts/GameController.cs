using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Greyman;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{

    public GameObject PlayerShipPrefab;
    public GameObject EnemyShipPrefab;
    public GameObject FPSCanvas;
    public GameObject GameStartCanvas;
    public GameObject GamePauseCanvas;
    public GameObject GameOverCanvas;
    public Text GameOverText;
    public Camera FPSCamera;
    public Camera OverShoulderCamera;
    public Camera OverviewCamera;
    public GameObject Didymos;
    public GameObject Didymoon;
    public float PlayerInitialImpulse;
    public float EnemyDistanceMeters = 500;
    public float EnemyRandomSpreadMeters = 200;
    public float EnemyInitialCount = 3;
    public float EnemyInitialImpulse = 10;
    public float DestroyedEnemyTimeToLiveSec = 5;
    public Material StrobeMaterial;
    public string StrobeTag = "Strobe";

    private HUDController hudController;
    private GameObject player;
    private List<GameObject> enemyShips = new List<GameObject>();
    private List<GameObject> targets = new List<GameObject>();
    private float gameStartInputTimer = -1;
    private float gameOverInputTimer = -1;
    private Dictionary<GameObject, float> destroyedEnemiesTimeToLive = new Dictionary<GameObject, float>();
    private GameState gameState;
    public enum GameState
    {
        STARTING,
        START_NOT_ACCEPTING_INPUT,
        START_AWAIT_INPUT,
        PREPARING_RUN,
        RUNNING,
        PAUSED,
        START_GAME_OVER,
        GAME_OVER_NOT_ACCEPTING_INPUT,
        GAME_OVER_AWAIT_INPUT
    }

    void Start()
    {
        hudController = GetComponent<HUDController>();
        TransitionToStarting();
    }

    public HUDController GetHUD()
    {
        return hudController;
    }

    #region Transitions

    private void TransitionToStarting()
    {
        gameState = GameState.STARTING;
        gameStartInputTimer = 0.5f;
        ResetGravityEngine();
        hudController.HideTargetIndicator();
        InstantiatePlayer();
        EnableOverviewCamera();
        GameStartCanvas.SetActive(true);
        FPSCanvas.SetActive(false);
        GamePauseCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        SetupStrobes();
        gameState = GameState.START_NOT_ACCEPTING_INPUT;
    }

    private void SetupStrobes()
    {
        strobes = GameObject.FindGameObjectsWithTag(StrobeTag);
        if (strobes != null && strobes.Length > 0)
        {
            //lightData = new SpriteLights.LightData[strobes.Length];
            for (int i = 0; i < strobes.Length; i++)
            {
                lightData = new SpriteLights.LightData[1];
                lightData[0] = new SpriteLights.LightData();
                lightData[0].position = strobes[i].transform.position;
                lightData[0].strobeID = i;
                lightData[0].strobeGroupID = Random.Range(0, 1);
                lightData[0].brightness = 1;
                SpriteLights.CreateLights("Strobes", lightData, StrobeMaterial, strobes[i]);
            }
        }
        strobePositionTimer = 0;
        //        UpdateStrobes();
        float SecBetweenFlash = 1;
        float strobeTimeStep = lightData.Length == 0 ? 20 : SecBetweenFlash / lightData.Length;
        float globalBrightnessOffset = 0;
        float fov = Camera.main.fieldOfView;
        float screenHeight = Screen.height;
        SpriteLights.Init(strobeTimeStep, globalBrightnessOffset, fov, screenHeight);
    }

    private GameObject[] strobes = new GameObject[0];
    private float strobePositionTimer = 0;
    private float secondsBetweenStrobeUpdate = 1;
    private SpriteLights.LightData[] lightData = new SpriteLights.LightData[0];

    private void UpdateStrobes()
    {
        /*
        if (strobePositionTimer > 0)
        {
            strobePositionTimer -= Time.deltaTime;
        }
        else
        {
            if (strobes != null && strobes.Length > 0)
            {
                for (int i = 0; i < strobes.Length; i++)
                {
                    lightData[i].position = strobes[i].transform.position;
                }
            }
            float SecBetweenFlash = 1;
            float strobeTimeStep = lightData.Length == 0 ? 20 : SecBetweenFlash / lightData.Length;
            float globalBrightnessOffset = 0;
            float fov = Camera.main.fieldOfView;
            float screenHeight = Screen.height;
            SpriteLights.Init(strobeTimeStep, globalBrightnessOffset, fov, screenHeight);
            strobePositionTimer = secondsBetweenStrobeUpdate;
        }
        */
    }

    private void TransitionToRunning()
    {
        AddFixedTargets();
        InstantiateEnemies();
        SetupInitialVelocities();
        FPSCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        GamePauseCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        FPSCamera.enabled = true;
        OverviewCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        gameState = GameState.RUNNING;
    }

    private void TransitionToPaused()
    {
        Time.timeScale = 0;
        FPSCanvas.SetActive(false);
        GameStartCanvas.SetActive(false);
        GamePauseCanvas.SetActive(true);
        GameOverCanvas.SetActive(false);
        FPSCamera.enabled = true;
        OverviewCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        gameState = GameState.PAUSED;
    }

    private void TransitionToRunningFromPaused()
    {
        FPSCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        GamePauseCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        FPSCamera.enabled = true;
        OverviewCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        Time.timeScale = 1.0f;
        gameState = GameState.RUNNING;
    }

    private void TransitionToStartAwaitInput()
    {
        gameState = GameState.START_AWAIT_INPUT;
    }

    private void TransitionToGameOverFromPaused()
    {
        gameState = GameState.START_GAME_OVER;
        hudController.RemoveIndicators();
        Time.timeScale = 0.5f;
        gameOverInputTimer = 1f;
        GameOverText.text = "Try again? (Y/N)";
        EnableOverviewCamera();
        GameOverCanvas.SetActive(true);
        GamePauseCanvas.SetActive(false);
        GameStartCanvas.SetActive(false);
        FPSCanvas.SetActive(false);
        gameState = GameState.GAME_OVER_NOT_ACCEPTING_INPUT;
    }

    public void TransitionToGameOverFromDeath(string reason)
    {
        gameState = GameState.START_GAME_OVER;
        hudController.RemoveIndicators();
        Time.timeScale = 0.5f;
        gameOverInputTimer = 1f;
        GameOverText.text = reason + "\nTry again? (Y/N)";
        EnableOverviewCamera();
        GameOverCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        GamePauseCanvas.SetActive(false);
        FPSCanvas.SetActive(false);
        gameState = GameState.GAME_OVER_NOT_ACCEPTING_INPUT;
    }

    private void TransitionToGameOverAwaitInput()
    {
        gameState = GameState.GAME_OVER_AWAIT_INPUT;
    }

    private void TransitionToStartingFromGameOver()
    {
        CleanupScene();
        SceneManager.UnloadScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void TransitionToQuit()
    {
        Application.CancelQuit();
    }

    #endregion

    #region Updates

    void Update()
    {
        switch (gameState)
        {
            case GameState.START_NOT_ACCEPTING_INPUT:
                UpdateWaitForSplash();
                break;
            case GameState.START_AWAIT_INPUT:
                UpdateCheckForGameStart();
                break;
            case GameState.RUNNING:
                UpdateShip();
                UpdateStrobes();
                UpdateCheckForDestroyedEnemies();
                UpdateCheckForGamePause();
                break;
            case GameState.PAUSED:
                UpdateCheckForGameUnpause();
                break;
            case GameState.GAME_OVER_NOT_ACCEPTING_INPUT:
                UpdateWaitForGameOver();
                break;
            case GameState.GAME_OVER_AWAIT_INPUT:
                UpdateCheckForGameFinished();
                break;
        }
    }

    private void UpdateWaitForSplash()
    {
        if (gameStartInputTimer > 0)
        {
            gameStartInputTimer -= Time.deltaTime;
        }
        else
        {
            TransitionToStartAwaitInput();
        }
    }

    private void UpdateWaitForGameOver()
    {
        if (gameOverInputTimer > 0)
        {
            gameOverInputTimer -= 2 * Time.deltaTime;
        }
        else
        {
            TransitionToGameOverAwaitInput();
        }
    }

    private void UpdateCheckForGameStart()
    {
        if (Input.anyKey)
        {
            TransitionToRunning();
        }
    }

    private void UpdateCheckForDestroyedEnemies()
    {
        HashSet<GameObject> pruneEnemies = new HashSet<GameObject>();
        List<GameObject> keys = new List<GameObject>(destroyedEnemiesTimeToLive.Keys);
        foreach (GameObject enemyShip in keys)
        {
            float timeToLive;
            if (destroyedEnemiesTimeToLive.TryGetValue(enemyShip, out timeToLive))
            {
                timeToLive -= Time.deltaTime;
                if (timeToLive <= 0)
                {
                    pruneEnemies.Add(enemyShip);
                }
                else
                {
                    destroyedEnemiesTimeToLive[enemyShip] = timeToLive;
                }
            }
        }
        foreach (GameObject g in pruneEnemies)
        {
            destroyedEnemiesTimeToLive.Remove(g);
        }
        foreach (GameObject g in pruneEnemies)
        {
            DestroyEnemy(g);
        }
    }

    private void UpdateCheckForGameUnpause()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            TransitionToGameOverFromPaused();
        }
        else if (Input.anyKeyDown)
        {
            TransitionToRunningFromPaused();
        }
    }

    private void UpdateCheckForGameFinished()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            TransitionToStartingFromGameOver();
        }
        else if (Input.anyKeyDown)
        {
            TransitionToQuit();
        }
    }

    private void UpdateShip()
    {
        GetPlayerShip().UpdateShip();
    }

    private void UpdateCheckForGamePause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TransitionToPaused();
        }
    }

    #endregion

    private void ResetGravityEngine()
    {
        GravityEngine.instance.Clear();
        GravityEngine.instance.Setup();
        GravityEngine.instance.SetEvolve(true);
    }


    public GameState GetGameState()
    {
        return gameState;
    }

    public bool IsRunning()
    {
        return gameState == GameState.RUNNING;
    }

    private void InstantiatePlayer()
    {
        DestroyPlayer();
        player = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;
        PlayerShipController controller = player.GetComponent<PlayerShipController>();
        controller.SetGameController(this);
        GravityEngine.instance.AddBody(player);
        GameObject playerModel = controller.GetShipModel();
        SetupCameras(playerModel);
        playerModel.GetComponent<PlayerShip>().StartShip();
    }

    private void SetupInitialVelocities()
    {
        float playerImpulse = Random.Range(0, PlayerInitialImpulse);
        GravityEngine.instance.ApplyImpulse(player.GetComponent<NBody>(), playerImpulse * player.transform.forward);
        foreach (GameObject enemyShip in enemyShips)
        {
            float enemyImpulse = Random.Range(0, EnemyInitialImpulse);
            GravityEngine.instance.ApplyImpulse(enemyShip.GetComponent<NBody>(), enemyImpulse * -player.transform.forward);
        }
    }

    private void InstantiateEnemies()
    {
        for (int i = 0; i < EnemyInitialCount; i++)
        {
            InstantiateEnemy(string.Format("{0:00}", i.ToString()));
        }
    }

    private void InstantiateEnemy(string suffix)
    {
        Vector3 enemyOffset = EnemyRandomSpreadMeters * Random.insideUnitSphere;
        Vector3 enemySpawnPoint = new Vector3(0, 0, EnemyDistanceMeters) + enemyOffset;
        Quaternion enemySpawnRotation = Quaternion.Euler(0, 180, 0);
        GameObject enemyShip = Instantiate(EnemyShipPrefab, enemySpawnPoint, enemySpawnRotation) as GameObject;
        GravityEngine.instance.AddBody(enemyShip);
        EnemyShipController controller = enemyShip.GetComponent<EnemyShipController>();
        controller.SetGameController(this);
        string nameRoot = controller.GetShipModel().GetComponent<EnemyShip>().VisibleName;
        enemyShip.name = string.Format("{0}-{1}", nameRoot, suffix);
        hudController.AddEnemyIndicator(enemyShip);
        enemyShips.Add(enemyShip);
        AddTarget(enemyShip);
        hudController.SelectNextTargetPreferClosestEnemy();
    }

    private void EnableOverviewCamera()
    {
        GameObject playerModel = player.GetComponent<PlayerShipController>().GetShipModel();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

    private void AddTarget(GameObject target)
    {
        targets.Add(target);
        GetComponent<InputController>().PropertyChanged("TargetList", targets);
        hudController.SelectNextReferenceBody();
    }

    private void RemoveTarget(GameObject target)
    {
        targets.Remove(target);
        hudController.RemoveIndicator(target.transform);
        GetComponent<InputController>().PropertyChanged("TargetList", targets);
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public PlayerShip GetPlayerShip()
    {
        return player.GetComponent<PlayerShipController>().GetShipModel().GetComponent<PlayerShip>();
    }

    public int EnemyCount()
    {
        return enemyShips.Count;
    }

    public GameObject GetEnemy(int i)
    {
        if (i >= 0 && i < enemyShips.Count)
        {
            return enemyShips[i];
        }
        else
        {
            return null;
        }
    }

    public int TargetCount()
    {
        return targets.Count;
    }

    public GameObject GetTarget(int i)
    {
        if (i >= 0 && i < targets.Count)
        {
            return targets[i];
        }
        else
        {
            return null;
        }
    }

    public int ClosestEnemy()
    {
        float dist = 0;
        int index = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject t = targets[i];
            if (enemyShips.Contains(t))
            {
                float d;
                PhysicsUtils.CalcDistance(player.transform, t, out d);
                if (d < dist || index == -1)
                {
                    dist = d;
                    index = i;
                }
            }
        }
        return index;
    }

    public int ClosestTarget()
    {
        float dist = 0;
        int index = -1;
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject t = targets[i];
            float d;
            PhysicsUtils.CalcDistance(player.transform, t, out d);
            if (d < dist || index == -1)
            {
                dist = d;
                index = i;
            }
        }
        return index;
    }

    private void AddFixedTargets()
    {
        hudController.AddPlanetaryObjectIndicators(Didymos, Didymoon);
        AddTarget(Didymos);
        AddTarget(Didymoon);
        hudController.AddFixedIndicators();
    }

    private void CleanupScene()
    {
        ClearTargets();
        DestroyPlayer();
        DestroyEnemies();
    }

    private void ClearTargets()
    {
        hudController.ClearTargetIndicators();
        targets.Clear();
        GetComponent<InputController>().PropertyChanged("TargetList", targets);
    }

    private void DestroyEnemies()
    {
        List<GameObject> allEnemies = new List<GameObject>(enemyShips);
        foreach (GameObject enemyShip in allEnemies)
        {
            DestroyEnemy(enemyShip);
        }
        enemyShips.Clear();
        destroyedEnemiesTimeToLive.Clear();
    }

    public void DestroyEnemyShipByCollision(GameObject enemyShip)
    {
        if (!destroyedEnemiesTimeToLive.ContainsKey(enemyShip))
        {
            destroyedEnemiesTimeToLive.Add(enemyShip, DestroyedEnemyTimeToLiveSec);
        }
    }

    private void DestroyEnemy(GameObject enemyShip)
    {
        if (enemyShip != null)
        {
            if (enemyShip == GetHUD().GetReferenceBody())
            {
                GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.OFF);
            }
            RemoveTarget(enemyShip);
            enemyShips.Remove(enemyShip);
            hudController.SelectNextTargetPreferClosestEnemy();
            //GravityEngine.instance.RemoveBody(enemyShip);     
            //Destroy(enemyShip);
            GravityEngine.instance.InactivateBody(enemyShip);
            enemyShip.SetActive(false);
        }
    }

    private void DestroyPlayer()
    {
        if (player != null)
        {
            /*
            GameObject playerModel = playerShip.GetComponent<PlayerShipController>().GetShipModel();
            if (playerModel != null && playerModel.activeInHierarchy)
            {
                playerModel.GetComponent<PlayerShipController>().GetComponent<PlayerShip>().PrepareDestroy();
            }
            */
            GravityEngine.instance.RemoveBody(player);
            Destroy(player);
            player = null;
        }
    }

    private void SetupCameras(GameObject playerModel)
    {
        FPSCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        OverShoulderCamera.GetComponent<ThirdPartyCameraController>().UpdatePlayer(playerModel);
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

}
