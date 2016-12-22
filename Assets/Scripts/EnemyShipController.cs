using UnityEngine;
using System.Collections;

public class EnemyShipController : MonoBehaviour {

    private static int SHIP_MODEL_INDEX = 0;
    private GameObject ShipModel;

    void Awake()
    {
        ShipModel = transform.GetChild(SHIP_MODEL_INDEX).gameObject;
    }

    public void SetGameController(GameController controller)
    {
        ShipModel.GetComponent<EnemyShip>().gameController = controller;
        ShipModel.GetComponent<RocketShip>().gameController = controller;
        ShipModel.GetComponent<Autopilot>().gameController = controller;
    }

    public GameObject GetShipModel()
    {
        return ShipModel;
    }

    public EnemyShip EnemyShip()
    {
        return ShipModel.GetComponent<EnemyShip>();
    }

}
