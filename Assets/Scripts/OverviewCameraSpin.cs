using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to roate camera boom using Arrow keys for rotation and < > keys for zoom.
///
/// Assumes the Main Camara is a child of the object holding this script with a local position offset
/// (the boom length) and oriented to point at this object. Then pressing the keys will spin the camera
/// around the object this script is attached to.
/// </summary>
public class OverviewCameraSpin : MonoBehaviour
{

    //! Rate of spin (degrees per Update)
    public float spinRateDegPerSec = 10f;
    public float zoomSize = 1f;
    public bool isFar;

    //private Vector3 initialBoom;
    // factor by which zoom is changed 
    //private float zoomStep = 0.02f;
    private Camera boomCamera;
    private GameObject target;
    private Vector3 boomOffset = new Vector3(5, 5, -40);
    private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
    private GameObject looker;

    // Use this for initialization
    void Start()
    {
        boomCamera = GetComponentInChildren<Camera>();
        looker = new GameObject();
        looker.name = "Overview Camera Looker";
    }

    // Update is called once per frame
    void Update()
    {
        if (boomCamera.enabled)
        {
            if (isFar)
            {
                Vector3 nearPos = target.transform.position;
                Vector3 farPos = NUtils.GetNBodyGameObject(target).transform.position;
                looker.transform.LookAt(nearPos);
                looker.transform.RotateAround(nearPos, rotationAxis, Time.deltaTime * spinRateDegPerSec);
                transform.position = farPos;
                transform.rotation = looker.transform.rotation;
            }
            else
            {
                Vector3 nearPos = target.transform.position;
                transform.LookAt(nearPos);
                transform.RotateAround(nearPos, rotationAxis, Time.deltaTime * spinRateDegPerSec);
            }
        }
        /*
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.rotation *= Quaternion.AngleAxis(spinRate, Vector3.right);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.rotation *= Quaternion.AngleAxis(-spinRate, Vector3.right);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.rotation *= Quaternion.AngleAxis(spinRate, Vector3.up);
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.rotation *= Quaternion.AngleAxis(-spinRate, Vector3.up);
        }
        */
        /*
        else if (Input.GetKey(KeyCode.Comma))
        {
            // change boom length
            zoomSize += zoomStep;
            boomCamera.transform.localPosition = zoomSize * initialBoom;
        }
        else if (Input.GetKey(KeyCode.Period))
        {
            // change boom lenght
            // change boom length
            zoomSize -= zoomStep;
            if (zoomSize < 0.1f)
                zoomSize = 0.1f;
            boomCamera.transform.localPosition = zoomSize * initialBoom;
        }
        */
    }

    public void UpdateTarget(GameObject newTarget)
    {
        target = newTarget;
        if (isFar)
        {
            Vector3 nearPos = target.transform.position;
            Vector3 farPos = NUtils.GetNBodyGameObject(target).transform.position;
            looker.transform.position = nearPos + boomOffset;
            looker.transform.LookAt(nearPos);
            transform.position = farPos;
            transform.rotation = looker.transform.rotation;
        }
        else
        {
            Vector3 nearPos = target.transform.position;
            transform.position = nearPos + boomOffset;
            transform.LookAt(nearPos);
        }
    }

}
