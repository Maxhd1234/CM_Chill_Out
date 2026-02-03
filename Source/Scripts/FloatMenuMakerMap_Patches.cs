using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public class ChillOut : ModSettings
{
    public float fckICE = 0.6f; 

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref fckICE, "fckICE", 0.5f);
    }
}


public class FloatMenuOptionProvider_Swim : FloatMenuOptionProvider
{

    protected override bool Drafted => false;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOption(FloatMenuContext context)
    {
        if (ModsConfig.IsActive("ludeon.rimworld.odyssey") && ModLister.OdysseyInstalled)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn.needs == null || pawn.needs.joy == null)
            {
                return base.GetSingleOption(context);
            }
            IntVec3 swimCell = context.ClickedCell;
            if (!SwimPathFinder.TryFindSwimPath(pawn, swimCell, out var result3))
            {
                return base.GetSingleOption(context);
            }

            Job job = JobMaker.MakeJob(JobDefOf.GoSwimming, result3[0]);
            job.targetQueueA = new List<LocalTargetInfo>();
            for (int i = 1; i < result3.Count; i++)
            {
                job.targetQueueA.Add(result3[i]);
            }
            job.locomotionUrgency = LocomotionUrgency.Walk;
            job.playerForced = true;
            if (swimCell.GetTerrain(pawn.Map).IsWater)
            {
                if (pawn.Map.mapTemperature.OutdoorTemp <= 10f && NextDestIsOutdoorsAndNotEnjoyable(pawn.Map, job))
                {
                    return new FloatMenuOption("KT_Chill_Out_Water_Too_Cold".Translate().CapitalizeFirst(), null);
                }
                if (NextDestIsOutdoorsAndNotEnjoyable(pawn.Map, job))
                {
                    return new FloatMenuOption("KT_Chill_Out_Cannot_Swim".Translate().CapitalizeFirst(), null);
                }

                else
                {
                    return new FloatMenuOption("KT_Chill_Out_Swim".Translate().CapitalizeFirst(), delegate
                    {
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced, jobGiver: job.jobGiver);
                    }, MenuOptionPriority.High);
                }
            }
            else
            {
                return base.GetSingleOption(context);
            }
        }
        else
        {
            return base.GetSingleOption(context);
        }
    }
    bool NextDestIsOutdoorsAndNotEnjoyable(Map map, Job job)
    {
        if (map == null)
        {
            return false;
        }
        if (!job.targetA.IsValid)
        {
            return false;
        }
        Room room = job.targetA.Cell.GetRoom(map);
        if (room == null || !room.PsychologicallyOutdoors)
        {
            return false;
        }
        return !JoyGiver_GoSwimming.HappyToSwimOutsideOnMap(map);
    }
}

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

        foreach (LocalTargetInfo joyTarget in GenUI.TargetsAt(thing.DrawPos, ForJoying(joyThingDefs)))
        {
            if (!joyThingDefs.Contains(joyTarget.Thing.def))
            {
                Log.Warning("ChillOut: joyThingDefs does not contain " + joyTarget.Thing.def);
                continue;
            }

            JoyGiverDef joyGiverDef = list.Find(gd => gd.thingDefs.Contains(joyTarget.Thing.def));

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
                .GetMethod("TryGivePlayJob", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (tryGivePlayJobMethod == null)
            {
                Log.Warning("ChillOut: Didnt find method TryGivePlayJob_" + joyGiver_InteractBuilding);
                continue;
            }

            Job joyJob = tryGivePlayJobMethod.Invoke(joyGiver_InteractBuilding, new object[2] { pawn, joyTarget.Thing }) as Job;
            if (joyJob == null)
            {
                continue;
            }
            joyJob.playerForced = true;
            joyJob.count = 1337;

            if (pawn.needs.joy.CurLevel > LoadedModManager.GetMod<ChillOutMod>().GetSettings<ChillOut>().fckICE)
            {
                
                return new FloatMenuOption(


                    "KT_Chill_Out_Cannot_Engage".Translate().RawText.Contains("{0}") ? "KT_Chill_Out_Cannot_Engage".Translate(joyGiverDef.joyKind.label) + ": " + "KT_Chill_Out_Not_Bored".Translate().CapitalizeFirst()
                    : "KT_Chill_Out_Cannot_Engage".Translate().RawText + " " + joyGiverDef.joyKind.label + ": " + "KT_Chill_Out_Not_Bored".Translate().CapitalizeFirst(),
                    null
                );
            }
            if (!pawn.CanReach(joyTarget, PathEndMode.OnCell, Danger.Deadly))
            {
                return new FloatMenuOption(
                    "KT_Chill_Out_Cannot_Engage".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
            }
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                     "KT_Chill_Out_Engage".Translate().RawText.Contains("{0}") ? "KT_Chill_Out_Engage".Translate(joyGiverDef.joyKind.label)
                    : "KT_Chill_Out_Engage".Translate().RawText + " " + joyGiverDef.joyKind.label,
                    delegate
                    {
                        pawn.jobs.ClearQueuedJobs(); pawn.jobs.TryTakeOrderedJob(joyJob, tag: JobTag.SatisfyingNeeds);
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
            if (pawn.CurJob == null)
                return;
            if (__result == TimeAssignmentDefOf.Work && pawn != null && pawn.IsColonist && (pawn.CurJob?.count == 1337 || pawn.CurJob?.def == JobDefOf.Reading && pawn.CurJob.playerForced || !ModLister.OdysseyInstalled ? (pawn.CurJob.playerForced && pawn.CurJob?.def == JobDefOf.GoSwimming) : false))
            {
                __result = TimeAssignmentDefOf.Joy;
            }
        }
    }

    private static TargetingParameters ForJoying(List<ThingDef> joyThingDefs)
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
