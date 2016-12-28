using UnityEngine;
using System.Collections;

public class ShipWeapons : MonoBehaviour {

    public Weapon MainGun;
    public float MainGunRangeM = 1000;
    private GameController gameController;
    
    public float CurrentAmmo()
    {
        return MainGun.CurrentAmmo();
    }

    public void SetGameController(GameController c)
    {
        gameController = c;
        if (MainGun != null)
        {
            MainGun.SetGameController(c);
        }
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
