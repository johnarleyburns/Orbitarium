using UnityEngine;

public class DisplayUtils {

    public static string TimeToTargetText(float dist, float relv)
    {
        string timeToTargetText;
        if (relv <= 0)
        {
            timeToTargetText = "Inf";
        }
        else
        {
            float sec = dist / relv;
            timeToTargetText = string.Format("{0:,0} s", sec);
        }
        return timeToTargetText;
    }

    public static string DistanceText(float dist)
    {
        float adist = Mathf.Abs(dist);
        string distText = adist > 100000 ? string.Format("{0:,0} km", dist / 1000)
            : (adist > 10000 ? string.Format("{0:,0.0} km", dist / 1000)
            : (adist > 1000 ? string.Format("{0:,0.00} km", dist / 1000)
                : (adist > 100 ? string.Format("{0:,0} m", dist)
                : string.Format("{0:,0.0} m", dist))));
        return distText;
    }

    public static string RelvText(float relV)
    {
        float arelV = Mathf.Abs(relV);
        string relvText = arelV > 10000 ? string.Format("{0:,0} km/s", relV / 1000)
            : (arelV > 1000 ? string.Format("{0:,0.0} m/s", relV / 1000)
            : (arelV > 100 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 10 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 1 ? string.Format("{0:,0.0} m/s", relV)
            : string.Format("{0:,0.00} m/s", relV)))));
        return relvText;
    }

    public static string Angle3Text(float x, float y, float z)
    {
        string angleText = string.Format("( {0:0}, {1:0}, {2:0} ) deg", x, y, z);
        return angleText;
    }

    public static string QText(Quaternion q)
    {
        string text = string.Format("( {0:0.00}, {1:0.00}, {2:0.00}, {3:0.00} )", q.w, q.x, q.y, q.z);
        return text;
    }

    public static Color ColorValueBetween(float value, float warnThreshold, float badThreshold)
    {
        float v = Mathf.Abs(value);
        Color c;
        if (v >= badThreshold)
        {
            c = MFDController.COLOR_BAD;
        }
        else if (v >= warnThreshold)
        {
            c = MFDController.COLOR_WARN;
        }
        else
        {
            c = MFDController.COLOR_GOOD;
        }
        return c;
    }

}
