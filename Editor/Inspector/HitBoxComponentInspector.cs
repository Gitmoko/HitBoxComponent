using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public enum TraceAnimMode
{
    Off,
    AnimationWindow,
    Scene
}

[CustomEditor(typeof(HitBoxComponent))]
public class HitBoxComponentInspector : Editor
{

    private HitBoxComponent targetComponents;
    private TraceAnimMode traceAnimMode = TraceAnimMode.Off;
    private string addScriptableObjectName = "";

    private static GUIStyle hitboxBackgroundStyle;

    public override bool RequiresConstantRepaint()
    {
        return true;
    }

    #region Drawing Methods
    public override void OnInspectorGUI()
    {


        serializedObject.Update();

        GUILayout.BeginVertical();
        EditorGUI.BeginChangeCheck();
        GUI.skin.label.wordWrap = true;


        var simpleAnimationProp = serializedObject.FindProperty("simpleAnimation");
        EditorGUILayout.PropertyField(simpleAnimationProp, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("New HitBox Name:");
        addScriptableObjectName = GUILayout.TextField(addScriptableObjectName);
        GUILayout.EndHorizontal();
        if (addScriptableObjectName != "" && GUILayout.Button("Create And Add"))
        {
            if (addScriptableObjectName == "")
            {
                Debug.Log("Don't set empty name");
            }
            else
            {
                var newhitbox = CreateInstance<HitBoxes>();
                targetComponents.hitboxes = newhitbox;
                var path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + addScriptableObjectName + ".asset");
                AssetDatabase.CreateAsset(newhitbox, path);
                addScriptableObjectName = "";
                targetComponents.SyncKeyFramesWithSimpleAnimator();
            }
        };


        EditorGUI.BeginChangeCheck();
        var prehitbox = targetComponents.hitboxes;
        var proph = serializedObject.FindProperty("hitboxes");
        EditorGUILayout.PropertyField(proph, true);

        if (EditorGUI.EndChangeCheck())
        {
            if (proph.objectReferenceValue == null || targetComponents.IsStructureSame((HitBoxes)proph.objectReferenceValue) || EditorUtility.DisplayDialog("", "this operation will break the hitboxdata you selected now. really do this?", "do", "cancel"))
            {

                serializedObject.ApplyModifiedProperties();
                targetComponents.SyncKeyFramesWithSimpleAnimator();
            }
            else
            {
                return;
            }
        }

        if (targetComponents.hitboxes == null)
        {
            GUILayout.EndVertical();
            return;
        }

        targetComponents.gizmoController.drawGizmoData.Clear();

        //var curframe = AnimationHelper.GetCurrentFrame();


        EditorGUI.BeginChangeCheck();
        if (targetComponents.hitboxes != null)
        {
            DrawHitboxes();
        }
        GUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();


        SetGizmoControllerParam();
    }

    private void OnSceneGUI()
    {
        if (targetComponents.hitboxes == null)
        {
            return;
        }

        int animationindex = 0;
        int keyframeIndex = 0;

        if (traceAnimMode == TraceAnimMode.Scene)
        {
            animationindex = targetComponents.GetStateNames().FindIndex(e => e == targetComponents.GetNowStateName());
            keyframeIndex = targetComponents.GetModuloFrameTime();
        }
        else
        {
            animationindex = targetComponents.inspectorTemp.animationindex;
            keyframeIndex = targetComponents.inspectorTemp.keyframeIndex;
        }

        var animname = targetComponents.GetStateNames()[animationindex];

        try
        {
            var a = targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders;
        }
        catch
        {
            return;
        }

        foreach (var col in targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders)
        {


#if UNITY_EDITOR
            var precolor = Handles.color;
            Handles.color = new Color(0, 0, 0);

            var sign = targetComponents.GetDirectionSign();

            var position = col.colliderParam.Position;
            position.Scale(new Vector3(sign, 1, 1));
            var movedPosition = Handles.FreeMoveHandle(position + targetComponents.gameObject.transform.position,
                                                     0.05f,
                                                     Vector3.one,
                                                     Handles.DotHandleCap);

            var newposition = movedPosition - targetComponents.gameObject.transform.position;
            newposition.Scale(new Vector3(sign, 1.0f, 1.0f));
            col.colliderParam.Position = newposition;

            var modified = false;
            if (col.colliderParam is RectColliderParam)
            {
                var rect = col.colliderParam as RectColliderParam;
                var helper = new Vector3(rect.rect.size.x * sign, rect.rect.size.y, 0.0f);
                Handles.color = new Color(1, 1, 1);
                var helperpreposition = position + helper;
                helperpreposition.Scale(new Vector3(sign, 1.0f, 1.0f));
                var movedhelper = Handles.FreeMoveHandle(helper + position + targetComponents.gameObject.transform.position,
                                                     0.05f,
                                                     Vector3.one,
                                                     Handles.DotHandleCap);

                if (!Mathf.Approximately((helperpreposition - movedhelper).magnitude, 0))
                {
                    var newsize = movedhelper - (position + targetComponents.gameObject.transform.position);
                    newsize.Scale(new Vector3(sign, 1.0f, 1.0f));
                    rect.rect.size = newsize;
                    modified = true;
                }

            }
            else if (col.colliderParam is SphereColliderParam)
            {
                var sphere = col.colliderParam as SphereColliderParam;
                var helper = new Vector3(sphere.radius, 0.0f, 0.0f);
                Handles.color = new Color(1, 1, 1);
                var helperpreposition = position + helper;
                helperpreposition.Scale(new Vector3(sign, 1.0f, 1.0f));
                var movedhelper = Handles.FreeMoveHandle(helper + position + targetComponents.gameObject.transform.position,
                                                     0.05f,
                                                     Vector3.one,
                                                     Handles.DotHandleCap);

                if (!Mathf.Approximately((helperpreposition - movedhelper).magnitude, 0))
                {
                    sphere.radius = Mathf.Abs((movedhelper - (position + targetComponents.gameObject.transform.position)).x);
                    modified = true;
                }
            }
            if (modified)
            {
                EditorUtility.SetDirty(targetComponents.hitboxes);
                Repaint();
            }

            Handles.color = precolor;
#endif
        }

    }


