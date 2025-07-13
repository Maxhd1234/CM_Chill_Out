using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

[StaticConstructorOnStartup]
public class FloatMenuOptionProvider_ChillOut : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

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
            if (joyGiverDef == null)
            {
                Log.Warning("ChillOut: Could not find JoyGiverDef for " + joyTarget.Thing.def.defName + ", this should not be possible...");
                continue;
            }
            JoyGiver_InteractBuilding joyGiver_InteractBuilding = joyGiverDef.Worker as JoyGiver_InteractBuilding;
            Job joyJob = joyGiver_InteractBuilding.GetType()
                .GetMethod("TryGivePlayJob", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(joyGiver_InteractBuilding, new object[2] { pawn, joyTarget.Thing }) as Job;
            if (joyJob == null)
            {
                continue;
            }
            if (pawn.needs.joy.CurLevel > 0.75f)
            {
                return new FloatMenuOption(
                    "KB_Chill_Out_Cannot_Engage".Translate() + " " + joyGiverDef.joyKind.label + ": " + "KB_Chill_Out_Not_Bored".Translate().CapitalizeFirst(),
                    null
                );
            }
            if (!pawn.CanReach(joyTarget, PathEndMode.OnCell, Danger.Deadly))
            {
                return new FloatMenuOption(
                    "KB_Chill_Out_Cannot_Engage".Translate() + " " + joyGiverDef.joyKind.label + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
            }
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    "KB_Chill_Out_Engage".Translate() + " " + joyGiverDef.joyKind.label,
                    delegate { pawn.jobs.TryTakeOrderedJob(joyJob, JobTag.Misc); }
                ),
                pawn, joyTarget
            );
        }
        return null;
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