using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Crosstales.RTVoice;

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
    public GameObject EezoApproach;
    public GameObject EezoApproachGhost;
    public GameObject EezoDock;
    public GameObject EezoDockGhost;
    public Transform SpawnPoint;
    public float PlayerInitialImpulse;
    public float PlayerShipRadiusM = 20;
    public float EnemyShipRadiusM = 20;
    public float DidymosRadiusM = 500;
    public float DidymoonRadiusM = 100;
    public float EezoDockingPortRadiusM = 20;
    public float EnemyDistanceMeters = 500;
    public float EnemyRandomSpreadMeters = 200;
    public float EnemyInitialCount = 3;
    public float EnemyInitialImpulse = 10;
    public Material StrobeMaterial;
    public string StrobeTag = "Strobe";
    public AudioSource AS;

    private HUDController hudController;
    private InputController inputController;
    private TargetDB targetDB;
    private EnemyTracker enemyTracker;
    private GameObject player;
    private Dictionary<GameObject, GameObject> followGhost = new Dictionary<GameObject, GameObject>();
    private float gameStartInputTimer = -1;
    private float gameOverInputTimer = -1;
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
        inputController = GetComponent<InputController>();
        targetDB = GetComponent<TargetDB>();
        enemyTracker = GetComponent<EnemyTracker>();
        targetDB.gameController = this;
        enemyTracker.gameController = this;
        StartFollowGhost();
        TransitionToStarting();
    }

    private void StartFollowGhost()
    {
        followGhost = new Dictionary<GameObject, GameObject>()
        {
            {
                EezoDockGhost,
                EezoDock
            },
            {
                EezoApproachGhost,
                EezoApproach
            }
        };
        UpdateFollows();
    }

    private void UpdateFollows()
    {
        foreach (GameObject ghost in followGhost.Keys)
        {
            GameObject shadow = followGhost[ghost];
            shadow.transform.position = ghost.transform.position;
            shadow.transform.rotation = ghost.transform.rotation;
            shadow.transform.GetChild(0).localScale = ghost.transform.GetChild(0).localScale;
            shadow.transform.GetChild(0).position = ghost.transform.GetChild(0).position;
            shadow.transform.GetChild(0).rotation = ghost.transform.GetChild(0).rotation;
        }
    }

    public HUDController HUD()
    {
        return hudController;
    }

    public InputController InputControl()
    {
        return inputController;
    }

    public TargetDB TargetData()
    {
        return targetDB;
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
        Speak(DialogText.ReadyPlayerOne);
        gameState = GameState.START_NOT_ACCEPTING_INPUT;
    }

    private void Speak(string text)
    {
        Speaker.Speak(text, AS, Speaker.VoiceForCulture("en", 1), false, 1, 0.4f, null, 3f);
    }

    private void SetupStrobes()
    {
        strobes = GameObject.FindGameObjectsWithTag(StrobeTag);
        if (strobes != null && strobes.Length > 0)
        {
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

    private void TransitionToRunning()
    {
        AddPlanetaryBodies();
        AddHUDFixedIndicators();
        InstantiateEnemies();
        SetupInitialVelocities();
        hudController.SelectNextTargetPreferClosestEnemy();
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
                UpdateFollows();
                UpdateCheckForGameStart();
                break;
            case GameState.RUNNING:
                UpdateShip();
                UpdateFollows();
                UpdateCheckForGamePause();
                break;
            case GameState.PAUSED:
                UpdateCheckForGameUnpause();
                break;
            case GameState.GAME_OVER_NOT_ACCEPTING_INPUT:
                UpdateFollows();
                UpdateWaitForGameOver();
                break;
            case GameState.GAME_OVER_AWAIT_INPUT:
                UpdateFollows();
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
        PlayerShipController controller = player.GetComponent<PlayerShipController>();
        controller.SetGameController(this);
        GravityEngine.instance.AddBody(player);
        GameObject playerModel = controller.GetShipModel();
        SetupCameras(playerModel);
        playerModel.GetComponent<PlayerShip>().StartShip();
        player.transform.position = SpawnPoint.position;
        playerModel.transform.rotation = SpawnPoint.rotation;
    }

    private void SetupInitialVelocities()
    {
        float playerImpulse = Random.Range(0, PlayerInitialImpulse);
        GravityEngine.instance.ApplyImpulse(player.GetComponent<NBody>(), playerImpulse * player.transform.forward);
        foreach (GameObject enemyShip in targetDB.GetTargets(TargetDB.TargetType.ENEMY))
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
        targetDB.AddTarget(enemyShip, TargetDB.TargetType.ENEMY, EnemyShipRadiusM);
        hudController.AddTargetIndicator(enemyShip);
    }

    private void EnableOverviewCamera()
    {
        GameObject playerModel = player.GetComponent<PlayerShipController>().GetShipModel();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public PlayerShip GetPlayerShip()
    {
        return player.GetComponent<PlayerShipController>().GetShipModel().GetComponent<PlayerShip>();
    }

    public GameObject ClosestTarget()
    {
        float dist = 0;
        GameObject target;
        foreach (GameObject t in targetDB.GetAllTargets())
        {
            float d;
            PhysicsUtils.CalcDistance(player.transform, t, out d);
            if (d < dist || dist == 0)
            {
                dist = d;
                target = t;
            }
        }
        return target = null;
    }

    public GameObject NextClosestTarget(GameObject g, TargetDB.TargetType? targetType = null)
    {
        GameObject target = null;
        List<GameObject> targets;
        if (targetType != null)
        {
            targets = new List<GameObject>(targetDB.GetTargets(targetType.Value));
        }
        else
        {
            targets = new List<GameObject>(targetDB.GetAllTargets());
        }
        Dictionary<GameObject, float> targetDist = new Dictionary<GameObject, float>();
        foreach (GameObject t in targets)
        {
            float d;
            PhysicsUtils.CalcDistance(player.transform, t, out d);
            targetDist.Add(t, d);
        }
        targets.Sort((x, y) => targetDist[x].CompareTo(targetDist[y]));
        if (targets.Count > 0)
        {
            int i = targets.IndexOf(g);
            if (i == -1)
            {
                target = targets[0];
            }
            else if (targets.Count > 1)
            {
                int j = (i + 1) % targets.Count;
                target = targets[j];
            }
        }
        return target;
    }

    private void AddPlanetaryBodies()
    {
        targetDB.AddTarget(Didymos, TargetDB.TargetType.ASTEROID, DidymosRadiusM);
        targetDB.AddTarget(Didymoon, TargetDB.TargetType.MOON, DidymoonRadiusM);
        targetDB.AddTarget(EezoApproach, TargetDB.TargetType.APPROACH, 0);
        targetDB.AddTarget(EezoDock, TargetDB.TargetType.DOCK, EezoDockingPortRadiusM);
        hudController.AddTargetIndicator(Didymos);
        hudController.AddTargetIndicator(Didymoon);
        hudController.AddTargetIndicator(EezoApproach);
        hudController.AddTargetIndicator(EezoDock);
    }

    private void AddHUDFixedIndicators()
    {
        hudController.AddFixedIndicators();
    }

    private void CleanupScene()
    {
        GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.OFF);
        hudController.ClearTargetIndicators();
        targetDB.ClearTargets();
        enemyTracker.ClearTimeToLive();
        DestroyPlayer();
    }
    
    public void DestroyEnemyShipByCollision(GameObject enemyShip)
    {
        enemyTracker.KillEnemy(enemyShip);
    }

    public bool IsEnemyActive(GameObject target)
    {
        TargetDB.TargetType t = targetDB.GetTargetType(target);
        bool isEnemy = t == TargetDB.TargetType.ENEMY;
        bool isDying = enemyTracker.IsEnemyDying(target);
        return isEnemy && !isDying;
    }

    public void DestroyEnemy(GameObject enemyShip)
    {
        if (enemyShip != null)
        {
            if (enemyShip == HUD().GetSelectedTarget())
            {
                GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.OFF);
            }
            hudController.RemoveIndicator(enemyShip.transform);
            targetDB.RemoveTarget(enemyShip);
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
