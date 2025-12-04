#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DayBase))]
public class DayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Run Editor Action"))
        {
            ((DayBase)target).Run();
        }
    }
}

[CustomEditor(typeof(DayOne))]
public class DayOneEditor : DayEditor
{
}
#endif