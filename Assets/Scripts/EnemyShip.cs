using UnityEngine;
using System.Collections;

public class EnemyShip : MonoBehaviour, IControllableShip {

    public GameController gameController;
    public NBodyDimensions NBodyDimensions;
    public string VisibleName;
    public float healthMax = 1;
    public float minRelVtoDamage = 5;
    public GameObject ShipExplosion;
    public Autopilot.Command InitialGoal = Autopilot.Command.OFF;

    private float health;
    private RocketShip ship;
    private Autopilot autopilot;
    private Autopilot.Command currentGoalCommand = Autopilot.Command.OFF;

    void Start()
    {
    }

    public void StartShip()
    {
        health = healthMax;
        ship = GetComponent<RocketShip>();
        autopilot = GetComponent<Autopilot>();
        GetComponent<ShipWeapons>().AddMissiles();
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
                Bounce(otherBody, relVel);
            }
            else
            {
                ExplodeCollide(otherBody);
            }
        }
    }

    public void Bounce(GameObject otherBody, float relVel)
    {
        Vector3 relV = PhysicsUtils.CalcRelV(transform, otherBody);
        ship.ApplyImpulse(-relV.normalized, relVel, 1);
        if (relVel >= minRelVtoDamage)
        {
            health--;
        }
    }

    public void ExplodeCollide(GameObject otherBody)
    {
        health = 0; // boom
        ShipExplosion.GetComponent<ParticleSystem>().Play();
        gameObject.SetActive(false);
        gameController.DestroyEnemyShipByCollision(transform.parent.gameObject);
    }

    public void Dock(GameObject dockModel, bool withSound = true)
    {
        Debug.LogWarning("EnemyShip Dock not implemented");
    }

    private void UpdateGoal()
    {
        if (currentGoalCommand == Autopilot.Command.OFF && currentGoalCommand != InitialGoal)
        {
            currentGoalCommand = InitialGoal;
            if (gameController != null && gameController.GetPlayer() != null)
            {
                GameObject target = gameController.GetPlayer();
                autopilot.ExecuteCommand(currentGoalCommand, target);
            }
        }
    }

}
