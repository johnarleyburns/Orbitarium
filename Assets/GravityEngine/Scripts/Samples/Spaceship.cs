using UnityEngine;
using UnityEngine.UI;
//using System.Collections;

/// <summary>
/// Spaceship.
/// A simple example of an object that is subject to the gravitational field and makes changes in 
/// own motion based on user input. 
///
/// Controls:
///   Arrow Keys: spacecraft pitch and yaw
///   Space: Pause/Resume
///   F: Fire engine
///
/// When paused the -/= keys can be used to set a course correction that will be applied when resume is pressed.
///
/// This script is to be attached to a model that is the child of an NBody object. 
/// The GravityEngine will perform the physical updates to position and velocity. 
///
/// Changes to the spaceship motion are via impulse changes to the velocity. 
///
/// </summary>

public class Spaceship : MonoBehaviour {

    //! Thrust scale 
    public float thrustPerKeypress = 0.5f;
    public float mainEngineThrustPerKeypress = 10f;
	//! Rate of spin when arrow keys pressed (degress/update cycle)
	public float spinDegreesPerKeypress = 0.01f;
    //! Forward direction of the ship model. Thrust us applies in opposite direction to this vector.
    public ToggleButton RotateButton;
    public ToggleButton TranslateButton;
    public GameObject Target;
    public GameObject HUD;
    public GameObject RelativeVelocityDirectionIndicator;
    public GameObject RelativeVelocityAntiDirectionIndicator;

    private NBody nbody; 
    private enum RCSMode { Rotate, Translate };
    private RCSMode currentRCSMode;
    private Vector3 currentSpin;
    private bool killingRot;

    //private Vector3 coneScale; // nitial scale of thrust cone