    private void DrawHitboxes()
    {


        traceAnimMode = (TraceAnimMode)EditorGUILayout.Popup("trace Animation Mode", (int)traceAnimMode, new string[] { "Off", "AnimationWindow", "Scene(PlayOnly)" });
        targetComponents.inspectorTemp.traceAnimMode = (int)traceAnimMode;
        if (traceAnimMode == TraceAnimMode.AnimationWindow)
        {
            var currentFrame = AnimationHelper.GetCurrentFrame();
            targetComponents.inspectorTemp.keyframeIndex = currentFrame;


            var AnimGuiClipname = AnimationHelper.GetCurrentClipName();
            var preanimationindex = targetComponents.inspectorTemp.animationindex;
            targetComponents.inspectorTemp.animationindex = EditorGUILayout.Popup("AnimStateName", targetComponents.inspectorTemp.animationindex, targetComponents.GetStateNames().ToArray());
            var selectedStateName = targetComponents.GetStateNames()[targetComponents.inspectorTemp.animationindex];
            var selectedState = targetComponents.GetState(selectedStateName);
            if (AnimGuiClipname != selectedState.clip.name)
            {
                AnimationHelper.SetCurrentClip(selectedState.clip);
                Debug.Log("From HitBoxComponent: Now AnimationWindow is synchronized with HitBoxComponent. If you want to change AnimationClip of AnimationWindow, turn \"TraceAnimMode\" off on HitBoxComponent Inspector");
            }
            EditorGUILayout.LabelField("Clipname", selectedState.clip.name);

            if (targetComponents.inspectorTemp.keyframeIndex >= targetComponents.hitboxes[selectedStateName].keyframes.Count)
            {
                return;
            }
;
        }
        var animname = targetComponents.GetStateNames()[targetComponents.inspectorTemp.animationindex];
        var keyframenum = targetComponents.hitboxes[animname].keyframes.Count;

        if (traceAnimMode == TraceAnimMode.Scene)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            var currentFrame = targetComponents.GetModuloFrameTime();
            targetComponents.inspectorTemp.keyframeIndex = currentFrame;
            var nowstate = targetComponents.GetNowState();
            EditorGUILayout.LabelField("NowStateName", nowstate.name);
            targetComponents.inspectorTemp.animationindex = targetComponents.GetStateNames().FindIndex(e => e == nowstate.name);
            EditorGUILayout.LabelField("Clipname", nowstate.clip.name);

        }
        if (traceAnimMode == TraceAnimMode.Off)
        {
            targetComponents.inspectorTemp.animationindex = EditorGUILayout.Popup("AnimStateName", targetComponents.inspectorTemp.animationindex, targetComponents.GetStateNames().ToArray());
            var statename = targetComponents.GetStateNames()[targetComponents.inspectorTemp.animationindex];
            EditorGUILayout.LabelField("ClipName", targetComponents.GetClipName(statename));
        }

        EditorGUILayout.LabelField("Flip", targetComponents.direction.ToString());
        targetComponents.hitboxes[animname].isLoopFrame = EditorGUILayout.Toggle("Is Loop FrameData", targetComponents.hitboxes[animname].isLoopFrame);

