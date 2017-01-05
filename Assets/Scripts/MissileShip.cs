using UnityEngine;
using System.Collections;

public class MissileShip : MonoBehaviour
{

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
        GameObject nBody = NUtils.GetNBodyGameObject(gameObject);
        GameObject parent = nBody.transform.parent.gameObject;
        nBody.transform.parent = null;
        Vector3 pos = nBody.transform.position;
        Vector3 vel = GravityEngine.instance.GetVelocity(NUtils.GetNBodyGameObject(parent)).ToVector3();
        GravityEngine.instance.UpdatePositionAndVelocity(nBody.GetComponent<NBody>(), pos, vel);
        GravityEngine.instance.ActivateBody(nBody);
        GravityEngine.instance.ApplyImpulse(nBody.GetComponent<NBody>(), MissileEjectV * nBody.transform.forward);
        yield return new WaitForSeconds(1);
        capsuleCollider.enabled = true;
        currentGoalCommand = Autopilot.Command.INTERCEPT;
        yield break;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            ShipExplosion.GetComponent<ParticleSystem>().Play();
            gameObject.SetActive(false);
            gameController.DestroyMissileByCollision(transform.parent.gameObject);
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
