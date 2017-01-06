using UnityEngine;
using System;

public struct DVector3
{
    public double x;
    public double y;
    public double z;
    public static DVector3 zero = new DVector3(0, 0, 0);

    public DVector3(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public DVector3(double[] r)
    {
        x = r[0];
        y = r[1];
        z = r[2];
    }

    public DVector3(double[,] r, int i)
    {
        x = r[i, 0];
        y = r[i, 1];
        z = r[i, 2];
    }

    public DVector3(Vector3 p, double physToWorldFactor = 1d)
    {
        x = (double)p.x / physToWorldFactor;
        y = (double)p.y / physToWorldFactor;
        z = (double)p.z / physToWorldFactor;
    }

    public Vector3 ToVector3(Vector3 origin, double scale)
    {
        float d_x = (float)(x * scale);
        float d_y = (float)(y * scale);
        float d_z = (float)(z * scale);
        return origin + new Vector3(d_x, d_y, d_z);
    }

    public Vector3 ToVector3()
    {
        return ToVector3(Vector3.zero, 1d);
    }

    public double sqrMagnitude
    {
        get
        {
            return x * x + y * y + z * z;
        }
    }

    public double magnitude
    {
        get
        {
            return Math.Sqrt(sqrMagnitude);
        }
    }

    public DVector3 normalized
    {
        get
        {
            double m = magnitude;
            DVector3 n;
            if (m > 0)
            {
                n = this / m;
            }
            else
            {
                n = DVector3.zero;
            }
            return n;
        }
    }

    public static double Dot(DVector3 a, DVector3 b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public static double Distance(DVector3 a, DVector3 b)
    {
        return Math.Sqrt(Math.Pow((b.x - a.x), 2d) + Math.Pow((b.y - a.y), 2d) + Math.Pow((b.z - a.z), 2d));
    }

    public static DVector3 operator +(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static DVector3 operator -(DVector3 a)
    {
        return new DVector3(-a.x, -a.y, -a.z);
    }
    public static DVector3 operator -(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static DVector3 operator *(double d, DVector3 a)
    {
        return new DVector3(d * a.x, d * a.y, d * a.z);
    }
    public static DVector3 operator *(DVector3 a, double d)
    {
        return new DVector3(d * a.x, d * a.y, d * a.z);
    }
    public static DVector3 operator /(DVector3 a, double d)
    {
        return new DVector3(a.x / d, a.y / d, a.z / d);
    }
    public static bool operator ==(DVector3 lhs, DVector3 rhs)
    {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }
    public static bool operator !=(DVector3 lhs, DVector3 rhs)
    {
        return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
    }

    public static DVector3 operator *(Quaternion quat, DVector3 vec)
    {
        double num = quat.x * 2f;
        double num2 = quat.y * 2f;
        double num3 = quat.z * 2f;
        double num4 = quat.x * num;
        double num5 = quat.y * num2;
        double num6 = quat.z * num3;
        double num7 = quat.x * num2;
        double num8 = quat.x * num3;
        double num9 = quat.y * num3;
        double num10 = quat.w * num;
        double num11 = quat.w * num2;
        double num12 = quat.w * num3;
        DVector3 result;
        result.x = (1f - (num5 + num6)) * vec.x + (num7 - num12) * vec.y + (num8 + num11) * vec.z;
        result.y = (num7 + num12) * vec.x + (1f - (num4 + num6)) * vec.y + (num9 - num10) * vec.z;
        result.z = (num8 - num11) * vec.x + (num9 + num10) * vec.y + (1f - (num4 + num5)) * vec.z;
        return result;
    }

}

