using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector2 ToVector2XZ(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    public static Vector3 Centroid(this IEnumerable<Vector3> vector3s)
    {
        //Calculate and return the centroid of the points
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 vector3 in vector3s)
        {
            centroid += vector3;
        }
        return centroid / vector3s.Count();
    }

    public static Vector3Int ToVector3Int(this Vector3 vector)
    {
        return new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z);
    }
}
