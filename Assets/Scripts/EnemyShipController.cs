using UnityEngine;
using System.Collections;

public class EnemyShipController : MonoBehaviour {

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
            ShipModel.GetComponent<EnemyShip>().gameController = value;
            ShipModel.GetComponent<RocketShip>().gameController = value;
            ShipModel.GetComponent<Autopilot>().gameController = value;
            ShipModel.GetComponent<ShipWeapons>().gameController = value;
        }
    }

    public EnemyShip EnemyShip()
    {
        return ShipModel.GetComponent<EnemyShip>();
    }

}
