using UnityEngine;

public class PlayerShipController : MonoBehaviour {

    private static int SHIP_MODEL_INDEX = 0;
    private GameObject ShipModel;

    void Awake()
    {
        ShipModel = transform.GetChild(SHIP_MODEL_INDEX).gameObject;
    }

    public void SetGameController(GameController controller)
    {
        ShipModel.GetComponent<PlayerShip>().gameController = controller;
        ShipModel.GetComponent<RocketShip>().gameController = controller;
    }

    public GameObject GetShipModel()
    {
        return ShipModel;
    }
}
