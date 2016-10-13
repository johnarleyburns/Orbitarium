using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour {

    public GameObject FPSCanvas;
    public GameObject GameStartCanvas;
    public GameObject GameOverCanvas;
    public Text GameOverText;
    public Camera FPSCamera;
    public Camera OverShoulderCamera;
    public Camera OverviewCamera;

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
        OverviewCamera.enabled = true;
        FPSCamera.enabled = false;
        OverShoulderCamera.enabled = false;
        GameStartCanvas.SetActive(true);
        FPSCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
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

}
