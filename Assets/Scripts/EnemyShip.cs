using UnityEngine;
using System.Collections;

public class EnemyShip : MonoBehaviour, IControllableShip {

    public GameController gameController;
    public string VisibleName;
    public float healthMax = 1;
    public float minRelVtoDamage = 5;
    public GameObject ShipExplosion;

    private float health;
    private RocketShip ship;
    private Autopilot autopilot;

    void Start()
    {
        health = healthMax;
        ship = GetComponent<RocketShip>();
        autopilot = GetComponent<Autopilot>();
    }

    void Update()
    {
        UpdateGoal();
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            GameObject otherBody = collider.attachedRigidbody.gameObject;
            float relVel;
            if (!PhysicsUtils.Fused(otherBody) && PhysicsUtils.ShouldBounce(gameObject, otherBody, out relVel))
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

    private Autopilot.Command currentGoalCommand = Autopilot.Command.OFF;

    private void UpdateGoal()
    {
        if (currentGoalCommand == Autopilot.Command.OFF)
        {
            currentGoalCommand = Autopilot.Command.STRAFE;
            //currentGoalCommand = Autopilot.Command.ACTIVE_TRACK;
            GameObject target = gameController.GetPlayer();
            autopilot.ExecuteCommand(currentGoalCommand, target);
        }
    }

}
