using System;
using UnityEngine;

[Serializable]
public class CubeColliderParam : ColliderParam
{
    public Vector3 position;
    public Vector3 size;
    override public ColliderShape GetShape()
    {
        return ColliderShape.Cube;
    }
    public override Vector3 Position {
        get {
            return position;
        }
        set {
            position = value;
        }
    }
}