    // Use this for initialization
    void Start() {
        if (transform.parent == null) {
            Debug.LogError("Spaceship script must be applied to a model that is a child of an NBody object.");
            return;
        }
        nbody = transform.parent.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogError("Parent must have an NBody script attached.");
        }
		//running = false;
		//ravityEngine.instance.SetEvolve(running);
		GravityEngine.instance.Setup();
        currentRCSMode = RCSMode.Translate;
        UpdateRCSMode();
        //coneScale = thrustCone.transform.localScale;
	}

    public void SetRCSModeRotate()
    {
        currentRCSMode = RCSMode.Rotate;
        UpdateRCSMode();
    }

    public void SetRCSModeTranslate()
    {
        currentRCSMode = RCSMode.Translate;
        UpdateRCSMode();
    }

    void UpdateRCSMode()
    {
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                RotateButton.isToggled = true;
                TranslateButton.isToggled = false;
                break;
            case RCSMode.Translate:
            default:
                RotateButton.isToggled = false;
                TranslateButton.isToggled = true;
                break;
        }
    }

    void ToggleRCSMode()
    {
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                currentRCSMode = RCSMode.Translate;
                break;
            case RCSMode.Translate:
            default:
                currentRCSMode = RCSMode.Rotate;
                break;
        }
        UpdateRCSMode();
    }

    void ApplyImpulse(Vector3 normalizedDirection, float thrustPer = -1)
    {
        if (thrustPer == -1)
        {
            thrustPer = thrustPerKeypress;
        }
        Vector3 thrust = normalizedDirection * thrustPer * Time.deltaTime;
        //thrust = transform.rotation * thrust * Time.deltaTime;
        GravityEngine.instance.ApplyImpulse(nbody, thrust);
    }

    // Update is called once per frame
    void Update()
    {
        if (nbody == null)
        {
            return; // misconfigured
        }
        if (Input.GetKeyDown(KeyCode.KeypadDivide))
        {
            ToggleRCSMode();
        }
        switch (currentRCSMode)
        {
            case RCSMode.Rotate:
                UpdateInputRotation();
                break;
            case RCSMode.Translate:
            default:
                UpdateInputTranslation();
                break;
        }
        if (Input.GetKey(KeyCode.KeypadPlus))
        {
            ApplyImpulse(transform.forward, mainEngineThrustPerKeypress);
        }
        ApplyCurrentRotation();
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (HUD != null && Target != null && Target.GetComponent<NBody>() != null)
        {
            float targetDistance = (Target.transform.position - transform.parent.transform.position).magnitude;
            Vector3 myVel = GravityEngine.instance.GetVelocity(transform.parent.gameObject);
            Vector3 targetVel = GravityEngine.instance.GetVelocity(Target);
            Vector3 relVel = myVel - targetVel;
            relVel.
            float relVelScalar = relVel.magnitude;
            //float RelativeVelocityIndicatorScale = 5;
            Vector3 relVelUnit = relVel.normalized;
            //Vector3 relVelScaled = RelativeVelocityIndicatorScale * relVelUnit;

            RelativeVelocityDirectionIndicator.transform.position = transform.position + relVel; ;
            RelativeVelocityAntiDirectionIndicator.transform.position = transform.position + -relVel;
            Greyman.OffScreenIndicator offScreenIndicator = HUD.GetComponent<Greyman.OffScreenIndicator>();
            if (offScreenIndicator.indicators[0].hasOnScreenText)
            {
                string targetString = string.Format("Asteroid\nDist: {0:0,0} m\nRelV: {1:0,0.0} m/s", targetDistance, relVelScalar);
                offScreenIndicator.UpdateIndicatorText(0, targetString);
            }
        }
    }

    void UpdateInputRotation()
    {
        if (Input.GetKey(KeyCode.Keypad2))
        {
            killingRot = false;
            currentSpin.x -= spinDegreesPerKeypress;
        }
        if (Input.GetKey(KeyCode.Keypad8))
        {
            killingRot = false;
            currentSpin.x += spinDegreesPerKeypress;
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            killingRot = false;
            currentSpin.y -= spinDegreesPerKeypress;
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            killingRot = false;
            currentSpin.y += spinDegreesPerKeypress;
        }
        if (Input.GetKey(KeyCode.Keypad6))
        {
            killingRot = false;
            currentSpin.z -= spinDegreesPerKeypress;
        }
        if (Input.GetKey(KeyCode.Keypad4))
        {
            killingRot = false;
            currentSpin.z += spinDegreesPerKeypress;
        }
        if (Input.GetKeyDown(KeyCode.Keypad5)) // kill rot
        {
            killingRot = !killingRot;
        }
    }

    void ApplyCurrentRotation()
    {
        if (killingRot)
        {
            Vector3 neededOffset = Vector3.zero - currentSpin;
            Vector3 allowedOffset = new Vector3(
                Mathf.Clamp(neededOffset.x, -spinDegreesPerKeypress, spinDegreesPerKeypress),
                Mathf.Clamp(neededOffset.y, -spinDegreesPerKeypress, spinDegreesPerKeypress),
                Mathf.Clamp(neededOffset.z, -spinDegreesPerKeypress, spinDegreesPerKeypress)
            );
            currentSpin += allowedOffset;
            if (currentSpin == Vector3.zero)
            {
                killingRot = false;
            }
        }
        transform.Rotate(
            currentSpin.x * Time.deltaTime,
            currentSpin.y * Time.deltaTime,
            currentSpin.z * Time.deltaTime,
            Space.Self);
    }

    void UpdateInputTranslation()
    {
        if (Input.GetKey(KeyCode.Keypad6))
        {
            ApplyImpulse(transform.forward);
        }
        if (Input.GetKey(KeyCode.Keypad9))
        {
            ApplyImpulse(-transform.forward);
        }
        if (Input.GetKey(KeyCode.Keypad2))
        {
            ApplyImpulse(transform.up);
        }
        if (Input.GetKey(KeyCode.Keypad8))
        {
            ApplyImpulse(-transform.up);
        }
        if (Input.GetKey(KeyCode.Keypad1))
        {
            ApplyImpulse(-transform.right);
        }
        if (Input.GetKey(KeyCode.Keypad3))
        {
            ApplyImpulse(transform.right);
        }
    }
        /*
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            running = !running;
            if (running && thrustSize > 0)
            {
                Vector3 thrust = thrustSize * axisForeAft * Time.deltaTime;
                //thrust = transform.rotation * thrust;
                GravityEngine.instance.ApplyImpulse(nbody, thrust);
                // reset thrust
                thrustSize = 0f;
                //SetThrustCone(thrustSize);
            }
            GravityEngine.instance.SetEvolve(running);
        }
        if (!running)
        {
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                thrustSize -= thrustPerKeypress;
                if (thrustSize < 0)
                    thrustSize = 0f;
            }
            else if (Input.GetKeyDown(KeyCode.Equals))
            {
                thrustSize += thrustPerKeypress;
            }
            //SetThrustCone(thrustSize);
            // In order for change in orbit to be see by the predictor, need to 
            // determine the new NBody velocity - but not set it (or all updates will
            // be cumulative). This way can see impact of rotating with a specific thrust setting.
            Vector3 thrust = thrustSize * axisForeAft * Time.deltaTime;
//            thrust = transform.rotation * thrust;
            nbody.vel = GravityEngine.instance.VelocityForImpulse(nbody, thrust);
        }
        */
    /*
    if (Input.GetKeyDown(KeyCode.Keypad)) {
        running = !running;
        if (running && thrustSize > 0) {
            thrust = thrustSize * axisForeAft;
            thrust = transform.rotation * thrust;
            GravityEngine.instance.ApplyImpulse(nbody, thrust);
            // reset thrust
            thrustSize = 0f;
            SetThrustCone(thrustSize);
        }
        GravityEngine.instance.SetEvolve(running);

    } else if (Input.GetKey(KeyCode.Keypad5)) {
        thrust = thrustPerKeypress * axisForeAft;
        thrust = transform.rotation * thrust;
        GravityEngine.instance.ApplyImpulse(nbody, thrust);
    }
    else if (Input.GetKey(KeyCode.Keypad1))
    {
        thrust = angularThrustPerKeypress * -axisPortStarboard * 10 * nbody.mass;
        GetComponent<Rigidbody>().AddTorque(thrust, ForceMode.Force);    
} else {
        Quaternion rotation = Quaternion.identity;
        if (Input.GetKey(KeyCode.Keypad1))
        {
            rotation = Quaternion.AngleAxis(spinRate, Vector3.forward);
        }
        else if (Input.GetKey(KeyCode.Keypad3))
        {
            rotation = Quaternion.AngleAxis(-spinRate, Vector3.forward);
        }
        else if (Input.GetKey(KeyCode.Keypad2))
        {
            rotation = Quaternion.AngleAxis(spinRate, Vector3.right);
        }
        else if (Input.GetKey(KeyCode.Keypad8))
        {
            rotation = Quaternion.AngleAxis(-spinRate, Vector3.right);
        }
        else if (Input.GetKey(KeyCode.Keypad4))
        {
            rotation = Quaternion.AngleAxis(-spinRate, Vector3.up);
        }
        else if (Input.GetKey(KeyCode.Keypad6))
        {
            rotation = Quaternion.AngleAxis(spinRate, Vector3.up);
        }
        transform.rotation *= rotation;
        */
    // }
    /*
		// When paused check for course correction
		if (!running) {
			if (Input.GetKeyDown(KeyCode.Minus)) {
				thrustSize -= thrustPerKeypress; 
				if (thrustSize < 0)
					thrustSize = 0f; 
			} else 	if (Input.GetKeyDown(KeyCode.Equals)) {
				thrustSize += thrustPerKeypress; 
			}
			SetThrustCone(thrustSize);
			// In order for change in orbit to be see by the predictor, need to 
			// determine the new NBody velocity - but not set it (or all updates will
			// be cumulative). This way can see impact of rotating with a specific thrust setting.
			thrust = thrustSize * axisForeAft;
			thrust = transform.rotation * thrust;
			nbody.vel = GravityEngine.instance.VelocityForImpulse(nbody, thrust);
		}
        */
    //}

        /*
    private const float thrustScale = 3f; // adjust for desired visual sensitivity

	private void SetThrustCone(float size) {
		Vector3 newConeScale = coneScale;
		newConeScale.z = coneScale.z * size * thrustScale;
		thrustCone.transform.localScale = newConeScale;
		// as cone grows, need to offset
		Vector3 localPos = thrustCone.transform.localPosition;
		// move cone center along spacecraft axis as cone grows
		localPos = -(size*thrustScale)/2f*axisForeAft;
		thrustCone.transform.localPosition = localPos;
	}
    */
}
