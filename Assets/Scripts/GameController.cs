using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public GameObject NBodyPrefab;
    public GameObject PlayerShipPrefab;
    public GameObject EnemyShipPrefab;
    public GameObject FPSCanvas;
    public GameObject GameStartCanvas;
    public GameObject GamePauseCanvas;
    public GameObject GameOverCanvas;
    public Text GameOverText;
    public Camera UICamera;
    public Camera NearCamera;
    public Camera FarCamera;
    public bool UseAdditionalCameras;
    public Camera RearNearCamera;
    public Camera RearFarCamera;
    public Camera LeftNearCamera;
    public Camera LeftFarCamera;
    public Camera RightNearCamera;
    public Camera RightFarCamera;
    public Camera OverShoulderNearCamera;
    public Camera OverShoulderFarCamera;
    public Camera OverviewUICamera;
    public Camera OverviewNearCamera;
    public Camera OverviewFarCamera;
    public GameObject ReferenceBody;
    public GameObject ReferenceBodyTarget;
    //public GameObject Didymos;
    //public GameObject Didymoon;
    //public GameObject EezoDock;
    //public GameObject EezoDockGhost;
    //public GameObject EezoDockingPort;
    public Transform PlayerSpawn;
    public Transform EnemySpawn;
    public PlayerStartMode StartMode;
    public float PlayerInitialImpulse;
    public float EnemyRandomSpreadMeters = 200;
    public float EnemyInitialCount = 3;
    public float EnemyInitialImpulse = 10;
    public float UndockVelocity = 1f;
    public Material StrobeMaterial;
    public string StrobeTag = "Strobe";

    private HUDController hudController;
    private InputController inputController;
    private MusicController musicController;
    private TargetDB targetDB;
    private EnemyTracker enemyTracker;
    private GameObject player;
    private Dictionary<GameObject, GameObject> followGhost = new Dictionary<GameObject, GameObject>();
    private float gameStartInputTimer = -1;
    private float gameOverInputTimer = -1;
    private GameState gameState;
    private bool transitionToStartingLateUpdate = false;

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
        GravityEngine.instance.Setup();
        hudController = GetComponent<HUDController>();
        inputController = GetComponent<InputController>();
        musicController = GetComponent<MusicController>();
        targetDB = GetComponent<TargetDB>();
        enemyTracker = GetComponent<EnemyTracker>();
        targetDB.gameController = this;
        enemyTracker.gameController = this;
        StartFollowGhost();
        GravityEngine.instance.SetEvolve(true);
        TransitionToStarting();
