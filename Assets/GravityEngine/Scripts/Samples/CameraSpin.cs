using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to roate camera boom using W-A-S-D keys.
///
/// Assumes the Main Camara is a child of the object holding this script with a local position offset
/// (the boom length) and oriented to point at this object. Then pressing the keys will spin the camera
/// around the object this script is attached to.
/// </summary>
public class CameraSpin : MonoBehaviour {

    public bool autoSpin = true;
    //! Rate of spin (degrees per Update)
    public float speedMod = 10.0f;//a speed modifier
    public float spinRate = 1f;
    public GameObject target;
    
    private Vector3 point = Vector3.zero;//the coord to the point where the camera looks at

    // Use this for initialization
    void Start () {
    }

    public void UpdatePos()
    {
        point = target.transform.position;//get target's coords
        transform.position = point + new Vector3(5, 5, -40);
        transform.LookAt(point);//makes the camera look to it
    }

    // Update is called once per frame
    void Update() {
        if (autoSpin)
        {
            //point = target.transform.position;//get target's coords
            //transform.position = point + new Vector3(5, 5, -20);
            transform.LookAt(point);//makes the camera look to it
            transform.RotateAround(point, new Vector3(0.0f, 1.0f, 0.0f), Time.deltaTime * speedMod);
        }
        else
        {
            if (Input.GetKey(KeyCode.W))
            {
                transform.rotation *= Quaternion.AngleAxis(spinRate, Vector3.right);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                transform.rotation *= Quaternion.AngleAxis(-spinRate, Vector3.right);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                transform.rotation *= Quaternion.AngleAxis(spinRate, Vector3.up);
            }
            else if (Input.GetKey(KeyCode.A))
            {
                transform.rotation *= Quaternion.AngleAxis(-spinRate, Vector3.up);
            }
        }
    }
}
