using UnityEngine;
using System.Collections;

public class MissileShipController : MonoBehaviour {

    private static int SHIP_MODEL_INDEX = 0;
    private GameObject ShipModel;

    void Awake()
    {
        ShipModel = transform.GetChild(SHIP_MODEL_INDEX).gameObject;
    }

    public void SetGameController(GameController controller)
    {
        ShipModel.GetComponent<MissileShip>().gameController = controller;
        ShipModel.GetComponent<RocketShip>().gameController = controller;
        ShipModel.GetComponent<Autopilot>().gameController = controller;
    }

    public GameObject GetShipModel()
    {
        return ShipModel;
    }

    public MissileShip MissileShip()
    {
        return ShipModel.GetComponent<MissileShip>();
    }
}