        if (Application.isPlaying && traceAnimMode == TraceAnimMode.Off)
        {

            if (GUILayout.Button("Show Now KeyFrame Data"))
            {
                var nowanim = targetComponents.GetNowStateName();
                var keyframe = targetComponents.GetModuloFrameTime();
                targetComponents.inspectorTemp.animationindex = targetComponents.GetStateNames().FindIndex(0, e => e == nowanim);
                targetComponents.inspectorTemp.keyframeIndex = keyframe;

            }
        }


        targetComponents.inspectorTemp.keyframeIndex = EditorGUILayout.Popup("KeyFrameIndex", targetComponents.inspectorTemp.keyframeIndex, Enumerable.Range(0, keyframenum).Select(e => e.ToString() + (targetComponents.hitboxes[animname].keyframes[e].colliders.Count > 0 ? " *" : "")).ToArray());
        if (traceAnimMode == TraceAnimMode.AnimationWindow)
        {
            AnimationHelper.SetCurrentFrame(targetComponents.inspectorTemp.keyframeIndex);
            AnimationHelper.RepaintOpenAnimationWindow();
        }

        var animationindex = targetComponents.inspectorTemp.animationindex;
        var keyframeIndex = targetComponents.inspectorTemp.keyframeIndex;


        for (int i = 0; i < targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders.Count; i++)
        {

            GUILayout.BeginVertical(hitboxBackgroundStyle);
            GUILayout.Label("Hitbox " + i);

            var colliderdata = targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders[i];
            //collider info
            colliderdata.tag = EditorGUILayout.TextField("tag", colliderdata.tag);
            colliderdata.layer = EditorGUILayout.TextField("layer", colliderdata.layer);
            colliderdata.dulation = EditorGUILayout.IntField("dulation", colliderdata.dulation);
            colliderdata.data = EditorGUILayout.TextField("data", colliderdata.data);

            //collider shape
            if (colliderdata.colliderParam == null)
            {
                colliderdata.colliderParam = CreateInstance<RectColliderParam>();
            }

            var selectedshape = (ColliderShape)EditorGUILayout.EnumPopup(colliderdata.colliderParam.GetShape());
            var newcolliderparam = ColliderParam.ColliderParamFactory(selectedshape);

            if (newcolliderparam != null && colliderdata.colliderParam.GetShape() != selectedshape)
            {

                var path = AssetDatabase.GetAssetPath(targetComponents.hitboxes);
                AssetDatabase.RemoveObjectFromAsset(colliderdata.colliderParam);

                colliderdata.colliderParam = newcolliderparam;
                AssetDatabase.AddObjectToAsset(colliderdata.colliderParam, path);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);
            }

            DrawColliderParam(colliderdata.colliderParam);

            EditorUtility.SetDirty(colliderdata.colliderParam);


            if (GUILayout.Button("Remove HitBox", GUILayout.Width(100)))
            {
                var path = AssetDatabase.GetAssetPath(targetComponents.hitboxes);
                AssetDatabase.RemoveObjectFromAsset(targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders[i].colliderParam);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);

                for (int j = i; j < targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders.Count - 1; j++)
                {
                    targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders[j] = targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders[j + 1];
                }

                targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders.RemoveAt(targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders.Count - 1);
                SceneView.RepaintAll();
            }
            GUILayout.EndVertical();


            //separate
            GUILayout.BeginVertical(GUILayout.Height(10));
            GUILayout.Label("", GUILayout.Height(10));
            GUILayout.EndVertical();
        }


        if (GUILayout.Button("Add Hitbox", GUILayout.Width(100)))
        {
            var addData = new KeyFrameCollider();
            addData.colliderParam = CreateInstance<RectColliderParam>();
            addData.startframe = keyframeIndex;
            targetComponents.hitboxes[animname].keyframes[keyframeIndex].colliders.Add(addData);

            SceneView.RepaintAll();

            var path = AssetDatabase.GetAssetPath(targetComponents.hitboxes);
            AssetDatabase.AddObjectToAsset(addData.colliderParam, path);

            //インポート処理を走らせて最新の状態にする
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path);
        }
    }

    private void DrawColliderParam(ColliderParam colliderParam)
    {
        EditorGUI.BeginChangeCheck();
        if (colliderParam is RectColliderParam)
        {

            var rect = colliderParam as RectColliderParam;
            var newrect = EditorGUILayout.RectField(rect.rect);
            rect.rect = newrect;
            var is2d = EditorGUILayout.Toggle("is 2D", rect.is2d);
            rect.is2d = is2d;

        }
        else if (colliderParam is SphereColliderParam)
        {
            var sphere = colliderParam as SphereColliderParam;
            var newposition = EditorGUILayout.Vector3Field("position", sphere.position);
            sphere.position = newposition;
            var newradius = EditorGUILayout.FloatField("radius", sphere.radius);
            sphere.radius = newradius;

        }
        else if (colliderParam is CapsuleColliderParam)
        {
            var capsule = colliderParam as CapsuleColliderParam;
            var newstartposition = EditorGUILayout.Vector3Field("startpos", capsule.start);
            capsule.start = newstartposition;
            var newendposition = EditorGUILayout.Vector3Field("endpos", capsule.end);
            capsule.end = newendposition;
            var newradius = EditorGUILayout.FloatField("radius", capsule.radius);
            capsule.radius = newradius;
        }
        else
        {
            Debug.Log("not implemented");
        }


        GUILayout.BeginHorizontal();
        var additionalDataInfoLabel = colliderParam.additionalData == null ? "null" : colliderParam.additionalData.GetType().ToString();
        EditorGUILayout.LabelField(additionalDataInfoLabel);
        if (GUILayout.Button("Set Additional Collider Data"))
        {
            GenericMenu menu = new GenericMenu();
            var assembly = Assembly.Load("Assembly-CSharp");
            var dlist = assembly.GetTypes().Where(c => c.IsSubclassOf(typeof(ColliderAdditionalData))).ToList();
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                if (colliderParam.additionalData != null)
                {
                    var path = AssetDatabase.GetAssetPath(colliderParam.additionalData);
                    AssetDatabase.RemoveObjectFromAsset(colliderParam.additionalData);
                    colliderParam.additionalData = null;
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(path);
                }

            });
            // forward slashes nest menu items under submenus
            foreach (var info in dlist)
            {
                menu.AddItem(new GUIContent(info.ToString()), false, e =>
                {

                    if (colliderParam.additionalData != null)
                    {
                        var path = AssetDatabase.GetAssetPath(colliderParam.additionalData);
                        AssetDatabase.RemoveObjectFromAsset(colliderParam.additionalData);
                    }

                    colliderParam.additionalData = (ColliderAdditionalData)CreateInstance(info.ToString());
                    var parentpath = AssetDatabase.GetAssetPath(colliderParam);
                    AssetDatabase.AddObjectToAsset(colliderParam.additionalData, parentpath);

                    //インポート処理を走らせて最新の状態にする
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(parentpath);


                }
                , info);
            }
            menu.ShowAsContext();
        }
        GUILayout.EndHorizontal();

        if (colliderParam.additionalData != null)
        {
            colliderParam.additionalData.DrawInspectorGUI();

        }


        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(colliderParam);
        }
    }

    private void SetGizmoControllerParam()
    {

        var animationindex = targetComponents.inspectorTemp.animationindex;
        var keyframeIndex = targetComponents.inspectorTemp.keyframeIndex;
        var animname = targetComponents.GetStateNames()[animationindex];

        if (targetComponents.gizmoController != null)
        {
            if (traceAnimMode == 0)
            {
                targetComponents.gizmoController.drawGizmoData.Add(targetComponents.hitboxes[animname].keyframes[keyframeIndex]);
            }
            else
            {
                var curframe = keyframeIndex;
                foreach (var keyframe in targetComponents.hitboxes[animname].keyframes)
                {
                    foreach (var col in keyframe.colliders)
                    {
                        if (col.startframe <= curframe && curframe < col.startframe + col.dulation)
                        {
                            targetComponents.gizmoController.drawGizmoData.Add(keyframe);
                        }
                    }
                }

            }
        }

    }
    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
    #endregion

    private void OnEnable()
    {
        if (hitboxBackgroundStyle == null)
        {
            hitboxBackgroundStyle = new GUIStyle();
            hitboxBackgroundStyle.normal.background = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0.2f));
        }
        //When changing scenes the background is deleted
        else if (hitboxBackgroundStyle.normal.background == null)
        {
            hitboxBackgroundStyle.normal.background = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0.2f));
        }

        targetComponents = target as HitBoxComponent;
        AnimationHelper.init();

        targetComponents.SyncKeyFramesWithSimpleAnimator();

        if (targetComponents.inspectorTemp != null)
        {
            var state = targetComponents.GetState(targetComponents.GetStateNames()[targetComponents.inspectorTemp.animationindex]);
            AnimationHelper.SetCurrentClip(state.clip);
            traceAnimMode = (TraceAnimMode)targetComponents.inspectorTemp.traceAnimMode;
        }
    }

}
