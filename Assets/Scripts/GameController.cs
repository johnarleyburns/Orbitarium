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

    private HUDController hudController;
    private GameObject playerShip;
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
        gameState = GameState.START_NOT_ACCEPTING_INPUT;
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
        playerShip.GetComponent<PlayerShipController>().GetShipModel().GetComponent<PlayerShip>().UpdateShip();
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
        playerShip = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        playerShip.transform.position = Vector3.zero;
        playerShip.transform.rotation = Quaternion.identity;
        PlayerShipController controller = playerShip.GetComponent<PlayerShipController>();
        controller.SetGameController(this);
        GravityEngine.instance.AddBody(playerShip);
        GameObject playerModel = controller.GetShipModel();
        SetupCameras(playerModel);
        playerModel.GetComponent<PlayerShip>().StartShip();
    }

    private void SetupInitialVelocities()
    {
        float playerImpulse = Random.Range(0, PlayerInitialImpulse);
        GravityEngine.instance.ApplyImpulse(playerShip.GetComponent<NBody>(), playerImpulse * playerShip.transform.forward);
        foreach (GameObject enemyShip in enemyShips)
        {
            float enemyImpulse = Random.Range(0, EnemyInitialImpulse);
            GravityEngine.instance.ApplyImpulse(enemyShip.GetComponent<NBody>(), enemyImpulse * -playerShip.transform.forward);
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
        hudController.SelectNextTarget(1);
    }

    private void EnableOverviewCamera()
    {
        GameObject playerModel = playerShip.GetComponent<PlayerShipController>().GetShipModel();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

    private void AddTarget(GameObject target)
    {
        targets.Add(target);
        hudController.SelectNextReferenceBody();
    }

    public GameObject GetPlayer()
    {
        return playerShip;
    }

    public int EnemyCount()
    {
        return enemyShips.Count;
    }

    public GameObject GetEnemy(int i)
    {
        if (i < enemyShips.Count)
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
        if (i < targets.Count)
        {
            return targets[i];
        }
        else
        {
            return null;
        }
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
            if (enemyShip == hudController.SelectedTarget())
            {
                hudController.SelectNextTarget(1);
            }
            targets.Remove(enemyShip);
            hudController.RemoveIndicator(enemyShip.transform);
            enemyShips.Remove(enemyShip);
            if (enemyShips.Count == 0)
            {
                hudController.SelectNextTarget(0);
            }
            //GravityEngine.instance.RemoveBody(enemyShip);     
            //Destroy(enemyShip);
            GravityEngine.instance.InactivateBody(enemyShip);
            enemyShip.SetActive(false);
        }
    }

    private void DestroyPlayer()
    {
        if (playerShip != null)
        {
            /*
            GameObject playerModel = playerShip.GetComponent<PlayerShipController>().GetShipModel();
            if (playerModel != null && playerModel.activeInHierarchy)
            {
                playerModel.GetComponent<PlayerShipController>().GetComponent<PlayerShip>().PrepareDestroy();
            }
            */
            GravityEngine.instance.RemoveBody(playerShip);
            Destroy(playerShip);
            playerShip = null;
        }
    }

    private void SetupCameras(GameObject playerModel)
    {
        FPSCamera.GetComponent<FPSCameraController>().UpdatePlayer(playerModel);
        OverShoulderCamera.GetComponent<ThirdPartyCameraController>().UpdatePlayer(playerModel);
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

}
