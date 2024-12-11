using UnityEngine;

public static class Vector2Extensions
{
    public static Vector3 XZ(this Vector2 vector)
    {
        return new Vector3(vector.x, 0f, vector.y);
    }

    public static Vector3 ToVector3(this Vector2 vector)
    {
        return new Vector3(vector.x, 0.0f, vector.y);
    }
}
