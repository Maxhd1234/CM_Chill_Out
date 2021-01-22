using HarmonyLib;
using RimWorld;
using Verse;

namespace CM_Chill_Out
{
    public class ChillOutMod : Mod
    {
        private static ChillOutMod _instance;
        public static ChillOutMod Instance => _instance;

        public ChillOutMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("CM_Chill_Out");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
