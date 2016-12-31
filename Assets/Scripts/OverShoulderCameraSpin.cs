using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to roate camera boom using Arrow keys for rotation and < > keys for zoom.
///
/// Assumes the Main Camara is a child of the object holding this script with a local position offset
/// (the boom length) and oriented to point at this object. Then pressing the keys will spin the camera
/// around the object this script is attached to.
/// </summary>
public class OverShoulderCameraSpin : MonoBehaviour
{

    //! Rate of spin (degrees per Update)
    private float spinRateDegPerSec = 20f;
    public float zoomSize = 1f;

    //private Vector3 initialBoom;
    // factor by which zoom is changed 
    //private float zoomStep = 0.02f;
    private Camera boomCamera;
    private GameObject target;
    private Vector3 point;
    
    // Use this for initialization
    void Start()
    {
        boomCamera = GetComponentInChildren<Camera>();
        //if (boomCamera != null)
        //{
        //    initialBoom = boomCamera.transform.localPosition;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (boomCamera.enabled)
        {
            transform.LookAt(point);//makes the camera look to it
            transform.RotateAround(point, new Vector3(0.0f, 1.0f, 0.0f), Time.deltaTime * spinRateDegPerSec);
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
        point = target.transform.position;//get target's coords
        transform.position = point + new Vector3(5, 5, -40);
        transform.LookAt(point);//makes the camera look to it
    }

}
