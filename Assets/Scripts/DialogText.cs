using UnityEngine;
using System.Collections.Generic;

public class DialogText : MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    // MFDController
    public static string Translation = "translation.";
    public static string Rotation = "rotation.";
    public static string LowFuel = "low fuel.";
    public static string MainEngineOn = "engine on.";
    public static string MainEngineOff = "engine off.";
    public static string AuxEngineOn = "aux on.";
    public static string AuxEngineOff = "aux off";
    public static string RCSFineControlOn = "fine on.";
    public static string RCSFineControlOff = "fine off.";
    public static string AutopilotOff = "Autopilot off";
    public static string Strafe = "streif";

    // MFD Weapons
    public static string WeaponsArmed = "Weapons armed.";
    public static string WeaponsOffline = "Weapons offline.";
    public static string SelectTargetToFire = "Select target to fire.";
    public static string NoMissilesRemaining = "No missiles remaining.";
    public static string FireMissile = "Missile fired!";
    public static string WeaponsMalfunction = "Weapons malfunction!";
    public static string NoAmmoRemaining = "Out of ammo.";
    public static string GunsTooHot = "Wait for cooldown!";

    // GameController
    public static string ReadyPlayerOne = "ready player one";

    // User Visible Names
    public static Dictionary<string, string> visibleName = new Dictionary<string, string>()
    {
        { "nBodyBulletModel", "Bullet" },
        { "AgenaModel", "Missile" },
        { "SoyuzModel", "Soyuz" }
    };

    public static string CombineImperative(string verb, string obj)
    {
        return string.Format("{0} {1}", verb, obj);
    }

    public static string VisibleName(string name)
    {
        string vName;
        if (!visibleName.TryGetValue(name, out vName))
        {
            vName = name;
        }
        return vName;
    }

}
