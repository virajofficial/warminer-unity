using UnityEngine;
using UnityEditor;

namespace FoW
{
    [CustomEditor(typeof(FogOfWarHideInFog))]
    [CanEditMultipleObjects]
    public class FogOfWarHideInFogEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length == 1)
            {
                GetErrors((FogOfWarHideInFog)target, FindObjectsOfType<FogOfWarTeam>());
                FogOfWarError.Display(false);
            }
        }

        public static void GetErrors(FogOfWarHideInFog hideinfog, FogOfWarTeam[] teams)
        {
            if (!System.Array.Exists(teams, t => t.team == hideinfog.team))
                FogOfWarError.Error(hideinfog, "Pointing to FogOfWarTeam index '" + hideinfog.team + "' that does not exist.");
        }
    }
}
