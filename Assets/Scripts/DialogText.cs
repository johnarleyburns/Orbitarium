using UnityEngine;
using System.Collections;

public class DialogText : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    // MFDController
    public static string Translation = "translation";
    public static string Rotation = "rotation";
    public static string LowFuel = "low fuel";
    public static string MainEngineOn = "main engine on";
    public static string MainEngineOff = "main engine off";
    public static string AuxEngineOn = "auxiliary engine on";
    public static string AuxEngineOff = "auxiliary engine off";
    public static string RCSFineControlOn = "RCS fine control on";
    public static string RCSFineControlOff = "RCS fine control off";
    public static string AutopilotOff = "Autopilot off";
    public static string Strafe = "streif";

    // GameController
    public static string ReadyPlayerOne = "ready player one";

    public static string CombineImperative(string verb, string obj)
    {
        return string.Format("{0} {1}", verb, obj);
    }
}
