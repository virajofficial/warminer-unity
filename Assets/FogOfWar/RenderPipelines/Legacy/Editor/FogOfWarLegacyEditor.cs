using UnityEngine;

namespace FoW
{
    public static class FogOfWarLegacyEditor
    {
        public static void TryFixInstallIssues()
        {
            FogOfWarSetup.ForceAddMainShadersInBuild();
            FogOfWarSetup.ForceAddShaderBuild("FogOfWarLegacy");
        }

        public static void GetInstallErrors()
        {
            FogOfWarSetup.CheckMainShadersIncludedInBuild();
            FogOfWarSetup.CheckShaderIsIncludedInBuild("FogOfWarLegacy");
        }

        public static void GetSceneErrors()
        {
            FogOfWarLegacy[] renderers = Object.FindObjectsOfType<FogOfWarLegacy>();
            if (renderers.Length == 0)
                FogOfWarError.Error(null, "There are no FogOfWarLegacy components in the scene (it should be on the same GameObject as your camera).");

            FogOfWarTeam[] teams = Object.FindObjectsOfType<FogOfWarTeam>();

            foreach (FogOfWarLegacy renderer in renderers)
            {
                if (!System.Array.Exists(teams, t => t.team == renderer.team))
                    FogOfWarError.Error(renderer, "There are no FogOfWarTeams in the scene with team index " + renderer.team.ToString() + "!");

                if (renderer.fogColor.a < 0.001f)
                    FogOfWarError.Error(renderer, "The fog color is set to be transparent and won't be visible!");
            }
        }
    }
}
