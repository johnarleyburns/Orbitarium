using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour {

    public GameObject PlayerShipPrefab;
    public GameObject FPSCanvas;
    public GameObject GameStartCanvas;
    public GameObject GameOverCanvas;
    public Text GameOverText;
    public Camera FPSCamera;
    public Camera OverShoulderCamera;
    public Camera OverviewCamera;
    public GameObject ReferenceBody;
    public GameObject Didymos;
    public GameObject Didymoon;

    private GameObject playerShip;

    public enum GameState
    {
        SPLASH,
        RUNNING,
        OVER
    }
    private GameState gameState;

	// Use this for initialization
	void Start () {
        gameState = GameState.SPLASH;
        EnableSplashScreen();
	}

    // Update is called once per frame
    void Update () {
        switch (gameState)
        {
            case GameState.SPLASH:
                if (Input.anyKeyDown)
                {
                    EnableRunningScreen();
                    gameState = GameState.RUNNING;
                }
                break;
            case GameState.RUNNING:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // pause is better;
                    EnableOverScreen();
                    gameState = GameState.OVER;
                }
                break;
            case GameState.OVER:
                if (Input.anyKeyDown)
                {
                    EnableSplashScreen();
                    gameState = GameState.SPLASH;
                }
                break;
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
        EnableOverScreen();
        gameState = GameState.OVER;
    }

    private void EnableSplashScreen()
    {
        InstantiatePlayer();
        SetupCameras();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        GameStartCanvas.SetActive(true);
        FPSCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
    }

    private void InstantiatePlayer()
    {
        if (playerShip != null)
        {
            GravityEngine.instance.RemoveBody(playerShip);
            Destroy(playerShip);
        }
        playerShip = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        PlayerShipController controller = playerShip.GetComponent<PlayerShipController>();
        controller.SetGameController(this);
    }

    private void SetupCameras()
    {
        PlayerShipController controller = playerShip.GetComponent<PlayerShipController>();
        GameObject playerModel = controller.GetShipModel();
        FPSCamera.GetComponent<FPSCameraController>().player = playerModel;
        FPSCamera.GetComponent<FPSCameraController>().UpdatePlayerPos();
        OverShoulderCamera.GetComponent<ThirdPartyCameraController>().player = playerModel;
        OverShoulderCamera.GetComponent<ThirdPartyCameraController>().UpdatePlayerPos();
        OverviewCamera.GetComponent<CameraSpin>().target = playerModel;
        OverviewCamera.GetComponent<CameraSpin>().UpdatePos();
    }

    private void EnableRunningScreen()
    {
        FPSCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        FPSCamera.enabled = true;
        OverviewCamera.enabled = false;
        OverShoulderCamera.enabled = false;
    }

    private void EnableOverScreen()
    {
        OverviewCamera.GetComponent<CameraSpin>().UpdatePos();
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        GameOverCanvas.SetActive(true);
        GameStartCanvas.SetActive(false);
        FPSCanvas.SetActive(false);
    }

    private static int HUD_INDICATOR_DIDYMOS = 0;
    private static int HUD_INDICATOR_RELV_PRO = 1;
    private static int HUD_INDICATOR_RELV_RETR = 2;
    private static int HUD_INDICATOR_DIDYMOON = 3;
    private static float RelativeVelocityIndicatorScale = 1000;

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
        float referenceBodyDist;
        float referenceBodyRelV;
        Vector3 refBodyRelVelUnit;
        CalcRelV(source, ReferenceBody, out referenceBodyDist, out referenceBodyRelV, out refBodyRelVelUnit);
        Greyman.OffScreenIndicator offScreenIndicator = GetComponent<InputController>().HUDLogic.GetComponent<Greyman.OffScreenIndicator>();
        if (offScreenIndicator.indicators[HUD_INDICATOR_DIDYMOS].hasOnScreenText)
        {
            string targetName = Didymos.name;
            string targetString = string.Format("{0}\n{1:0,0} m\n{2:0,0.0} m/s", targetName, referenceBodyDist, referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_DIDYMOS, targetString);
        }
        if (offScreenIndicator.indicators[HUD_INDICATOR_DIDYMOON].hasOnScreenText)
        {
            float moonBodyDist;
            float moonBodyRelV;
            Vector3 moonBodyRelVelUnit;
            CalcRelV(source, Didymoon, out moonBodyDist, out moonBodyRelV, out moonBodyRelVelUnit);
            string targetName = Didymoon.name;
            string targetString = string.Format("{0}\n{1:0,0} m\n{2:0,0.0} m/s", targetName, moonBodyDist, moonBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_DIDYMOON, targetString);
        }

        Vector3 relVIndicatorScaled = RelVIndicatorScaled(refBodyRelVelUnit);
        Vector3 myPos = source.transform.position;
        if (offScreenIndicator.indicators[HUD_INDICATOR_RELV_PRO].hasOnScreenText)
        {
            GetComponent<InputController>().RelativeVelocityDirectionIndicator.transform.position = myPos + relVIndicatorScaled;
            string targetString = string.Format("PRO {0:0,0.0} m/s", referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_PRO, targetString);
        }
        if (offScreenIndicator.indicators[HUD_INDICATOR_RELV_RETR].hasOnScreenText)
        {
            GetComponent<InputController>().RelativeVelocityAntiDirectionIndicator.transform.position = myPos + -relVIndicatorScaled;
            string targetString = string.Format("RETR {0:0,0.0} m/s", -referenceBodyRelV);
            offScreenIndicator.UpdateIndicatorText(HUD_INDICATOR_RELV_RETR, targetString);
        }
    }

}
