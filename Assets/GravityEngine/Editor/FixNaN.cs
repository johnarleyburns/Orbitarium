using UnityEngine;
using System.Collections;

public class FixNaN  {

    public static Vector3 FixIfNaN(Vector3 v)
    {
        if (float.IsNaN(v.x))
        {
            v.x = 0;
        }
        if (float.IsNaN(v.y))
        {
            v.y = 0;
        }
        if (float.IsNaN(v.z))
        {
            v.z = 0;
        }
        return v;
    }

    public static DVector3 FixIfNaN(DVector3 v)
    {
        if (double.IsNaN(v.x))
        {
            v.x = 0;
        }
        if (double.IsNaN(v.y))
        {
            v.y = 0;
        }
        if (double.IsNaN(v.z))
        {
            v.z = 0;
        }
        return v;
    }

    public static float FixIfNaN(float x)
    {
        if (float.IsNaN(x))
        {
            x = 0;
        }
        return x;
    }

    public static double FixIfNaN(double x)
    {
        if (double.IsNaN(x))
        {
            x = 0;
        }
        return x;
    }
}
