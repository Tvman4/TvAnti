#if UNITY_EDITOR
using UnityEditor;using UnityEngine;
namespace TvAnti.Editor { public static class TvAntiSetupWizard { [MenuItem("TvAnti/Create Runtime")]
 static void Create(){var x=Object.FindObjectOfType<TvAntiRuntime>();if(x!=null){Selection.activeObject=x.gameObject;return;}var g=new GameObject("TvAnti");g.AddComponent<TvAntiRuntime>();Undo.RegisterCreatedObjectUndo(g,"Create TvAnti");Selection.activeObject=g;}}
}
#endif
