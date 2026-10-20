#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GameConfigMenu
{
    [MenuItem("Tools/Hook Climber/Select Config")]
    static void SelectConfig()
    {
        var cfg = GameConfig.I;
        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
    }
}
#endif
