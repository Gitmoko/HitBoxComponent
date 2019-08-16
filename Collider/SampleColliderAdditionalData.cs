#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SampleColliderAdditionalData : ColliderAdditionalData
{
    public string teststr = "test";
    public int testamount = 10;

    public override void DrawInspectorGUI()
    {
#if UNITY_EDITOR
        var so = new SerializedObject(this);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("teststr"));
        EditorGUILayout.PropertyField(so.FindProperty("testamount"));
        so.ApplyModifiedProperties();
#endif
    }

}
