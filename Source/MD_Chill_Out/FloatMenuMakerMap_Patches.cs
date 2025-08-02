using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public class FloatMenuOptionProvider_ChillOut : FloatMenuOptionProvider
{
    protected override bool Drafted => false;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOptionFor(Thing thing, FloatMenuContext context)
    {

        Pawn pawn = context.FirstSelectedPawn;
        if (pawn.needs == null || pawn.needs.joy == null)
        {
            return base.GetSingleOptionFor(pawn, context);
        }
        List<JoyGiverDef> list = DefDatabase<JoyGiverDef>.AllDefsListForReading
            .Where(jg => jg.Worker is JoyGiver_InteractBuilding).ToList();
        List<ThingDef> joyThingDefs = list.SelectMany(jg => jg.thingDefs).ToList();



        foreach (LocalTargetInfo joyTarget in GenUI.TargetsAt(thing.DrawPos, ForJoying(pawn, joyThingDefs), thingsOnly: true))
        {
            JoyGiverDef joyGiverDef = list.Find(gd => gd.thingDefs.Contains(joyTarget.Thing.def));

            if (!joyThingDefs.Contains(joyTarget.Thing.def))
                continue;

            if (joyGiverDef == null)
            {
                Log.Warning("ChillOut: Could not find JoyGiverDef for " + joyTarget.Thing.def.defName + ", this should not be possible...");
                continue;
            }

            JoyGiver_InteractBuilding joyGiver_InteractBuilding = joyGiverDef.Worker as JoyGiver_InteractBuilding;
            if (joyGiver_InteractBuilding == null)
            {
                Log.Warning("ChillOut: JoyGiverDef.Worker is not of type JoyGiver_InteractBuilding for " + joyGiverDef.defName);
                continue;
            }

            var tryGivePlayJobMethod = joyGiver_InteractBuilding.GetType()
                .GetMethod("TryGivePlayJob", BindingFlags.Instance | BindingFlags.NonPublic);
            if (tryGivePlayJobMethod == null)
            {
                Log.Warning("ChillOut: Didnt find method TryGivePlayJob" + joyGiverDef.defName);
                continue;
            }

            Job joyJob = tryGivePlayJobMethod.Invoke(joyGiver_InteractBuilding, new object[2] { pawn, joyTarget.Thing }) as Job;
            if (joyJob == null)
            {
                continue;
            }
            joyJob.playerForced = true;
            joyJob.count = 1337;


            if (pawn.needs.joy.CurLevel > 0.60f)
            {
                    
                return new FloatMenuOption(


                    "KB_Chill_Out_Cannot_Engage".Translate().RawText.Contains("{0}") ? "KB_Chill_Out_Cannot_Engage".Translate(joyGiverDef.joyKind.label) + ": " + "KB_Chill_Out_Not_Bored".Translate().CapitalizeFirst()
                    : "KB_Chill_Out_Cannot_Engage".Translate().RawText + " " + joyGiverDef.joyKind.label + ": " + "KB_Chill_Out_Not_Bored".Translate().CapitalizeFirst(),
                    null
                );
            }
            if (!pawn.CanReach(joyTarget, PathEndMode.OnCell, Danger.Deadly))
            {
                return new FloatMenuOption(
                    "KB_Chill_Out_Cannot_Engage".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
            }
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                     "KB_Chill_Out_Engage".Translate().RawText.Contains("{0}") ? "KB_Chill_Out_Engage".Translate(joyGiverDef.joyKind.label)
                    : "KB_Chill_Out_Engage".Translate().RawText + " " + joyGiverDef.joyKind.label,
                    delegate
                    {
                        pawn.jobs.ClearQueuedJobs();  pawn.jobs.TryTakeOrderedJob(joyJob, tag: JobTag.SatisfyingNeeds);
                    }, MenuOptionPriority.High
                ),
                pawn, joyTarget

            );
        }
        return base.GetSingleOptionFor(pawn, context);
    }
    [HarmonyPatch(typeof(Pawn_TimetableTracker), "CurrentAssignment", MethodType.Getter)]
    public static class Patch_CurrentAssignment
    {
        public static void Postfix(Pawn_TimetableTracker __instance, ref TimeAssignmentDef __result)
        {
            var pawnField = typeof(Pawn_TimetableTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            Pawn pawn = (Pawn)pawnField?.GetValue(__instance);
            if ((__result == TimeAssignmentDefOf.Work && pawn.IsColonist) && (pawn.CurJob?.count == 1337 || pawn.CurJob?.def == JobDefOf.Reading))
            {
                __result = TimeAssignmentDefOf.Joy;
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
            validator = (TargetInfo targ) => targ.HasThing && joyThingDefs.Contains(targ.Thing.def)
        };
    }
}
