﻿using UnityEngine;
using System.Collections;

public class EnemyShipController : MonoBehaviour {

    public GameObject ShipModel;
    private GameController _gameController;
    private NBodyDimensions _nBodyDimensions;

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

    public NBodyDimensions NBodyDimensions
    {
        get
        {
            return _nBodyDimensions;
        }
        set
        {
            _nBodyDimensions = value;
            ShipModel.GetComponent<EnemyShip>().NBodyDimensions = value;
            ShipModel.GetComponent<RocketShip>().NBodyDimensions = value;
            ShipModel.GetComponent<Autopilot>().NBodyDimensions = value;
            ShipModel.GetComponent<ShipWeapons>().NBodyDimensions = value;
        }
    }

}
