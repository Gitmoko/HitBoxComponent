using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class KVPair<TKey, TValue>
{
    public KVPair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }


    public TKey Key;

    public TValue Value;
}

public enum ColliderShape
{
    Rect,
    Sphere,
    Cube,
    Capsule,
    Mesh,
    None,
}



[Serializable]
public class KeyFrameCollider
{
    public ColliderParam colliderParam;
    public string tag = "Untagged";
    public string layer = "Default";
    public int dulation = 1;
    public int startframe = 0;
    public string data;

}


[Serializable]
public class KeyFrameData
{
    [SerializeField]
    public List<KeyFrameCollider> colliders = new List<KeyFrameCollider>();
}

[Serializable]
public class HitBoxKeyFrames
{
    [SerializeField]
    public List<KeyFrameData> keyframes = new List<KeyFrameData>();
}


[Serializable]
public class HitBoxesKeyVal : KVPair<string, HitBoxKeyFrames>, IComparable<HitBoxesKeyVal>
{
    public HitBoxesKeyVal(string key, HitBoxKeyFrames data) : base(key, data)
    {

    }


    public int CompareTo(HitBoxesKeyVal b)
    {
        var ret = Key.CompareTo(b.Key);
        return ret;
    }
}



public class HitBoxComponentGizmoController
{
    [HideInInspector]
    public List<KeyFrameData> drawGizmoData = new List<KeyFrameData>();
}


/// <summary>
/// InspectorのFocusが外れるたびにEditor用の値が初期されるので保存しておく
/// </summary>
public class HitBoxComponentInspectorTemporaryValue
{
    public int animationindex;
    public int keyframeIndex;
}

public enum DirectionType
{
    Forward,
    Backward

}

[Serializable]
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SimpleAnimation))]
public class HitBoxComponent : MonoBehaviour
{
    [HideInInspector]
    public HitBoxComponentGizmoController gizmoController = new HitBoxComponentGizmoController();

    [HideInInspector]
    public HitBoxComponentInspectorTemporaryValue inspectorTemp = new HitBoxComponentInspectorTemporaryValue();

    public HitBoxes hitboxes = null;

    public SimpleAnimation simpleAnimation;

    [HideInInspector]
    public DirectionType direction = DirectionType.Forward;


    private List<KVPair<GameObject, KeyFrameCollider>> nowColliders = new List<KVPair<GameObject, KeyFrameCollider>>();

    private int preveousKeyFrameIndex = -1;

    private SimpleAnimation.State prestate = null;


