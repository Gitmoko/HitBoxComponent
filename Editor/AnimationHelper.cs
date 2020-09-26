using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

//https://forum.unity.com/threads/animation-window-preview-animation-with-specific-start-and-end-frames.467892/#post-3045523


[InitializeOnLoad]
public class AnimationHelper
{
    private static bool initflag = false;
    private static UnityEngine.Object _window;
    private static BindingFlags _flags;
    private static FieldInfo _animEditor;
    private static Type _animEditorType;
    private static System.Object _animEditorObject;
    private static FieldInfo _animWindowState;
    private static System.Object _animobj;
    private static Type _windowStateType;

    private AnimationHelper()
    {
        EditorApplication.update += init;
    }

    public static void init()
    {

        _window = GetOpenAnimationWindow();

        _flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        _animEditor = GetAnimationWindowType().GetField("m_AnimEditor", _flags);

        _animEditorType = _animEditor.FieldType;
        _animEditorObject = _animEditor.GetValue(_window);
        _animWindowState = _animEditorType.GetField("m_State", _flags);
        _animobj = _animWindowState.GetValue(_animEditorObject);
        _windowStateType = _animWindowState.FieldType;
        initflag = true;

    }



    public static bool GetPlaying()
    {
        bool ret = false;

        if (_window != null)
        {
            System.Object playing = _windowStateType.InvokeMember("get_playing", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);

            ret = (bool)playing;
        }

        return ret;
    }

    public static void Repaint()
    {
        if (_window != null)
        {
            _windowStateType.InvokeMember("Repaint", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);
        }
    }

    public static void StartPlayback()
    {
        if (_window != null)
        {
            _windowStateType.InvokeMember("StartPlayback", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);
        }
    }

    public static void StopPlayback()
    {
        if (_window != null)
        {
            _windowStateType.InvokeMember("StopPlayback", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);
        }
    }

    public static void SetCurrentFrame(int frame)
    {
        if (_window != null)
        {
            PropertyInfo pi = _windowStateType.GetProperty("currentFrame");
            pi.SetValue(_animobj, (object)frame);
            //System.Object frame = _windowStateType.InvokeMember("currentTime", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);
        }
    }

    public static int GetCurrentFrame()
    {
        int ret = 0;

        if (_window != null)
        {
            PropertyInfo pi = _windowStateType.GetProperty("currentFrame");
            var frame = pi.GetValue(_animobj);
            //System.Object frame = _windowStateType.InvokeMember("currentTime", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);

            ret = (int)frame;
        }

        return ret;
    }

    public static string GetCurrentClipName()
    {
        string ret = "";

        if (_window != null)
        {
            PropertyInfo pi = _windowStateType.GetProperty("activeAnimationClip");
            var obj = pi.GetValue(_animobj);
            //System.Object frame = _windowStateType.InvokeMember("currentTime", BindingFlags.InvokeMethod | BindingFlags.Public, null, _animWindowState.GetValue(_animEditorObject), null);

            var clip = obj as AnimationClip;
            ret = clip.name;
        }

        return ret;
    }

    public static void SetCurrentClip(AnimationClip clip)
    {
        if (_window != null)
        {
            PropertyInfo pi = _windowStateType.GetProperty("activeAnimationClip");
            pi.SetValue(_animobj, clip);

        }
    }

    private static System.Type animationWindowType = null;

    private static System.Type GetAnimationWindowType()
    {
        if (animationWindowType == null)
        {
            animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
        }
        return animationWindowType;
    }

    private static UnityEngine.Object GetOpenAnimationWindow()
    {
        UnityEngine.Object[] openAnimationWindows = Resources.FindObjectsOfTypeAll(GetAnimationWindowType());
        if (openAnimationWindows.Length > 0)
        {
            return openAnimationWindows[0];
        }
        return null;
    }

    public static void RepaintOpenAnimationWindow()
    {
        UnityEngine.Object w = GetOpenAnimationWindow();
        if (w != null)
        {
            (w as EditorWindow).Repaint();
        }
    }

    private static void PrintMethods()
    {
        UnityEngine.Object w = GetOpenAnimationWindow();
        if (w != null)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            FieldInfo animEditor = GetAnimationWindowType().GetField("m_AnimEditor", flags);

            Type animEditorType = animEditor.FieldType;
            System.Object animEditorObject = animEditor.GetValue(w);
            FieldInfo animWindowState = animEditorType.GetField("m_State", flags);
            Type windowStateType = animWindowState.FieldType;

            Debug.Log("Methods");
            MethodInfo[] methods = windowStateType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Debug.Log("Methods : " + methods.Length);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo currentInfo = methods[i];
                Debug.Log(currentInfo.ToString());
            }
        }
    }

}