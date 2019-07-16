using System;
using UnityEngine;


[Serializable]
public class RectColliderParam : ColliderParam
{
    public Rect rect = new Rect(0, 0, 0.3f, 0.3f);

    override public ColliderShape GetShape()
    {
        return ColliderShape.Rect;
    }
    public override Vector3 Position {
        get => rect.position;
        set => rect.position = value;
    }
}