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
    public float EnemyDistanceMeters = 500;
    public float EnemyRandomSpreadMeters = 200;
    public float EnemyInitialCount = 3;

    private OffScreenIndicator OffscreenIndicator;
    private GameObject playerShip;
    private List<GameObject> enemyShips = new List<GameObject>();
    private List<GameObject> targets = new List<GameObject>();
    private GameObject referenceBody = null;
    private GameObject selectedTarget = null;
    private int selectedTargetIndex = -1;
    private static int HUD_INDICATOR_DIDYMOS = 0;
    private static int HUD_INDICATOR_RELV_PRO = 1;
    private static int HUD_INDICATOR_RELV_RETR = 2;
    private static int HUD_INDICATOR_DIDYMOON = 3;
    private static int HUD_INDICATOR_ENEMY_SHIP_TEMPLATE = 4;
    private static int HUD_INDICATOR_TARGET_DIRECTION = 5;
    private static float RelativeVelocityIndicatorScale = 1000;
    private Dictionary<GameObject, int> targetIndicatorId = new Dictionary<GameObject, int>();
    private bool inTargetTap = false;
    private float doubleTapTargetTimer = 0;
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
        TransitionToStarting();
    }

    #region Transitions

    private void TransitionToStarting()
    {
        gameState = GameState.STARTING;
        OffscreenIndicator = GetComponent<InputController>().HUDLogic.GetComponent<Greyman.OffScreenIndicator>();
        gameStartInputTimer = 0.5f;
        ResetGravityEngine();
        HideTargetIndicator();
        GetComponent<InputController>().TargetDirectionIndicator.SetActive(true);
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
        OffscreenIndicator.RemoveIndicators();
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
        OffscreenIndicator.RemoveIndicators();
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
        UpdateHUD();
        UpdateTargetSelection();
    }

    private void UpdateTargetSelection()
    {
        if (!inTargetTap)
        {
            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                inTargetTap = true;
                doubleTapTargetTimer = 0.2f;
            }
        }
        else // user tapped before
        {
            if (doubleTapTargetTimer <= 0) // double tap time has passed, do default select
            {
                SelectNextTarget(1);
                inTargetTap = false;
                doubleTapTargetTimer = 0;
            }
            else // still waiting to see if user double tapped
            {
                if (Input.GetKeyDown(KeyCode.KeypadMultiply)) // user doubletapped
                {
                    PlayerShipController controller = playerShip.GetComponent<PlayerShipController>();
                    GameObject playerModel = controller.GetShipModel();
                    playerModel.GetComponent<PlayerShip>().RotateTowardsTarget();
                    inTargetTap = false;
                    doubleTapTargetTimer = 0;
                }
                else // continue countdown
                {
                    doubleTapTargetTimer -= Time.deltaTime;
                }
            }
        }
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

    public GameObject GetReferenceBody()
    {
        return referenceBody;
    }

    private void HideTargetIndicator()
    {
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOnScreen = false;
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOffScreen = false;
    }

    private void ShowTargetIndicator()
    {
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOnScreen = true;
        OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].showOffScreen = true;
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
        int newIndicatorId = OffscreenIndicator.AddNewIndicatorFromClone(HUD_INDICATOR_ENEMY_SHIP_TEMPLATE, enemyShip.name);
        OffscreenIndicator.AddIndicator(enemyShip.transform, newIndicatorId);
        enemyShips.Add(enemyShip);
        AddTarget(enemyShip, newIndicatorId);
        SelectNextTarget(1);
    }

    private void SelectNextTarget(int offset)
    {
        // enemy-only selector
        /*
        if (enemyShips.Count > 0)
        {
            selectedTargetIndex = (selectedTargetIndex + offset) % enemyShips.Count;
            if (selectedTargetIndex < 0)
            {
                selectedTargetIndex = enemyShips.Count + selectedTargetIndex;
            }
            selectedTarget = enemyShips[selectedTargetIndex];
            GetComponent<InputController>().TargetToggleText.text = selectedTarget.name;
            ShowTargetIndicator();
            SelectNextReferenceBody(selectedTarget);
        }
        else
        {
            selectedTargetIndex = -1;
            selectedTarget = null;
            GetComponent<InputController>().TargetToggleText.text = "TARGET";
            HideTargetIndicator();
            SelectNextReferenceBody();
        }
        */
        if (targets.Count > 0)
        {
            if (selectedTargetIndex < 0)
            {
                selectedTargetIndex = targets.Count + selectedTargetIndex;
            }
            else
            {
                selectedTargetIndex = (selectedTargetIndex + offset) % targets.Count;
            }
            selectedTarget = targets[selectedTargetIndex];
            GetComponent<InputController>().TargetToggleText.text = selectedTarget.name;
            ShowTargetIndicator();
            SelectNextReferenceBody(selectedTarget);
        }
        else
        {
            selectedTargetIndex = -1;
            selectedTarget = null;
            GetComponent<InputController>().TargetToggleText.text = "NONE";
            HideTargetIndicator();
            SelectNextReferenceBody();
        }
    }

    private void SelectNextReferenceBody(GameObject specificTarget = null)
    {
        if (specificTarget != null)
        {
            referenceBody = specificTarget;
        }
        else if (targets.Count > 0)
        {
            referenceBody = targets[targets.Count - 1];
        }
        else
        {
            referenceBody = null;
        }
    }

    private void AddTarget(GameObject target, int indicatorId)
    {
        targetIndicatorId[target] = indicatorId;
        targets.Add(target);
        SelectNextReferenceBody();
    }

    private void AddFixedTargets()
    {
        AddTarget(Didymos, HUD_INDICATOR_DIDYMOS);
        AddTarget(Didymoon, HUD_INDICATOR_DIDYMOON);
        OffscreenIndicator.AddFixedIndicators();
    }

    private void CleanupScene()
    {
        ClearTargets();
        DestroyPlayer();
        DestroyEnemies();
    }

    private void ClearTargets()
    {
        selectedTarget = null;
        selectedTargetIndex = -1;
        targetIndicatorId.Clear();
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
    }

    private void DestroyEnemy(GameObject enemyShip)
    {
        if (enemyShip != null)
        {
            if (enemyShip == selectedTarget)
            {
                SelectNextTarget(1);
            }
            enemyShips.Remove(enemyShip);
            if (enemyShips.Count == 0)
            {
                SelectNextTarget(0);
            }
            //GameObject enemyModel = enemyShip.GetComponent<enemyShipController>().GetShipModel();
            //if (enemyModel != null && enemyModel.activeInHierarchy)
            //{
            //    enemyModel.GetComponent<enemyShipController>().GetComponent<Spaceship>().PrepareDestroy();
            //}
            GravityEngine.instance.RemoveBody(enemyShip);
            Destroy(enemyShip);
            enemyShip = null;
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

    private void EnableOverviewCamera()
    {
        GameObject playerModel = playerShip.GetComponent<PlayerShipController>().GetShipModel();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
    }

    private static Vector3 RelVIndicatorScaled(Vector3 relVelUnit)
    {
        return RelativeVelocityIndicatorScale * relVelUnit;
    }

    public void UpdateHUD()
    {
        Transform source = playerShip.transform;
        foreach (GameObject target in targets)
        {
            int indicatorId = targetIndicatorId[target];
            UpdateTargetIndicator(source, indicatorId, target);
        }
    }

    private void UpdateTargetIndicator(Transform source, int index, GameObject target)
    {
        bool hasText = OffscreenIndicator.indicators[index].hasOnScreenText;
        bool isRefBody = target == referenceBody;
        bool isSelectedTarget = target == selectedTarget;
        bool calcRelV = hasText || isRefBody || isSelectedTarget;
        if (calcRelV)
        {
            float targetDist;
            float targetRelV;
            Vector3 targetRelVUnitVec;
            RocketShip.CalcRelV(source, target, out targetDist, out targetRelV, out targetRelVUnitVec);
            if (hasText)
            {
                string targetString = string.Format("{0}\n{1:0,0} m", target.name, targetDist);
                OffscreenIndicator.UpdateIndicatorText(index, targetString);
            }
            if (isRefBody)
            {
                Vector3 relVIndicatorScaled = RelVIndicatorScaled(targetRelVUnitVec);
                Vector3 myPos = source.transform.position;
                GetComponent<InputController>().RelativeVelocityDirectionIndicator.transform.position = myPos + relVIndicatorScaled;
                if (OffscreenIndicator.indicators[HUD_INDICATOR_RELV_PRO].hasOnScreenText)
                {
                    OffscreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_PRO, "POS");
                }
                GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator.transform.position = myPos + -relVIndicatorScaled;
                if (OffscreenIndicator.indicators[HUD_INDICATOR_RELV_RETR].hasOnScreenText)
                {
                    OffscreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_RETR, "NEG");
                }
            }
            if (isSelectedTarget)
            {
                GetComponent<InputController>().TargetDirectionIndicator.transform.position = target.transform.position;
                if (OffscreenIndicator.indicators[HUD_INDICATOR_TARGET_DIRECTION].hasOnScreenText)
                {
                    string targetString = string.Format("{0:0,0.0} m/s", targetRelV);
                    OffscreenIndicator.UpdateIndicatorText(HUD_INDICATOR_TARGET_DIRECTION, targetString);
                }
            }
        }
    }

}
