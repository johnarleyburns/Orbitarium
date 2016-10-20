﻿using UnityEngine;
using UnityEngine.UI;
using Greyman;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{

    public GameObject PlayerShipPrefab;
    public GameObject EnemyShipPrefab;
    public GameObject FPSCanvas;
    public GameObject GameStartCanvas;
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
    private float gameStartTimer = -1;
    private bool startedGameOverTransition = false;
    private bool doPlayGameOverTransition = false;
    private float gameOverTimer = -1;

    public enum GameState
    {
        SPLASH,
        RUNNING,
        OVER
    }
    private GameState gameState;

    void Start()
    {
        gameState = GameState.SPLASH;
        OffscreenIndicator = GetComponent<InputController>().HUDLogic.GetComponent<Greyman.OffScreenIndicator>();
        gameStartTimer = 0.5f;
        EnableSplashScreen();
    }

    private void ResetGravityEngine()
    {
        GravityEngine.instance.Clear();
        GravityEngine.instance.Setup();
        GravityEngine.instance.SetEvolve(true);
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameState.SPLASH:
                UpdateCheckForGameStart();
                break;
            case GameState.RUNNING:
                UpdateTargetSelection();
                UpdateCheckForGamePause();
                break;
            case GameState.OVER:
                UpdateGameOver();
                break;
        }
    }

    private void UpdateCheckForGameStart()
    {
        if (gameStartTimer > 0)
        {
            gameStartTimer -= Time.deltaTime;
        }
        else if (gameStartTimer == -1)
        {
            if (Input.anyKey)
            {
                gameStartTimer = 0.5f;
            }
        }
        else
        {
            EnableRunningScreen();
            gameState = GameState.RUNNING;
            gameStartTimer = -1;
        }
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
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            // pause is better;
            GameOver("Player Quit");
        }
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public bool IsReady()
    {
        return gameState == GameState.RUNNING;
    }

    public void GameOver(string gameOverText)
    {
        GameOverText.text = gameOverText
            + "\nPress any key to continue";
        startedGameOverTransition = true;
        doPlayGameOverTransition = true;
        gameOverTimer = 3f;
        gameState = GameState.OVER;
    }

    private void UpdateGameOver()
    {
        if (startedGameOverTransition)
        {
            if (gameOverTimer > 0)
            {
                if (doPlayGameOverTransition)
                {
                    EnableGameOverScreen();
                    doPlayGameOverTransition = false;
                }
                gameOverTimer -= Time.deltaTime;
            }
            else
            {
                if (Input.anyKey)
                {
                    CleanupScene();
                    EnableSplashScreen();
                    gameState = GameState.SPLASH;
                    gameOverTimer = -1;
                    startedGameOverTransition = false;
                }
            }
        }
    }

    public GameObject GetReferenceBody()
    {
        return referenceBody;
    }

    private void EnableSplashScreen()
    {
        ResetGravityEngine();
        HideTargetIndicator();
        GetComponent<InputController>().TargetDirectionIndicator.SetActive(true);
        InstantiatePlayer();
        EnableOverviewCamera();
        GameStartCanvas.SetActive(true);
        FPSCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
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

    private void EnableRunningScreen()
    {
        AddFixedTargets();
        InstantiateEnemies();
        EnableFPSCamera();
    }

    private void EnableGameOverScreen()
    {
        EnableOverviewCamera();
        GameOverCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        FPSCanvas.SetActive(false);
    }

    private void EnableOverviewCamera()
    {
        GameObject playerModel = playerShip.GetComponent<PlayerShipController>().GetShipModel();
        OverviewCamera.GetComponent<CameraSpin>().UpdateTarget(playerModel);
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
    }

    private void EnableFPSCamera()
    {
        FPSCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        FPSCamera.enabled = true;
        OverviewCamera.enabled = false;
        OverShoulderCamera.enabled = false;
    }

    private static Vector3 RelVIndicatorScaled(Vector3 relVelUnit)
    {
        return RelativeVelocityIndicatorScale * relVelUnit;
    }

    public static void CalcRelV(Transform source, GameObject target, out float dist, out float relv, out Vector3 relVelUnit)
    {
        dist = (target.transform.position - source.transform.position).magnitude;
        Vector3 myVel = GravityEngine.instance.GetVelocity(source.gameObject);
        Vector3 targetVel = GravityEngine.instance.GetVelocity(target);
        Vector3 relVel = myVel - targetVel;
        Vector3 targetPos = target.transform.position;
        Vector3 myPos = source.transform.position;
        Vector3 relLoc = targetPos - myPos;
        float relVelDot = Vector3.Dot(relVel, relLoc);
        float relVelScalar = relVel.magnitude;
        relv = Mathf.Sign(relVelDot) * relVelScalar;
        relVelUnit = relVel.normalized;
    }

    public void UpdateHUD(Transform source)
    {
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
            CalcRelV(source, target, out targetDist, out targetRelV, out targetRelVUnitVec);
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
