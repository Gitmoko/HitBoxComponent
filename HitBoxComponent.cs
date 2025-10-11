using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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

    [SerializeField]
    public bool isLoopFrame;
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
    public int traceAnimMode;
}

public enum DirectionType
{
    Forward,
    Backward

}

[Serializable]
[ExecuteInEditMode]
public class HitBoxComponent : MonoBehaviour
{
    [HideInInspector]
    public HitBoxComponentGizmoController gizmoController = new HitBoxComponentGizmoController();

    [HideInInspector]
    public HitBoxComponentInspectorTemporaryValue inspectorTemp = new HitBoxComponentInspectorTemporaryValue();

    public HitBoxes hitboxes = null;

    [SerializeField]
    public SimpleAnimation simpleAnimation;

    public DirectionType direction = DirectionType.Forward;


    private List<KVPair<GameObject, ColliderInfo>> nowColliders = new List<KVPair<GameObject, ColliderInfo>>();

    private float? previousNormalizedTime;

    private SimpleAnimation.State prestate = null;

    public bool stateChanged = false;

    public bool enableFlipByDirection = true;

    private bool autoEnd;

    private Action autoEndCallback;

    private SimpleAnimation.State _currentState;

    private SimpleAnimation.State CurrentState
    {
        get
        {
            _currentState ??= simpleAnimation.GetStates().FirstOrDefault(s => s.enabled);
            return _currentState;
        }
        set
        {
            _currentState = value;
        }
    }

