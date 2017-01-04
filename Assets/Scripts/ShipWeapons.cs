using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ShipWeapons : MonoBehaviour {

    public NBodyDimensions NBodyDimensions;
    public Weapon MainGun;
    public float MainGunRangeM = 1000;
    public GameObject MissilePrefab;
    public GameObject[] MissileSlotPositions;
    public bool isPlayer;

    private GameController _gameController;
    private bool initMissilesOnLateUpdate = false;
    private Dictionary<GameObject, GameObject> MissileSlots = new Dictionary<GameObject, GameObject>();

    public float CurrentAmmo()
    {
        return MainGun.CurrentAmmo();
    }

    public GameController gameController
    {
        get
        {
            return _gameController;
        }
        set
        {
            _gameController = value;
            if (MainGun != null)
            {
                MainGun.SetGameController(value);
            }
        }
    }

    void Start () {
    }

    void Update () {
	}

    void LateUpdate()
    {
        /*
        if (initMissilesOnLateUpdate)
        {
            foreach (GameObject missile in MissileSlots.Values)
            {
                GravityEngine.instance.InactivateBody(missile);
            }
            initMissilesOnLateUpdate = false;
        }
        */
    }

    public void AddMissiles()
    {
        int i = 0;
        foreach (GameObject missileSlotPosition in MissileSlotPositions)
        {
            if (!MissileSlots.ContainsKey(missileSlotPosition))
            {
                GameObject missile = Instantiate(MissilePrefab, missileSlotPosition.transform) as GameObject;
                missile.name = "Missile " + (i + 1);
                missile.transform.localPosition = Vector3.zero;
                missile.transform.localRotation = Quaternion.identity;
                missile.transform.GetChild(0).transform.localPosition = Vector3.zero;
                missile.transform.GetChild(0).transform.localRotation = Quaternion.identity;
                missile.transform.GetChild(1).transform.localPosition = Vector3.zero;
                missile.transform.GetChild(1).transform.localRotation = Quaternion.identity;
                missile.GetComponent<MissileShipController>().SetGameController(gameController);
                MissileSlots[missileSlotPosition] = missile;
                //if (!isPlayer) { // no autodetect 
                //    GravityEngine.instance.AddBody(missile);
                //    GravityEngine.instance.InactivateBody(missile);
                //}
            }
            i++;
        }
        if (isPlayer)
        {
            gameController.GetComponent<InputController>().PropertyChanged("MissileCount", MissileSlots.Count);
            initMissilesOnLateUpdate = true; // fix after autodetect
        }
    }

    public bool FireFirstAvailableMissile(GameObject target)
    {
        bool success = false;
        KeyValuePair<GameObject, GameObject> available = GetFirstAvailableMissile();
        GameObject missileSlot = available.Key;
        GameObject missile = available.Value;
        if (missile != null)
        {
            MissileShip missileShip = missile.GetComponent<MissileShipController>().MissileShip();
            missileShip.SetTarget(target);
            success = missileShip.Fire();
            if (success)
            {
                MissileSlots.Remove(missileSlot);
                TargetDB.TargetType missileType = isPlayer ? TargetDB.TargetType.FRIEND : TargetDB.TargetType.ENEMY;
                gameController.TargetData().AddTarget(missile, missileType);
                gameController.HUD().AddTargetIndicator(missile);
                if (isPlayer)
                {
                    gameController.GetComponent<InputController>().PropertyChanged("MissileCount", MissileSlots.Count);
                }
            }
        }
        return success;
    }

    public bool FireGuns()
    {
        bool firing;
        if (GunsReady())
        {
            StartCoroutine(FireGunsCo());
            firing = true;
        }
        else
        {
            firing = false;
        }
        return firing;
    }

    public IEnumerator FireGunsCo()
    {
        if (GunsReady())
        {
            yield return MainGun.AIFiringCo();
        }
        else
        {
            yield break;
        }
    }

    public bool GunsReady()
    {
        return MainGun != null && MainGun.ReadyToFire();
    }

    private KeyValuePair<GameObject, GameObject> GetFirstAvailableMissile()
    {
        GameObject missile = null;
        GameObject missileSlot = null;
        foreach (GameObject missileSlotPosition in MissileSlotPositions)
        {
            if (MissileSlots.TryGetValue(missileSlotPosition, out missile))
            {
                missileSlot = missileSlotPosition;
                break;
            }
        }
        return new KeyValuePair<GameObject, GameObject>(missileSlot, missile);
    }

}
