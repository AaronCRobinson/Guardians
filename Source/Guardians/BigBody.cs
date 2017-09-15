using System;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace Guardians
{
    // NOTE: this probably won't be needed in the future (as either I am doing something wrong or Tynan will fix)

    [StaticConstructorOnStartup]
    class BigBodyPatches
    {
        static BigBodyPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.bigbody");
            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryFindShootLineFromTo)), new HarmonyMethod(typeof(BigBodyPatches), nameof(TryFindShootLineFromToPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(ReachabilityImmediate), nameof(ReachabilityImmediate.CanReachImmediate), new Type[] {typeof(IntVec3), typeof(LocalTargetInfo), typeof(Map), typeof(PathEndMode), typeof(Pawn)}), new HarmonyMethod(typeof(BigBodyPatches), nameof(CanReachImmediatePrefix)), null);

            #region debugging
#if DEBUG
            harmony.Patch(AccessTools.Method(typeof(Verb), "TryCastNextBurstShot"), new HarmonyMethod(typeof(HarmonyPatches), nameof(TryCastNextBurstShotPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryStartCastOn)), new HarmonyMethod(typeof(HarmonyPatches), nameof(TryStartCastOnPrefix)), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryMeleeAttack)), new HarmonyMethod(typeof(HarmonyPatches), nameof(TryMeleeAttackPrefix)), null);
#endif
        }

#if DEBUG
        public static void TryCastNextBurstShotPrefix(Verb __instance) { Log.Message($"TryCastNextBurstShot {__instance}"); }
        public static void TryStartCastOnPrefix(Verb __instance) { Log.Message($"TryStartCastOn {__instance}"); }
        public static void TryMeleeAttackPrefix() { Log.Message("TryMeleeAttack"); }
#endif
#endregion debugging

        public static bool TryFindShootLineFromToPrefix(Verb __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            Pawn pawn = (Pawn)__instance.caster;
            resultingLine = new ShootLine(root, targ.Cell);
            if (BigBodyHelper.IsBigBody(pawn) && __instance.verbProps.MeleeRange)
            {
                __result = BigBodyHelper.CanReachImmediate(root, targ, pawn.Map, PathEndMode.Touch, pawn);
                return false;
            }
            return true;
        }

        public static bool CanReachImmediatePrefix(ref bool __result, IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Pawn pawn)
        {
            if (BigBodyHelper.IsBigBody(pawn) && peMode == PathEndMode.Touch)
            {
                __result = BigBodyHelper.CanReachImmediate(start, target, map, peMode, pawn);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Helper for working with 'big body' pawns.
    /// </summary>
    public static class BigBodyHelper
    {
        private const float bigBodyMin = 2.5f;

        /// <summary>
        /// Checks if a pawn is 'big body'
        /// </summary>
        public static bool IsBigBody(Pawn pawn)
        {
            return pawn != null && pawn.BodySize > bigBodyMin;
        }

        /// <summary>
        /// Used to redirect ReachabilityImmediate.CanReachImmediate to handle 'big body' pawns
        /// </summary>
        public static bool CanReachImmediate(IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Pawn pawn)
        {
            if (!target.IsValid)
            {
                return false;
            }
            target = (LocalTargetInfo)GenPath.ResolvePathMode(pawn, target.ToTargetInfo(map), ref peMode);
            if (target.HasThing)
            {
                Thing thing = target.Thing;
                if (!thing.Spawned)
                {
                    if (pawn != null)
                    {
                        if (pawn.carryTracker.innerContainer.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.inventory.innerContainer.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.apparel != null && pawn.apparel.Contains(thing))
                        {
                            return true;
                        }
                        if (pawn.equipment != null && pawn.equipment.Contains(thing))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                if (thing.Map != map)
                {
                    return false;
                }
            }

            // NOTE: can this be better?
            // ASSUME: pawns are square
            //bool canReach = pawn.BodySize / 2 >= start.DistanceTo(target.Cell);
            bool canReach = pawn.def.Size.x / 2 >= start.DistanceTo(target.Cell);
#if DEBUG
            Log.Message($"BigBodyHelper.CanReachImmediate: {canReach}");
#endif
            return canReach;
        }

    }
}