    private void OnEnable()
    {
        simpleAnimation = GetComponent<SimpleAnimation>();
        SyncKeyFramesWithSimpleAnimator();
    }


    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (hitboxes != null)
        {
            ;

            var nowstates = simpleAnimation.GetStates().First(s => s.enabled);
            //var newKeyframesIndex = Convert.ToInt32(Math.Floor(hitboxes[nowstates.name].keyframes.Count * elapsedTime));
            var newKeyframesIndex = GetNowKeyFrame();

            if (prestate == null || prestate.name != nowstates.name)
            {
                prestate = nowstates;
                foreach (var e in nowColliders)
                {
                    if (e.Key != null)
                    {
                        Destroy(e.Key.gameObject);
                    }
                }
                nowColliders.Clear();
                preveousKeyFrameIndex = -1;
            }

            if (preveousKeyFrameIndex != newKeyframesIndex)
            {

                var destroylist = new List<GameObject>();
                ///生存期間のすぎたcolliderのリスト化


                nowColliders.RemoveAll((KVPair<GameObject, KeyFrameCollider> e) =>
                {
                    var elapsed = newKeyframesIndex - e.Value.startframe;
                    if (elapsed >= e.Value.dulation && e.Key.activeSelf)
                    {

                        destroylist.Add(e.Key);

                        return true;
                    }
                    return false;
                });


                /// 生存期間の過ぎたcolliderの削除
                foreach (var e in destroylist)
                {
                    Destroy(e);
                }


                for (int i = preveousKeyFrameIndex + 1; i <= newKeyframesIndex; i++)
                {
                    //新たに発生したcolliderの追加
                    foreach (var e in hitboxes[nowstates.name].keyframes[i].colliders)
                    {
                        Debug.Log("create col");
                        var col = AddColliderComponentFromParam(e);
                        nowColliders.Add(new KVPair<GameObject, KeyFrameCollider>(col, e));

                    }

                }
                preveousKeyFrameIndex = newKeyframesIndex;
                /*
                if (KeyframesIndex == 0)
                {
                    nowkeyframedata.colliders.Add(new KeyFrameCollider());
                    var obj = new GameObject("test");
                    var objtrans = obj.GetComponent<Transform>();
                    var rigidbody = obj.AddComponent<Rigidbody>();
                    rigidbody.useGravity = false;
                    var col = obj.AddComponent<CapsuleCollider>();
                    nowkeyframedata.colliders[0].collider = col;
                    col.center = new Vector3(3, 0);
                    col.radius = 1;
                    col.height = 2;
                    col.tag = "test";
                    col.isTrigger = true;

                    objtrans.SetParent(gameObject.transform, false);

                }
                */

            }



        }
    }

    public GameObject AddColliderComponentFromParam(KeyFrameCollider keyframe)
    {


        var ret = new GameObject("test");
        var objtrans = ret.GetComponent<Transform>();
        var rigidbody = ret.AddComponent<Rigidbody>();
        var colliderinfo = ret.AddComponent<ColliderInfo>();
        colliderinfo.keyframecollider = keyframe;
        rigidbody.useGravity = false;

        var param = keyframe.colliderParam;

        float sign = (direction == DirectionType.Forward) ? 1 : -1;


        if (param is RectColliderParam)
        {
            var rect = param as RectColliderParam;
            var col = ret.AddComponent<BoxCollider>();
            col.center = new Vector3(rect.rect.center.x * sign, rect.rect.center.y, 0.0f);
            col.size = new Vector3(rect.rect.size.x, rect.rect.size.y, 0.2f);
            col.tag = keyframe.tag;
            col.gameObject.layer = LayerMask.NameToLayer(keyframe.layer);
            col.isTrigger = true;
        }
        else if (param is SphereColliderParam)
        {
            var sphere = param as SphereColliderParam;
            var col = ret.AddComponent<SphereCollider>();
            col.center = new Vector3(sphere.position.x, sphere.position.y, 0.0f);
            col.radius = sphere.radius;
            col.tag = keyframe.tag;
            col.gameObject.layer = LayerMask.NameToLayer(keyframe.layer);
            col.isTrigger = true;

        }
        else if (param is CapsuleColliderParam)
        {
            Debug.LogError("Not Implemented");
        }
        else
        {
            Debug.LogError("Not Implemented");
        }
        objtrans.SetParent(gameObject.transform, false);

        return ret;

    }

    public void SyncKeyFramesWithSimpleAnimator()
    {

        if (hitboxes == null)
        {
            return;
        }

        foreach (var state in simpleAnimation.GetEditorStates())
        {
            //ToInt32は四捨五入するので、多少の誤差があっても正確にフレーム数を取得できる
            var numframe = Convert.ToInt32(state.clip.length * state.clip.frameRate);

            //keyframesの数が変化しない場合
            if (hitboxes[state.name].keyframes.Count == numframe)
            {
                continue;
            }
            //keyframesが縮小する場合
            else if (hitboxes[state.name].keyframes.Count > numframe)
            {
                hitboxes[state.name].keyframes.RemoveRange(numframe, hitboxes[state.name].keyframes.Count - numframe);
            }
            //keyframesが拡大する場合
            else
            {
                var addSize = numframe - hitboxes[state.name].keyframes.Count;
                for (int i = 0; i < addSize; i++)
                {
                    hitboxes[state.name].keyframes.Add(new KeyFrameData());
                }
            }
        }
    }

    public List<string> GetStateNames()
    {
        var animationStates = simpleAnimation.GetEditorStates();
        var statenames = new List<string>();
        for (int i = 0; i < animationStates.Length; i++)
        {
            statenames.Add(animationStates[i].name);
        }
        return statenames;
    }

    public string GetClipName(string statename)
    {
        var clipname = simpleAnimation.GetEditorStates().First(e => e.name == statename).clip.name;
        return clipname;
    }


    //実行中の現在ステートの取得
    public SimpleAnimation.State GetNowState()
    {
        var nowstates = simpleAnimation.GetStates().First(s => s.enabled);
        return nowstates;
    }


    //実行中の現在ステートの取得
    public string GetNowStateName()
    {
        var nowstates = simpleAnimation.GetStates().First(s => s.enabled);
        return nowstates.name;
    }

    //実行中の現在ステートの取得
    public int GetNowKeyFrame()
    {
        var nowstate = simpleAnimation.GetStates().First(s => s.enabled);
        var normalizedTime = nowstate.normalizedTime;
        float elapsedTime = normalizedTime - (float)Math.Truncate(normalizedTime);
        int ret = 0;
        if (normalizedTime >= 1 && !(nowstate.wrapMode == WrapMode.Loop))
        {
            ret = Convert.ToInt32(nowstate.clip.length * nowstate.clip.frameRate) - 1;
        }
        else
        {
            ret = Convert.ToInt32(Math.Floor(nowstate.clip.frameRate * nowstate.clip.length * elapsedTime));
        }
        return ret;
    }

    public float GetDirectionSign()
    {
        float ret = (direction == DirectionType.Forward) ? 1.0f : -1.0f;
        return ret;
    }

    public void SetDirectionReverse()
    {
        if (direction == DirectionType.Forward)
        {
            direction = DirectionType.Backward;
        }
        else
        {
            direction = DirectionType.Forward;
        }
    }
}
