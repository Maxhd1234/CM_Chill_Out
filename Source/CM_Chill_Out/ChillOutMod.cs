using HarmonyLib;
using RimWorld;
using Verse;

namespace KB_Chill_Out
{
    public class ChillOutMod : Mod
    {
        private static ChillOutMod _instance;
        public static ChillOutMod Instance => _instance;

        public ChillOutMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("KB_Chill_Out");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
