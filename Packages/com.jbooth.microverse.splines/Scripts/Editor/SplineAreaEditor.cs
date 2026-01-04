using UnityEngine;
using UnityEditor;
#if UNITY_2021_3_OR_NEWER

using UnityEngine.Splines;
using UnityEditor.Splines;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(SplineArea), true)]
    [CanEditMultipleObjects]
    public class SplineAreaEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            SplineArea area = target as SplineArea;

            if (area.spline != null && area.spline.Spline != null && !area.spline.Spline.Closed)
            {
                EditorGUILayout.HelpBox("Spline is open, this considers the spline as path. Close spline for area mode.", MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("spline"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closedMode"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sdfRes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSDF"));

            GUIUtil.DrawNoise(area, area.positionNoise, "Position Noise", FilterSet.NoiseOp.Add, false, false);


            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                var path = target as SplineArea;
                path.UpdateSplineSDFs();
                MicroVerse.instance?.Invalidate(path.GetBounds());
            }
        }

        private void OnEnable()
        {
            EditorSplineUtility.AfterSplineWasModified += OnAfterSplineWasModified;
        }
        private void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnAfterSplineWasModified;
        }

        void OnAfterSplineWasModified(Spline spline)
        {
            var path = target as SplineArea;
            if (path != null && path.spline != null && path.spline.Splines != null)
            {
                // first clear renderers of child gameobjects
                // required if you have a hierarchy like this:
                // + Track [Spline Container,SplineArea]
                // -- Trench [Spline Path]
                // otherwise if you modify track then the Trench wouldn't be updated
                // get all splinepath components of children in the hierarchy
                // eg required for the Path content type in which the height is modified via spline path as a child gameobject
                SplinePath[] paths = path.transform.GetComponentsInChildren<SplinePath>();
                foreach (var s in path.spline.Splines)
                {
                    foreach (SplinePath childPath in paths)
                    {
                        if (childPath == path)
                            continue;

                        if (ReferenceEquals(spline, s))
                        {
                            childPath.ClearSplineRenders(path.GetBounds());
                        }
                    }
                }

                // modification of the gameobject with this spline area
                foreach (var s in path.spline.Splines)
                {
                    if (ReferenceEquals(spline, s))
                    {
                        path.UpdateSplineSDFs();
                        MicroVerse.instance?.Invalidate(path.GetBounds());
                        return;
                    }
                }
            }
        }
    }
}
#endif