//        transitionToStartingLateUpdate = true; // for gravity engine
    }

    private void StartFollowGhost()
    {
        
        followGhost = new Dictionary<GameObject, GameObject>()
        {
        //    {
        //        EezoDockGhost,
        //        EezoDock
        //    }
        };
    }

    private void UpdateFollows()
    {

        foreach (GameObject ghost in followGhost.Keys)
        {
            GameObject shadow = followGhost[ghost];

            //shadow.transform.position = ghost.transform.position;
            shadow.transform.rotation = ghost.transform.rotation;
            //shadow.transform.GetChild(0).localScale = ghost.transform.GetChild(0).localScale;
            //shadow.transform.GetChild(0).position = ghost.transform.GetChild(0).position;
            shadow.transform.GetChild(0).rotation = ghost.transform.GetChild(0).rotation;
            Vector3 pos = ghost.transform.position;
            Vector3 vel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(ghost));
            GravityEngine.instance.UpdatePositionAndVelocity(shadow.GetComponent<NBody>(), pos, vel);
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
        hudController.HideTargetIndicator();
        InstantiatePlayer();
        EnableOverviewCamera();
        UpdateCanvasState(GameState.START_NOT_ACCEPTING_INPUT);
        SetupStrobes();
        musicController.StartMusic.Play();
        gameState = GameState.START_NOT_ACCEPTING_INPUT;
        Speak(DialogText.ReadyPlayerOne);
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
        //strobePositionTimer = 0;
        float SecBetweenFlash = 1;
        float strobeTimeStep = lightData.Length == 0 ? 20 : SecBetweenFlash / lightData.Length;
        float globalBrightnessOffset = 0;
        float fov = Camera.main.fieldOfView;
        float screenHeight = Screen.height;
        SpriteLights.Init(strobeTimeStep, globalBrightnessOffset, fov, screenHeight);
    }

    private GameObject[] strobes = new GameObject[0];
    //private float strobePositionTimer = 0;
    //private float secondsBetweenStrobeUpdate = 1;
    private SpriteLights.LightData[] lightData = new SpriteLights.LightData[0];

    private void TransitionToRunning()
    {
        AddPlanetaryBodies();
        AddHUDFixedIndicators();
        InstantiateEnemies();
        SetupInitialVelocities();
        hudController.SelectNextTargetPreferClosestEnemy();
        //inputController.PropertyChanged("SelectDockTarget", EezoDock);
        UpdateCanvasState(GameState.RUNNING);
        EnableFPSCamera();
        musicController.StartMusic.Stop();
        musicController.RunningBackgroundMusic.Play();
        gameState = GameState.RUNNING;
    }


    private void TransitionToPaused()
    {
        Time.timeScale = 0;
        UpdateCanvasState(GameState.PAUSED);
        EnableFPSCamera();
        musicController.RunningBackgroundMusic.Stop();
        gameState = GameState.PAUSED;
    }

    private void TransitionToRunningFromPaused()
    {
        UpdateCanvasState(GameState.RUNNING);
        EnableFPSCamera();
        Time.timeScale = 1.0f;
        musicController.RunningBackgroundMusic.Play();
        gameState = GameState.RUNNING;
    }

    private void UpdateCanvasState(GameState newState)
    {
        switch (newState)
        {
            case GameState.STARTING:
            case GameState.START_NOT_ACCEPTING_INPUT:
                FPSCanvas.SetActive(false);
                GameStartCanvas.SetActive(true);
                GamePauseCanvas.SetActive(false);
                GameOverCanvas.SetActive(false);
                break;
            case GameState.START_AWAIT_INPUT:
            case GameState.RUNNING:
                FPSCanvas.SetActive(true);
                GameStartCanvas.SetActive(false);
                GamePauseCanvas.SetActive(false);
                GameOverCanvas.SetActive(false);
                break;
            case GameState.PAUSED:
                FPSCanvas.SetActive(false);
                GameStartCanvas.SetActive(false);
                GamePauseCanvas.SetActive(true);
                GameOverCanvas.SetActive(false);
                break;
            case GameState.GAME_OVER_NOT_ACCEPTING_INPUT:
                FPSCanvas.SetActive(false);
                GameStartCanvas.SetActive(false);
                GamePauseCanvas.SetActive(false);
                GameOverCanvas.SetActive(true);
                break;
        }
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
        UpdateCanvasState(GameState.GAME_OVER_NOT_ACCEPTING_INPUT);
        gameState = GameState.GAME_OVER_NOT_ACCEPTING_INPUT;
    }

    public void TransitionToGameOverFromDeath(string otherName)
    {
        gameState = GameState.START_GAME_OVER;
        hudController.RemoveIndicators();
        Time.timeScale = 0.5f;
        gameOverInputTimer = 1f;
        string visibleName = DialogText.VisibleName(otherName);
        string msg = string.Format("Destroyed by {0}", visibleName);
        nextMsg = msg;
        GameOverText.text = msg + "\nTry again? (Y/N)";
        EnableOverviewCamera();
        UpdateCanvasState(GameState.GAME_OVER_NOT_ACCEPTING_INPUT);
        musicController.RunningBackgroundMusic.Stop();
        gameState = GameState.GAME_OVER_NOT_ACCEPTING_INPUT;
    }

    private string nextMsg = "";

    public void Speak(string text)
    {
        GetComponent<MFDController>().Speak(text);
    }

    private void TransitionToGameOverAwaitInput()
    {
        musicController.GameOverMusic.Play();
        if (!string.IsNullOrEmpty(nextMsg))
        {
            Speak(nextMsg);
            nextMsg = "";
        }
        gameState = GameState.GAME_OVER_AWAIT_INPUT;
    }

    private void TransitionToStartingFromGameOver()
    {
        musicController.GameOverMusic.Stop();
        CleanupScene();
        //FIXME always have base menu screen scene
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
        GameObject nBody = Instantiate(NBodyPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        player = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        player.GetComponent<PlayerShipController>().gameController = this;
        player.GetComponent<NBodyDimensions>().NBody = nBody;
        player.GetComponent<NBodyDimensions>().PlayerNBody = nBody;
        player.GetComponent<PlayerShipController>().NBodyDimensions = player.GetComponent<NBodyDimensions>();
        GravityEngine.instance.AddBody(nBody);
        nBody.name = "NBody " + player.name;

        GameObject playerModel = player.GetComponent<PlayerShipController>().ShipModel;
        SetupCameras(playerModel);
        playerModel.GetComponent<PlayerShip>().StartShip();

        switch (StartMode)
        {
            case PlayerStartMode.DOCKED:
                //GetPlayerShip().Dock(EezoDockingPort.transform.GetChild(0).gameObject, false);
                break;
            case PlayerStartMode.SPAWN:               
                player.transform.GetChild(0).transform.rotation = PlayerSpawn.rotation;
                GravityEngine.instance.UpdatePositionAndVelocity(nBody.GetComponent<NBody>(), PlayerSpawn.position, Vector3.zero);
                break;
        }
    }

    public enum PlayerStartMode
    {
        SPAWN,
        DOCKED
    }

    public void LateUpdate()
    {
        if (transitionToStartingLateUpdate)
        {
            transitionToStartingLateUpdate = false;
            TransitionToStarting();
        }
    }

    private void SetupInitialVelocities()
    {
        float playerImpulse = Random.Range(0, PlayerInitialImpulse);
        player.GetComponent<PlayerShipController>().ShipModel.GetComponent<RocketShip>().ApplyImpulse(player.transform.forward, playerImpulse, 1);
        foreach (GameObject enemyShip in targetDB.GetTargets(TargetDB.TargetType.ENEMY))
        {
            float enemyImpulse = Random.Range(0.1f * EnemyInitialImpulse, EnemyInitialImpulse);
            enemyShip.GetComponent<EnemyShipController>().ShipModel.GetComponent<RocketShip>().ApplyImpulse(enemyShip.transform.forward, enemyImpulse, 1);
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
        GameObject playerNBody = NUtils.GetNBodyGameObject(player);

        GameObject nBody = Instantiate(NBodyPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        GameObject enemy = Instantiate(EnemyShipPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        enemy.GetComponent<EnemyShipController>().gameController = this;
        enemy.GetComponent<NBodyDimensions>().NBody = nBody;
        enemy.GetComponent<NBodyDimensions>().PlayerNBody = playerNBody;
        enemy.GetComponent<EnemyShipController>().NBodyDimensions = player.GetComponent<NBodyDimensions>();
        GravityEngine.instance.AddBody(nBody);

        GameObject enemyModel = enemy.GetComponent<EnemyShipController>().ShipModel;
        enemyModel.GetComponent<EnemyShip>().StartShip();
        string nameRoot = enemyModel.GetComponent<EnemyShip>().VisibleName;
        enemy.name = string.Format("{0}-{1}", nameRoot, suffix);
        nBody.name = string.Format("NBody {0}-{1}", nameRoot, suffix);

        KeyValuePair < Vector3, Quaternion> spawn = NextEnemySpawn(playerNBody);
        enemy.transform.GetChild(0).transform.rotation = spawn.Value;
        GravityEngine.instance.UpdatePositionAndVelocity(nBody.GetComponent<NBody>(), spawn.Key, Vector3.zero);

        targetDB.AddTarget(enemy, TargetDB.TargetType.ENEMY);
        hudController.AddTargetIndicator(enemy);
    }

    private KeyValuePair<Vector3, Quaternion> NextEnemySpawn(GameObject playerNBody)
    {
        float scale = NUtils.GetNBodyToModelScale(player);
        Vector3 enemyOffset = (EnemyRandomSpreadMeters/scale) * Random.insideUnitSphere;
        Vector3 enemySpawnPoint = EnemySpawn.position + enemyOffset;
        Quaternion enemyRandomRotation = Quaternion.AngleAxis(Random.Range(0, 360), Random.onUnitSphere);
        Quaternion enemyLookRotation = Quaternion.LookRotation(enemySpawnPoint - playerNBody.transform.position); // facePlayer
        Quaternion enemyOffsetRotation = Quaternion.RotateTowards(enemyLookRotation, enemyRandomRotation, 90f);
        Quaternion enemySpawnRotation = enemyOffsetRotation * enemyLookRotation;
        return new KeyValuePair<Vector3, Quaternion>(enemySpawnPoint, enemySpawnRotation);
    }

    public void EnableFPSCamera()
    {
        UICamera.enabled = true;
        NearCamera.enabled = true;
        FarCamera.enabled = true;
        if (UseAdditionalCameras)
        {
            RearNearCamera.enabled = true;
            RearFarCamera.enabled = true;
            LeftNearCamera.enabled = true;
            LeftFarCamera.enabled = true;
            RightNearCamera.enabled = true;
            RightFarCamera.enabled = true;
        }
        else
        {
            RearNearCamera.enabled = false;
            RearFarCamera.enabled = false;
            LeftNearCamera.enabled = false;
            LeftFarCamera.enabled = false;
            RightNearCamera.enabled = false;
            RightFarCamera.enabled = false;
        }
        OverShoulderNearCamera.enabled = false;
        OverShoulderFarCamera.enabled = false;
        OverviewUICamera.enabled = false;
        OverviewNearCamera.enabled = false;
        OverviewFarCamera.enabled = false;
    }

    public void EnableOverviewCamera()
    {
        GameObject playerModel = player.GetComponent<PlayerShipController>().ShipModel;
        OverviewUICamera.enabled = true;
        OverviewNearCamera.enabled = true;
        OverviewFarCamera.enabled = true;
        UICamera.enabled = false;
        NearCamera.enabled = false;
        FarCamera.enabled = false;
        RearNearCamera.enabled = false;
        RearFarCamera.enabled = false;
        LeftNearCamera.enabled = false;
        LeftFarCamera.enabled = false;
        RightNearCamera.enabled = false;
        RightFarCamera.enabled = false;
        OverShoulderNearCamera.enabled = false;
        OverShoulderFarCamera.enabled = false;
        OverviewNearCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        OverviewFarCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
    }

    public void EnableOverShoulderCamera()
    {
        GameObject playerModel = player.GetComponent<PlayerShipController>().ShipModel;
        UICamera.enabled = true;
        OverShoulderNearCamera.enabled = true;
        OverShoulderFarCamera.enabled = true;
        NearCamera.enabled = false;
        FarCamera.enabled = false;
        RearNearCamera.enabled = false;
        RearFarCamera.enabled = false;
        LeftNearCamera.enabled = false;
        LeftFarCamera.enabled = false;
        RightNearCamera.enabled = false;
        RightFarCamera.enabled = false;
        OverviewUICamera.enabled = false;
        OverviewNearCamera.enabled = false;
        OverviewFarCamera.enabled = false;
        OverShoulderNearCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        OverShoulderFarCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public PlayerShip GetPlayerShip()
    {
        return player.GetComponent<PlayerShipController>().ShipModel.GetComponent<PlayerShip>();
    }

    public ShipWeapons GetPlayerWeapons()
    {
        return player.GetComponent<PlayerShipController>().ShipModel.GetComponent<ShipWeapons>();
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

        ReferenceBodyTarget.GetComponent<NBodyDimensions>().PlayerNBody = NUtils.GetNBodyGameObject(player);
        ReferenceBodyTarget.GetComponent<NBodyDimensions>().NBody = ReferenceBody;
        GravityEngine.instance.AddBody(ReferenceBody);
        targetDB.AddTarget(ReferenceBodyTarget, TargetDB.TargetType.PLANET);
        hudController.AddTargetIndicator(ReferenceBodyTarget);
        /*
        targetDB.AddTarget(Didymos, TargetDB.TargetType.ASTEROID);
        targetDB.AddTarget(Didymoon, TargetDB.TargetType.MOON);
        targetDB.AddTarget(EezoDock, TargetDB.TargetType.DOCK);
        hudController.AddTargetIndicator(Didymos);
        hudController.AddTargetIndicator(Didymoon);
        hudController.AddTargetIndicator(EezoDock);
        */
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
        string visibleName = DialogText.VisibleName(enemyShip.name);
        string msg = string.Format("{0} destroyed.", visibleName);
        Speak(msg);
    }

    public void DestroyMissileByCollision(GameObject missile)
    {
        DestroyNPC(missile);
    }

    public bool IsTargetActive(GameObject target)
    {
        bool active;
        TargetDB.TargetType t = targetDB.GetTargetType(target);
        bool isEnemy = t == TargetDB.TargetType.ENEMY;
        bool isDying = enemyTracker.IsEnemyDying(target);
        if (target == player)
        {
            active = true;
        }
        else if (isEnemy && !isDying)
        {
            active = true;
        }
        else
        {
            active = false;
        }
        return active;
    }

    public void DestroyNPC(GameObject ship)
    {
        if (ship != null)
        {
            if (ship == HUD().GetSelectedTarget())
            {
                GetPlayerShip().ExecuteAutopilotCommand(Autopilot.Command.OFF);
            }
            hudController.RemoveIndicator(ship.transform);
            targetDB.RemoveTarget(ship);
            hudController.SelectNextTargetPreferClosestEnemy();
            //GravityEngine.instance.RemoveBody(ship);     
            //Destroy(ship);
            GravityEngine.instance.InactivateBody(ship);
            ship.SetActive(false);
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
        //UICamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        NearCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        FarCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        RearNearCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        RearFarCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        LeftNearCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        LeftFarCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        RightNearCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        RightFarCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        OverShoulderNearCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        OverShoulderFarCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        //OverviewUICamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        OverviewNearCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
        OverviewFarCamera.GetComponent<OverShoulderCameraSpin>().UpdateTarget(playerModel);
    }

    public void Dock(GameObject ship, GameObject shipDockingPort, GameObject dockGhost, GameObject dockingPort)
    {
        GameObject myNBody = NUtils.GetNBodyGameObject(ship);
        GameObject dockNBody = NUtils.GetNBodyGameObject(dockGhost);
//      Vector3 shipDockOffset = shipDockingPort.transform.position - ship.transform.position;
        Vector3 pos = dockGhost.transform.position;
        Vector3 vel = GravityEngine.instance.GetVelocity(dockNBody);
        GravityEngine.instance.UpdatePositionAndVelocity(myNBody.GetComponent<NBody>(), pos, vel);
        GravityEngine.instance.InactivateBody(ship);
        ship.transform.parent = dockGhost.transform.parent;
        ship.transform.position = dockGhost.transform.position;
        ship.transform.rotation = dockGhost.transform.rotation;
        ship.transform.GetChild(0).transform.position = dockGhost.transform.GetChild(0).transform.position;
        ship.transform.GetChild(0).transform.rotation = dockGhost.transform.GetChild(0).transform.rotation;
        inputController.PropertyChanged("Dock", true);
    }

    public void Undock(GameObject ship)
    {
        GameObject nBodyObj = NUtils.GetNBodyGameObject(ship);
        GameObject parent = ship.transform.parent.gameObject;
        GameObject dockGhost = parent.transform.GetChild(0).gameObject;
        ship.transform.parent = null;
        GravityEngine.instance.ActivateBody(nBodyObj);
        Vector3 pos = dockGhost.transform.position;
        Vector3 vel = UndockVelocity * -nBodyObj.transform.forward;
        GravityEngine.instance.UpdatePositionAndVelocity(nBodyObj.GetComponent<NBody>(), pos, vel);
        inputController.PropertyChanged("Undock", true);
    }

}
