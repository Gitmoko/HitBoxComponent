using System;
using UnityEngine;

[Serializable]
public class CapsuleColliderParam : ColliderParam
{
    public Vector3 start = new Vector3(0, 0, 0);
    public Vector3 end = new Vector3(0.3f, 0, 0);
    public float radius = 0.1f;
    override public ColliderShape GetShape()
    {
        return ColliderShape.Capsule;
    }
    public override Vector3 Position {
        get {
            return start;
        }
        set {

            var dist = value - start;
            start = value;
            end += dist;

        }
    }

}

