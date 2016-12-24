using UnityEngine;
using System.Collections;

public class MissileShip : MonoBehaviour {

    public GameController gameController;
    public GameObject ShipExplosion;
    public float MissileEjectV = 5f;
    
    private RocketShip ship;
    private Autopilot autopilot;
    private Collider capsuleCollider;
    private GameObject target;
    private Autopilot.Command currentCommand = Autopilot.Command.OFF;
    private Autopilot.Command currentGoalCommand = Autopilot.Command.OFF;

    void Start()
    {
        ship = GetComponent<RocketShip>();
        autopilot = GetComponent<Autopilot>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        UpdateGoal();
    }

    public void SetTarget(GameObject t)
    {
        target = t;
    }

    public bool Fire()
    {
        bool success = false;
        if (target != null)
        {
            StartCoroutine(FireMissileCo());
            success = true;
        }
        return success;
    }

    private IEnumerator FireMissileCo()
    {
        GameObject nBody = PhysicsUtils.GetNBodyGameObject(gameObject);
        GameObject parent = nBody.transform.parent.gameObject;
        nBody.transform.parent = null;
        GravityEngine.instance.SetVelocity(nBody, GravityEngine.instance.GetVelocity(PhysicsUtils.GetNBodyGameObject(parent)));
        GravityEngine.instance.SetPosition(nBody, nBody.transform.position);
        GravityEngine.instance.ActivateBody(nBody);
        GravityEngine.instance.ApplyImpulse(nBody.GetComponent<NBody>(), MissileEjectV * nBody.transform.forward);
        yield return new WaitForSeconds(3);
        capsuleCollider.enabled = true;
        currentGoalCommand = Autopilot.Command.INTERCEPT;
        yield break;
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
            }
            else
            {
                ShipExplosion.GetComponent<ParticleSystem>().Play();
                gameObject.SetActive(false);
                gameController.DestroyEnemyShipByCollision(transform.parent.gameObject);
            }
        }
    }

    private void UpdateGoal()
    {
        if (currentCommand != currentGoalCommand)
        {
            currentCommand = currentGoalCommand;
            autopilot.ExecuteCommand(currentCommand, target);
        }
    }
}
