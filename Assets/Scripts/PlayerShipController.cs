using UnityEngine;

public class PlayerShipController : MonoBehaviour {

    public GameObject ShipModel;
    private GameController _gameController;

    public GameController gameController
    {
        get
        {
            return _gameController;
        }
        set
        {
            _gameController = value;
            ShipModel.GetComponent<PlayerShip>().gameController = value;
            ShipModel.GetComponent<RocketShip>().gameController = value;
            ShipModel.GetComponent<Autopilot>().gameController = value;
            ShipModel.GetComponent<ShipWeapons>().gameController = value;
        }
    }

}
