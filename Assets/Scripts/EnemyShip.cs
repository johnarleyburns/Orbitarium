using UnityEngine;
using System.Collections;

public class EnemyShip : MonoBehaviour {

    public GameController gameController;
    public string VisibleName;
    public float healthMax = 1;
    public float minRelVtoDamage = 5;
//    public bool hasRandomRotation = true;
//    public bool hasRandomV = true;
//    public float randomVMainEngineMaxSec = 5;
    public GameObject ShipExplosion;

    private float health;
    private RocketShip ship;
    private float secMainEngineBurnLeft;

    void Start()
    {
        health = healthMax;
        ship = GetComponent<RocketShip>();
//        if (hasRandomRotation)
//        {
//            InitRandomRotation();
//        }
//        StartTimedMainEngineBurn(Random.Range(0, randomVMainEngineMaxSec));
    }

    void Update()
    {
        UpdateTimedMainEngineBurn();
    }

//    private void InitRandomRotation()
//    {
//        Vector3 r = Random.insideUnitSphere;
//        Quaternion rot = Quaternion.Euler(r);
//        transform.rotation = rot;
//    }

    private void StartTimedMainEngineBurn(float timeSec)
    {
        secMainEngineBurnLeft = timeSec;
    }

    private void UpdateTimedMainEngineBurn()
    {
        if (secMainEngineBurnLeft > 0)
        {
            ship.MainEngineGo();
            secMainEngineBurnLeft -= Time.deltaTime;
        }
        else
        {
            StopTimedMainEngineBurn();
            secMainEngineBurnLeft = 0;
        }
    }

    private void StopTimedMainEngineBurn()
    {
        ship.MainEngineCutoff();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
            {
//                gameController.FPSCamera.GetComponent<FPSCameraController>().PlayMainEngineShake();
                if (relVel >= minRelVtoDamage)
                {
                    health--;
                }
            }
            else
            {
                health = 0; // boom
                ShipExplosion.GetComponent<ParticleSystem>().Play();
                gameObject.SetActive(false);
                gameController.DestroyEnemyShipByCollision(transform.parent.gameObject);
            }
        }
    }

}
