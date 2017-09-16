using System;
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
        private static MethodInfo MI_TryGetExtraVerb = AccessTools.Method(typeof(ExtraVerbsHelper), nameof(ExtraVerbsHelper.TryGetExtraVerb));
        //private static MethodInfo MI_TryGetExtraVerbJob = AccessTools.Method(typeof(ExtraVerbsHelper), nameof(ExtraVerbsHelper.TryGetExtraVerbJob));

        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.main");
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.TryGetAttackVerb)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TryGetAttackVerbTranspiler)));
            //harmony.Patch(AccessTools.Method(typeof(JobGiver_Manhunter), "TryGiveJob"), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(ManhunterTryGiveJobTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(JobGiver_Manhunter), "TryGiveJob"), new HarmonyMethod(typeof(HarmonyPatches), nameof(ManhunterTryGiveJobPrefix)), null);

        }

        public static IEnumerable<CodeInstruction> TryGetAttackVerbTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool firstTime = true;
            for (int i = 0; i < instructionList.Count; i++)
            {
                yield return instructionList[i];
                if (instructionList[i].opcode == OpCodes.Ret && firstTime)
                {
                    i++;
                    // Fix labels (or else the injected code will be skipped)
                    List<Label> juggledLabels = instructionList[i].labels;

                    yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = juggledLabels };
                    // NOTE: storing this as a variable did not work (unsure why)
                    yield return new CodeInstruction(OpCodes.Call, MI_TryGetExtraVerb);

                    // branch (if)
                    Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse, @continue);

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, MI_TryGetExtraVerb);
                    yield return new CodeInstruction(OpCodes.Ret);

                    // end if
                    CodeInstruction instruction = new CodeInstruction(instructionList[i].opcode, instructionList[i].operand);
                    instruction.labels.Add(@continue);
                    yield return instruction;

                    firstTime = false;
                }
            }
        }

        public static bool ManhunterTryGiveJobPrefix(JobGiver_Manhunter __instance, ref Job __result, Pawn pawn)
        {
            if (pawn.GetComp<CompExtraVerbs>() != null)
            {
                __result = __instance.TryGiveJobWithExtraVerbs(pawn);
                return false;
            }
            return true;
        }

        // NOTE: a prefix with a hard detour may be easier/better
        /*public static IEnumerable<CodeInstruction> ManhunterTryGiveJobTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool firstTime = true;
            for (int i = 0; i < instructionList.Count; i++)
            {
                yield return instructionList[i];
                if (instructionList[i].opcode == OpCodes.Ret && firstTime)
                {
                    i++;
                    // Fix labels (or else the injected code will be skipped)
                    List<Label> juggledLabels = instructionList[i].labels;

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, MI_TryGetExtraVerbJob);

                    // end if
                    CodeInstruction instruction = new CodeInstruction(instructionList[i].opcode, instructionList[i].operand);
                    //instruction.labels.Add(@continue);
                    yield return instruction;

                    firstTime = false;
                }
            }
        }*/

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
            Verb verb = pawn.TryGetAttackVerb(false);
            if (verb == null)
            {
                return null;
            }
            // Here be dragons
            if (!(verb is Verb_MeleeAttack))
            {
                return new Job(JobDefOf.WaitCombat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
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

        /*public static Job TryGetExtraVerbJob(JobGiver_Manhunter jobGiver, Pawn pawn)
        {

        }*/

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
