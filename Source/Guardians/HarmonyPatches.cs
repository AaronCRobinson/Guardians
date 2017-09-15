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
        private static MethodInfo MI_TryGetExtraVerb = AccessTools.Method(typeof(VerbHelper), nameof(VerbHelper.TryGetExtraVerb));

        static HarmonyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.main");
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.TryGetAttackVerb)), null, null, new HarmonyMethod(typeof(HarmonyPatches), nameof(TryGetAttackVerbTranspiler)));
        }

        public static IEnumerable<CodeInstruction> TryGetAttackVerbTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            bool firstTime = true;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ret && firstTime)
                {
                    // Fix label (or else the injected code will be skipped)
                    Label @continue = instructionList[i].labels.First(); // ASSUME: only one here
                    yield return new CodeInstruction(instructionList[i].opcode, instructionList[i].operand);

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, MI_TryGetExtraVerb);
                    yield return new CodeInstruction(OpCodes.Stloc_0); // open to change
                    yield return new CodeInstruction(OpCodes.Ldloc_0);

                    // branch (if)
                    //Label @continue = il.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse, @continue);

                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ret);

                    // end if
                    instructionList[++i].labels.Add(@continue);
                    yield return instructionList[i];

                    firstTime = false;
                }
                else
                    yield return instructionList[i];
            }
        }
    }

    // REFERENCE: GetUpdatedAvailableVerbsList
    static class VerbHelper
    {
        // REFERENCE: TryGetMeleeVerb
        public static Verb TryGetExtraVerb(Pawn pawn)
        {
#if DEBUG
            Log.Message($"TryGetExtraVerb:");
#endif
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
