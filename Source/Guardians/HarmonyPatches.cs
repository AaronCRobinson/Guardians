using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace Guardians
{

    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.main");
            // TODO: revisit transpilers for performance...
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.TryGetAttackVerb)), new HarmonyMethod(typeof(HarmonyPatches), nameof(TryGetAttackVerbPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(JobGiver_Manhunter), "TryGiveJob"), new HarmonyMethod(typeof(HarmonyPatches), nameof(ManhunterTryGiveJobPrefix)), null);

        }

        public static bool TryGetAttackVerbPrefix(Pawn __instance, ref Verb __result, Thing target)
        {
            if (__instance.GetComp<CompExtraVerbs>() != null)
            {
                Log.Message("TryGetAttackVerbPrefix");
                __result = ExtraVerbsHelper.TryGetExtraVerb(__instance);
                return __result == null ? true : false;
            }
            return true;
        }

        public static bool ManhunterTryGiveJobPrefix(JobGiver_Manhunter __instance, ref Job __result, Pawn pawn)
        {
            if (pawn.GetComp<CompExtraVerbs>() != null)
            {
                Log.Message("ManhunterTryGiveJobPrefix");
                __result = __instance.TryGiveJobWithExtraVerbs(pawn);
                return false;
            }
            return true;
        }
    }

    // REFERENCE: GetUpdatedAvailableVerbsList
    static class ExtraVerbsHelper
    {
        // TODO: abstract this pattern...
        private static MethodInfo MI_FindPawnTarget = AccessTools.Method(typeof(JobGiver_Manhunter), "FindPawnTarget");
        private static MethodInfo MI_MeleeAttackJob = AccessTools.Method(typeof(JobGiver_Manhunter), "MeleeAttackJob");
        private static MethodInfo MI_FindTurretTarget = AccessTools.Method(typeof(JobGiver_Manhunter), "FindTurretTarget");

        private static Pawn FindPawnTarget(this JobGiver_Manhunter jobGiver, Pawn pawn)
        {
            return (Pawn)MI_FindPawnTarget.Invoke(jobGiver, new object[] { pawn });
        }

        private static Job MeleeAttackJob(this JobGiver_Manhunter jobGiver, Pawn pawn, Thing target)
        {
            return (Job)MI_MeleeAttackJob.Invoke(jobGiver, new object[] { pawn, target });
        }

        private static Building FindTurretTarget(this JobGiver_Manhunter jobGiver, Pawn pawn)
        {
            return (Building)MI_FindTurretTarget.Invoke(jobGiver, new object[] { pawn });
        }

        public static Job TryGiveJobWithExtraVerbs(this JobGiver_Manhunter jobGiver, Pawn pawn)
        {
            Verb verb = pawn.TryGetAttackVerb(null);
            if (verb == null)
            {
                return null;
            }
            // Here be dragons
            if (!(verb is Verb_MeleeAttack))
            {
                return new Job(JobDefOf.Wait_Combat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }
            Pawn pawn2 = jobGiver.FindPawnTarget(pawn);
            if (pawn2 != null && pawn.CanReach(pawn2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
            {
                return jobGiver.MeleeAttackJob(pawn, pawn2);
            }
            Building building = jobGiver.FindTurretTarget(pawn);
            if (building != null)
            {
                return jobGiver.MeleeAttackJob(pawn, building);
            }
            if (pawn2 != null)
            {
                using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, pawn2.Position, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false), PathEndMode.OnCell))
                {
                    Job result;
                    if (!pawnPath.Found)
                    {
                        result = null;
                        return result;
                    }
                    if (!pawnPath.TryFindLastCellBeforeBlockingDoor(pawn, out IntVec3 loc))
                    {
                        Log.Error(pawn + " did TryFindLastCellBeforeDoor but found none when it should have been one. Target: " + pawn2.LabelCap);
                        result = null;
                        return result;
                    }
                    IntVec3 randomCell = CellFinder.RandomRegionNear(loc.GetRegion(pawn.Map, RegionType.Set_Passable), 9, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), null, null, RegionType.Set_Passable).RandomCell;
                    if (randomCell == pawn.Position)
                    {
                        result = new Job(JobDefOf.Wait, 30, false);
                        return result;
                    }
                    result = new Job(JobDefOf.Goto, randomCell);
                    return result;
                }
            }
            return null;
        }

        // REFERENCE: TryGetMeleeVerb
        public static Verb TryGetExtraVerb(Pawn pawn)
        {
            //NOTE: check for comp to reduce impact
            CompExtraVerbs comp = pawn.GetComp<CompExtraVerbs>();
            if (comp != null)
            {
                // NOTE: might need to remove this first check as null is now valid...
                if (comp.curExtraVerb == null || Find.TickManager.TicksGame >= comp.curExtraVerbUpdateTick + 60 || !comp.curExtraVerb.IsStillUsableBy(pawn))
                {
                    comp.ChooseExtraVerb();
                }
#if DEBUG
            Log.Message($"TryGetExtraVerb: {pawn} -> {comp.curExtraVerb}");
#endif
                return comp.curExtraVerb;
            }
            return null;
        }
    }

}
