using System;
using UnityEngine;


[Serializable]
public class ColliderParam : ScriptableObject
{
    public ColliderAdditionalData additionalData = null;

    virtual public ColliderShape GetShape()
    {
        Debug.LogError("invalid collider param");
        return ColliderShape.None;
    }

    virtual public Vector3 Position {
        get;
        set;
    }

    public static ColliderParam ColliderParamFactory(ColliderShape shape)
    {
        ColliderParam ret = null;
        if (shape == ColliderShape.Rect)
        {
            ret = CreateInstance<RectColliderParam>();
        }
        else if (shape == ColliderShape.Sphere)
        {
            ret = CreateInstance<SphereColliderParam>();
        }
        else if (shape == ColliderShape.Capsule)
        {
            ret = CreateInstance<CapsuleColliderParam>();
        }
        else
        {
            Debug.LogError("not implemented");
        }
        return ret;

    }
}