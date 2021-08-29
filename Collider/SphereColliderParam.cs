using System;
using UnityEngine;


[Serializable]
public class SphereColliderParam : ColliderParam
{
    public Vector3 position;
    public float radius = 0.3f;
    override public ColliderShape GetShape()
    {
        return ColliderShape.Sphere;
    }
    public override Vector3 Position {
        get => position;
        set => position = value;
    }
}
