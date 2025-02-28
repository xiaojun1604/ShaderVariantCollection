// Editor/FrameDebuggerTools/AutoFrameDebugger.cs
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using System;
using System.Reflection;

public static class AutoFrameDebugger
{
    private static EditorWindow _frameDebuggerEditorWindow;
    private static Type _frameDebugWindows_Type;

    public static void FrameDebuggerWidnowInit()
    {
        var assembly = typeof(AnimationUtility).Assembly;
        if (null == _frameDebuggerEditorWindow)
        {
            _frameDebugWindows_Type = assembly.GetType("UnityEditor.FrameDebuggerWindow");
        }

        if (null == _frameDebuggerEditorWindow)
        {
            _frameDebuggerEditorWindow = EditorWindow.GetWindow(_frameDebugWindows_Type);
        }
    }

    public static void ToggleFrameDebuggerEnabled()
    {
        if (_frameDebuggerEditorWindow == null && _frameDebuggerEditorWindow == null)
        {
            FrameDebuggerWidnowInit();
        }
        _frameDebugWindows_Type.GetMethod("ToggleFrameDebuggerEnabled", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_frameDebuggerEditorWindow, null);
    }

    [MenuItem("Tools/Frame Debugger/Auto Start")]
    public static void StartCapture()
    {
        try
        {
            ToggleFrameDebuggerEnabled();
        }
        catch (Exception e)
        {
            Debug.LogError($"StartCapture failed: {e}");
        }
    }

    [MenuItem("Tools/Frame Debugger/Auto Stop")]
    public static void StopCapture()
    {
        //ToggleFrameDebuggerEnabled();
    }

   

    [InitializeOnLoadMethod]
    private static void AutoHook()
    {
        Debug.Log("AutoHook");
        var assembly = typeof(AnimationUtility).Assembly;
        _frameDebugWindows_Type = assembly.GetType("UnityEditor.FrameDebuggerWindow");
        _frameDebuggerEditorWindow = EditorWindow.GetWindow(_frameDebugWindows_Type);
        
        // Play模式自动启动
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                StartCapture();
            else if (state == PlayModeStateChange.ExitingPlayMode)
                StopCapture();
        };
    }
}