using UnityEngine;
using System.Collections.Generic;

public class EnemyTracker : MonoBehaviour {

    public float DestroyedEnemyTimeToLiveSec = 5;

    public GameController gameController;
    private Dictionary<GameObject, float> destroyedEnemiesTimeToLive = new Dictionary<GameObject, float>();

    // Use this for initialization
    void Start () {
	
	}

    void Update()
    {
        switch (gameController.GetGameState())
        {
            case GameController.GameState.START_NOT_ACCEPTING_INPUT:
                break;
            case GameController.GameState.START_AWAIT_INPUT:
                break;
            case GameController.GameState.RUNNING:
                UpdateCheckForDestroyedEnemies();
                break;
            case GameController.GameState.PAUSED:
                break;
            case GameController.GameState.GAME_OVER_NOT_ACCEPTING_INPUT:
                break;
            case GameController.GameState.GAME_OVER_AWAIT_INPUT:
                break;
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
            gameController.DestroyNPC(g, true);
        }
    }

    public void KillEnemy(GameObject target)
    {
        if (!destroyedEnemiesTimeToLive.ContainsKey(target))
        {
            destroyedEnemiesTimeToLive.Add(target, DestroyedEnemyTimeToLiveSec);
        }
    }

    public bool IsEnemyDying(GameObject target)
    {
        return destroyedEnemiesTimeToLive.ContainsKey(target);
    }

    public void ClearTimeToLive()
    {
        destroyedEnemiesTimeToLive.Clear();
    }

}