    private void OnEnable()
    {
        simpleAnimation ??= GetComponent<SimpleAnimation>();
        if (Application.isPlaying)
        {
            simpleAnimation.enabled = true;
        }
        SyncKeyFramesWithSimpleAnimator();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            CurrentState = null;
            if (simpleAnimation.GetState("Default").clip != null)
            {
                simpleAnimation.Play("Default");
            }
            simpleAnimation.enabled = false;
            foreach (var e in nowColliders)
            {
                if (e.Key != null)
                {
                    Destroy(e.Key.gameObject);
                }
            }
            nowColliders.Clear();
            previousNormalizedTime = null;
            prestate = null;
            stateChanged = false;
        }
    }

    private void Reset()
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
            if (CurrentState == null)
            {
                return;
            }

            if (autoEnd && GetNormalizedTime() >= 1)
            {
                autoEndCallback?.Invoke();
                simpleAnimation.Play("Default");
                CurrentState = simpleAnimation.GetStates().FirstOrDefault(s => s.enabled);
                autoEnd = false;
                autoEndCallback = null;
                previousNormalizedTime = null;
            }

            if (prestate == null || stateChanged)
            {
                prestate = CurrentState;
                foreach (var e in nowColliders)
                {
                    if (e.Key != null)
                    {
                        Destroy(e.Key.gameObject);
                    }
                }
                nowColliders.Clear();
                previousNormalizedTime = null;
                stateChanged = false;
            }


            var destroylist = new List<GameObject>();
            ///生存期間のすぎたcolliderのリスト化

            nowColliders.RemoveAll((KVPair<GameObject, ColliderInfo> e) =>
            {
                var elapsed = Time.time - e.Value.startTime;
                if (e.Value.keyframecollider.dulation / CurrentState.clip.frameRate <= elapsed || !e.Key.activeSelf)
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

            //新たに発生したcolliderの追加
            if (hitboxes.data.Find(e => e.Key == CurrentState.name) != null)
            {

                List<int> processFrames = new();
                if (previousNormalizedTime == null)
                {
                    //表示しているフレームだけ処理
                    processFrames = new List<int> { Mathf.FloorToInt(this.CurrentState.time * this.CurrentState.clip.frameRate) };
                }
                else
                {
                    //previousTimeのフレーム+1　~ 今表示しているフレーム (ループを加味する) 
                    var loopTime = Mathf.FloorToInt(this.CurrentState.normalizedTime - previousNormalizedTime.Value);
                    if (loopTime == 0)
                    {
                        var currentFrame = Mathf.FloorToInt(CurrentState.normalizedTime % 1.0f * this.CurrentState.clip.length * this.CurrentState.clip.frameRate);
                        var previousFrame = Mathf.FloorToInt(previousNormalizedTime.Value % 1.0f * this.CurrentState.clip.length * this.CurrentState.clip.frameRate);
                        if (Mathf.FloorToInt(previousNormalizedTime.Value) != Mathf.FloorToInt(this.CurrentState.normalizedTime))
                        {
                            processFrames = Enumerable.Range(previousFrame + 1, Mathf.Max(0, hitboxes[CurrentState.name].keyframes.Count - previousFrame))
                            .Concat(Enumerable.Range(0, currentFrame + 1))
                            .ToList();
                        }
                        else
                        {
                            processFrames = Enumerable.Range(previousFrame + 1, currentFrame - previousFrame).ToList();
                        }
                    }
                    else
                    {
                        //一周以上する場合は後回し
                        Debug.LogError("アニメーションが1fで一周以上した");
                    }
                }
                foreach (var i in processFrames)
                {
                    if (hitboxes[CurrentState.name].keyframes.Count - 1 < i)
                    {
                        continue;
                    }
                    foreach (var e in hitboxes[CurrentState.name].keyframes[i].colliders)
                    {
                        Debug.Log("create col");
                        var col = AddColliderComponentFromParam(e, Time.time);
                        nowColliders.Add(new KVPair<GameObject, ColliderInfo>(col.gameObject, col));
                    }
                }
            }
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

            previousNormalizedTime = this.CurrentState.normalizedTime;
        }
    }

    public ColliderInfo AddColliderComponentFromParam(KeyFrameCollider keyframe, float startTime)
    {


        var ret = new GameObject("test");
        var objtrans = ret.GetComponent<Transform>();
        var colliderinfo = ret.AddComponent<ColliderInfo>();
        colliderinfo.keyframecollider = keyframe;
        colliderinfo.hitboxParent = gameObject;
        colliderinfo.startTime = startTime;

        var param = keyframe.colliderParam;

        float sign = enableFlipByDirection ? ((direction == DirectionType.Forward) ? 1 : -1 ) : 1f;


        if (param is RectColliderParam)
        {
            var rect = param as RectColliderParam;
            if (rect.is2d)
            {
                var col = ret.AddComponent<BoxCollider2D>();
                col.offset = new Vector3(rect.rect.center.x * sign, rect.rect.center.y);
                col.size = new Vector3(rect.rect.size.x, rect.rect.size.y);
                col.tag = keyframe.tag;
                col.gameObject.layer = LayerMask.NameToLayer(keyframe.layer);
                col.isTrigger = true;
                var r2d = ret.AddComponent<Rigidbody2D>();
                r2d.gravityScale = 0;
                r2d.isKinematic = true;
               
            }
            else
            {
                var col = ret.AddComponent<BoxCollider>();
                col.center = new Vector3(rect.rect.center.x * sign, rect.rect.center.y, 0.0f);
                col.size = new Vector3(rect.rect.size.x, rect.rect.size.y, 0.2f);
                col.tag = keyframe.tag;
                col.gameObject.layer = LayerMask.NameToLayer(keyframe.layer);
                col.isTrigger = true;
                var rd = ret.AddComponent<Rigidbody>();
                rd.isKinematic = true;
                rd.useGravity = false;
            }
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
            var rd = ret.AddComponent<Rigidbody>();
            rd.isKinematic = true;
            rd.useGravity = false;

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

        return colliderinfo;

    }

    public void SyncKeyFramesWithSimpleAnimator()
    {

        if (hitboxes == null)
        {
            return;
        }

        //simpleanimationに存在しないステートのデータがHitBoxesに入っている場合削除
        var states = GetEditorStates();
        hitboxes.data.RemoveAll(hitboxkeyval => !states.Any((s) => s.name == hitboxkeyval.Key));

        foreach (var state in states)
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

    //targethitboxesの中身が現在のsimpleAnimationの構成と一致するかを返す
    public bool IsStructureSame(HitBoxes targethitboxes)
    {

        if (hitboxes == null)
        {
            return true;
        }

        var states = GetEditorStates();
        foreach (var e in targethitboxes.data)
        {
            //simpleanimationに存在しないstate名がtargethitboxesの中にあれば構成が一致していないとみなす
            if (!states.Any(s => s.name == e.Key))
            {
                return false;
            }
        }

        return true;

    }

    public List<string> GetStateNames()
    {
        var animationStates = GetEditorStates();
        var statenames = new List<string>();
        for (int i = 0; i < animationStates.Length; i++)
        {
            statenames.Add(animationStates[i].name);
        }
        return statenames;
    }

    public SimpleAnimation.EditorState GetState(string statename)
    {
        var state = GetEditorStates().First(e => e.name == statename);
        return state;
    }

    public string GetClipName(string statename)
    {
        var clipname = GetEditorStates().First(e => e.name == statename).clip.name;
        return clipname;
    }


    //実行中の現在ステートの取得
    public SimpleAnimation.State GetNowState()
    {
        return CurrentState;
    }


    //実行中の現在ステートの取得
    public string GetNowStateName()
    {
        return CurrentState.name;
    }

    public int GetElapsedFrameTime()
    {
        var ret = Convert.ToInt32(Math.Floor(CurrentState.clip.frameRate * CurrentState.time));
        return ret;
    }

    public int GetModuloFrameTime()
    {
        var moduloTime = CurrentState.normalizedTime % 1.0f;
        var ret = Convert.ToInt32(Math.Floor(CurrentState.clip.frameRate * CurrentState.clip.length * moduloTime));
        return ret;
    }


    public float GetNormalizedTime()
    {
        var nowstate = simpleAnimation.GetStates().First(s => s.enabled);
        var normalizedTime = nowstate.normalizedTime;

        return normalizedTime;
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

    public void PlayAnimation(string name, float speed = 1f, bool autoEnd = false, Action callBack = null)
    {
        //同じnameのステートを呼び出しても、AnimationClipの先頭のイベントは呼び出されない
        //先頭以降なら呼び出されそう
        //「アニメーションが始まったフレームで行いたい処理」はイベント以外でやった方がよさそう
        simpleAnimation.Stop();
        simpleAnimation.Play(name);
        CurrentState = simpleAnimation.GetStates().FirstOrDefault(s => s.enabled);
        CurrentState.speed = speed;
        stateChanged = true;
        if(autoEnd){
            this.autoEnd = autoEnd;
            autoEndCallback = callBack;
        }
    }
    
    public void PlayAnimation(string name)
    {
        PlayAnimation(name, 1f, false, null);
    }

    private SimpleAnimation.EditorState[] GetEditorStates()
    {
        Type type = simpleAnimation.GetType();

        //// 以下privateフィールドの値を無理やり取得する
        // Typeからフィールドを探す。フィールド名とBindingFlagsを引数にする。
        FieldInfo field = type.GetField("m_States", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance);
        SimpleAnimation.EditorState[] states = (SimpleAnimation.EditorState[])(field.GetValue(simpleAnimation));

        return states;
    }
}
