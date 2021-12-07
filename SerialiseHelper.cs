using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;

static public class SH
{

    static private string vs = "|+|";
    static private string ps = "|++|";

    static public string Serialise(string value)
    {
        return value;
    }

    static public void UnSerialise(string value, ref String output)
    {
        output = value;
    }

    static public string Serialise(List<int> value)
    {
        List<string> str = new List<string>();
        foreach (int i in value)
            str.Add("" + i);

        return string.Join(':',str);
    }
    static public void UnSerialise(string value, ref List<int> output)
    {
        
        output = new List<int>();
        if (value != "")
        {
            string[] bits = value.Split(':');
            foreach (string s in bits)
                output.Add(int.Parse(s));
        }
    }



    static public string Serialise(string[] strings)
    {
        return string.Join(vs, strings);
    }

    static public void UnSerialise(string value, ref String[] output)
    {
        output = value.Split(vs);
    }

    static public string Serialise(int value)
    {
        return "" + value;
    }

    static public void UnSerialise(string value, ref int output)
    {
        output = int.Parse(value);
    }

    static public string Serialise(float value)
    {
        return "" + value;
    }

    static public void UnSerialise(string value, ref float output)
    {
        output = float.Parse(value);
    }

    static public string Serialise(bool value) {
        return "" + value;
    }

    static public void UnSerialise(string value, ref bool output)
    {
        output = value.ToLower() == "true";
    }

    static public string Serialise(Pose value)
    {
        return Serialise(value.position) + ps + Serialise(value.orientation);
    }

    static public void UnSerialise(string value, ref Pose output)
    {
        string[] bits = value.Split(ps);
        Vec3 v = new Vec3();
        UnSerialise(bits[0], ref v);
        Quat q = new Quat();
        UnSerialise(bits[1], ref q);

        output = new Pose(v, q);
    }

    static public string Serialise(Vec3 value)
    {
        return value.x + vs + value.y + vs + value.z;
    }

    static public void UnSerialise(string value, ref Vec3 output)
    {
        string[] bits = value.Split(vs);
        output = new Vec3(float.Parse(bits[0]), float.Parse(bits[1]), float.Parse(bits[2]));
    }

    static public string Serialise(Quat value)
    {
        return value.x + vs + value.y + vs + value.z + vs + value.w;
    }

    static public void UnSerialise(string value, ref Quat output)
    {
        string[] bits = value.Split(vs);
        output = new Quat(float.Parse(bits[0]), float.Parse(bits[1]), float.Parse(bits[2]), float.Parse(bits[3]));
    }

    static public string Serialise(Color value)
    {
        return value.r + vs + value.g + vs + value.b + vs + value.a;
    }

    static public void UnSerialise(string value, ref Color output)
    {
        string[] bits = value.Split(vs);
        output = new Color(float.Parse(bits[0]), float.Parse(bits[1]), float.Parse(bits[2]), float.Parse(bits[3]));
    }
}
