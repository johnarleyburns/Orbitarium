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
}
