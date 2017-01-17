using UnityEngine;
using System.Collections;

public class MissileShip : MonoBehaviour, IControllableShip
{

    public GameController gameController;
    public GameObject NBodyMissilePrefab;
    public GameObject ShipExplosion;
    public float MissileEjectV = 10f;
    public float MissileClearShipSec = 1f;
    public float MissileArmSec = 2f;

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
        GameObject attachmentSlot = transform.parent.gameObject;
        GameObject nearShipModel = attachmentSlot.transform.GetComponentInParent<RocketShip>().gameObject;
        NBodyDimensions shipDim = NUtils.GetNBodyDimensions(attachmentSlot.transform.parent.gameObject);
        Vector3 attachmentCenter = attachmentSlot.transform.position;
        Vector3 nearShipCenter = nearShipModel.transform.position;
        Vector3 nearShipAttachVec = attachmentCenter - nearShipCenter;
        DVector3 farShipAttachVec = new DVector3(nearShipAttachVec) / shipDim.NBodyToModelScaleFactor; 
        //Vector3 awayFromShip = shipDim.transform.GetChild(0).transform.forward;

        GameObject shipNBody = shipDim.NBody;
        DVector3 shipNBodyPos;
        GravityEngine.instance.GetPosition(shipNBody.GetComponent<NBody>(), out shipNBodyPos);
        DVector3 farAttachmentPos = shipNBodyPos + farShipAttachVec;
//        DVector3 farAttachmentPos = shipNBodyPos + new DVector3(awayFromShip) / shipDim.NBodyToModelScaleFactor;

        GameObject nBody = Instantiate(NBodyMissilePrefab, farAttachmentPos.ToVector3(), Quaternion.identity) as GameObject;

        NBodyDimensions missileDim = NUtils.GetNBodyDimensions(gameObject);
        GameObject playerNBody = NUtils.GetNBodyGameObject(gameController.GetPlayer());
        missileDim.transform.parent = null;
        missileDim.NBody = nBody;
        missileDim.PlayerNBody = playerNBody;
        nBody.transform.GetChild(0).GetComponent<NBodyCollision>().Propogate = true;
        nBody.transform.GetChild(0).GetComponent<NBodyCollision>().PropogateModel = gameObject;
        nBody.name = string.Format("NBody {0}", name);
        DVector3 shipVel = GravityEngine.instance.GetVelocity(shipNBody);
        DVector3 missileVel = shipVel; // + new DVector3(MissileEjectV * awayFromShip) / shipDim.NBodyToModelScaleFactor;

        GravityEngine.instance.AddBody(nBody);
        GravityEngine.instance.UpdatePositionAndVelocity(nBody.GetComponent<NBody>(), farAttachmentPos, missileVel);
        ship.NBodyDimensions = missileDim;
        ship.ApplyImpulse(transform.forward, MissileEjectV, 1);
        currentGoalCommand = Autopilot.Command.OFF;
        yield return new WaitForSeconds(MissileClearShipSec);
        currentGoalCommand = Autopilot.Command.INTERCEPT;
        yield return new WaitForSeconds(MissileClearShipSec - MissileArmSec);
        nBody.transform.GetChild(0).GetComponent<SphereCollider>().enabled = true;
        yield break;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (gameController != null)
        {
            ExplodeCollide(null);
        }
    }

    public void Bounce(GameObject otherBody, float relVel)
    {
        ExplodeCollide(otherBody);
    }

    public void ExplodeCollide(GameObject otherBody)
    {
        ShipExplosion.GetComponent<ParticleSystem>().Play();
        if (otherBody == null) // self destruct
        {
            StartCoroutine(SelfDestructCo());
        }
        else
        {
            Destruct();
        }
    }

    private IEnumerator SelfDestructCo()
    {
        yield return new WaitForSeconds(3);
        Destruct();
    }

    private void Destruct()
    {
        NBodyDimensions dim = NUtils.GetNBodyDimensions(gameObject);
        gameController.DestroyMissileByCollision(dim.NBody);
        gameController.DestroyMissileByCollision(transform.parent.gameObject);
    }

    public void Dock(GameObject dockModel, bool withSound = true)
    {
        Debug.LogWarning("Dock MissileShip not implemented");
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
