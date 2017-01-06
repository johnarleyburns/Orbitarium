using UnityEngine;

public class DisplayUtils {

    public static string TimeToTargetText(float dist, float relv)
    {
        const float minTimeDist = 10f;
        string timeToTargetText;
        if (relv <= 0 && dist > minTimeDist)
        {
            timeToTargetText = "Inf";
        }
        else
        {
            float sec;
            if (dist <= minTimeDist)
            {
                sec = 0;
            }
            else
            {
                sec = dist / relv;
            }
            timeToTargetText = sec > 604800 ? string.Format("{0:,0} w {1:,0} d", sec / 604800, (sec % 604800) / 86400)
                : (sec > 86400 ? string.Format("{0:,0} d {1:0,0} h", sec / 86400, (sec % 86400) / 3600)
                : (sec > 3600 ? string.Format("{0:,0} h {1:0,0} m", sec / 3600, (sec % 3600) / 60)
                    : (sec > 60 ? string.Format("{0:,0} m {1:0,0} s", sec / 60, sec % 60)
                    : string.Format("{0:,0} s", sec))));
        }
        return timeToTargetText;
    }

    public static string DistanceText(float dist)
    {
        float adist = Mathf.Abs(dist);
        string distText = adist > 100000 ? string.Format("{0:,0} km", dist / 1000f)
            : (adist > 10000 ? string.Format("{0:,0.0} km", dist / 1000f)
            : (adist > 1000 ? string.Format("{0:,0.00} km", dist / 1000f)
                : (adist > 100 ? string.Format("{0:,0} m", dist)
                : string.Format("{0:,0.0} m", dist))));
        return distText;
    }

    public static string RelvText(float relV)
    {
        float arelV = Mathf.Abs(relV);
        string relvText = arelV > 10000 ? string.Format("{0:,0.0} km/s", relV / 1000f)
            : (arelV > 1000 ? string.Format("{0:,0.00} km/s", relV / 1000f)
            : (arelV > 100 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 10 ? string.Format("{0:,0} m/s", relV)
            : (arelV > 1 ? string.Format("{0:,0.0} m/s", relV)
            : string.Format("{0:,0.00} m/s", relV)))));
        return relvText;
    }

    public static string Angle3Text(float x, float y, float z)
    {
        string angleText = string.Format("pitch={0:0} roll={1:0} yaw={2:0}", x, y, z);
        return angleText;
    }

    public static string QText(Quaternion q)
    {
        string text = string.Format("{0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}", q.w, q.x, q.y, q.z);
        return text;
    }

    public static string DegreeText(float deg)
    {
        string text = string.Format("{0:+000°;-000°; 000°}", deg);
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
