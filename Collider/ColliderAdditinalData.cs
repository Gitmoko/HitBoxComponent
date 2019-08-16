using System;
using UnityEngine;


[Serializable]
public abstract class ColliderAdditionalData : ScriptableObject
{

    public abstract void DrawInspectorGUI();

}