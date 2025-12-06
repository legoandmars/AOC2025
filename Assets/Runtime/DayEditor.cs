#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DayBase))]
public class DayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Run once"))
        {
            ((DayBase)target).Run(1);
        }
        if (GUILayout.Button("Run 10,000 times"))
        {
            ((DayBase)target).Run(10000);
        }
        if (GUILayout.Button("Run 1,000,000 times"))
        {
            ((DayBase)target).Run(1000000);
        }
    }
}

[CustomEditor(typeof(DayOne))]
public class DayOneEditor : DayEditor
{
}

[CustomEditor(typeof(DayOnePartTwo))]
public class DayOnePartTwoEditor : DayEditor
{
}

[CustomEditor(typeof(DayTwo))]
public class DayTwoEditor : DayEditor
{
}

#endif