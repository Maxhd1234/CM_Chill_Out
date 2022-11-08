using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace KB_Chill_Out
{
    [StaticConstructorOnStartup]
    public static class FloatMenuMakerMap_Patches
    {
        [HarmonyPatch(typeof(FloatMenuMakerMap))]
        [HarmonyPatch("AddHumanlikeOrders", MethodType.Normal)]
        public static class FloatMenuMakerMap_AddHumanlikeOrders
        {
            [HarmonyPostfix]
            public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
            {
                if (pawn.needs == null || pawn.needs.joy == null)
                    return;

                List<JoyGiverDef> joyGiverDefs = DefDatabase<JoyGiverDef>.AllDefsListForReading.Where(joyGiverDef => joyGiverDef.Worker as JoyGiver_InteractBuilding != null).ToList();
                List<ThingDef> joyThingDefs = joyGiverDefs.SelectMany(joyGiverDef => joyGiverDef.thingDefs).ToList();

                foreach (LocalTargetInfo joyTarget in GenUI.TargetsAt(clickPos, ForJoying(pawn, joyThingDefs), thingsOnly: true))
                {
                    JoyGiverDef joyGiverDef = joyGiverDefs.Find(giverDef => giverDef.thingDefs.Contains(joyTarget.Thing.def));
                    if (joyGiverDef == null)
                    {
                        Log.Warning("ChillOut: Could not find JoyGiverDef for " + joyTarget.Thing.def.defName + ", this should not be possible...");
                        continue;
                    }

                    JoyGiver_InteractBuilding interactBuilding = joyGiverDef.Worker as JoyGiver_InteractBuilding;

                    MethodInfo getPlayJob = interactBuilding.GetType().GetMethod("TryGivePlayJob", BindingFlags.NonPublic | BindingFlags.Instance);
                    Job joyJob = getPlayJob?.Invoke(interactBuilding, new object[] { pawn, joyTarget.Thing }) as Job;

                    if (joyJob != null)
                    {
                        if (pawn.needs.joy.CurLevel > 0.75f)
                        {
                            opts.Add(new FloatMenuOption("KB_Chill_Out_Cannot_Engage".Translate() + " " + joyGiverDef.joyKind.label + ": " + "KB_Chill_Out_Not_Bored".Translate().CapitalizeFirst(), null));
                        }
                        else if (!pawn.CanReach(joyTarget, PathEndMode.OnCell, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("KB_Chill_Out_Cannot_Engage".Translate() + " " + joyGiverDef.joyKind.label + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("KB_Chill_Out_Engage".Translate() + " " + joyGiverDef.joyKind.label, delegate
                            {
                                pawn.jobs.TryTakeOrderedJob(joyJob);
                            }, MenuOptionPriority.High), pawn, joyTarget.Thing));
                        }
                    }
                }
            }

            private static TargetingParameters ForJoying(Pawn sleeper, List<ThingDef> joyThingDefs)
            {
                return new TargetingParameters
                {
                    canTargetPawns = false,
                    canTargetBuildings = true,
                    mapObjectTargetsMustBeAutoAttackable = false,
                    validator = delegate (TargetInfo targ)
                    {
                        if (!targ.HasThing)
                        {
                            return false;
                        }

                        return joyThingDefs.Contains(targ.Thing.def);
                    }
                };
            }
        }
    }
}
