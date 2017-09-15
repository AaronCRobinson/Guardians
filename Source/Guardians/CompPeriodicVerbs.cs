using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Harmony;

namespace Guardians
{

    [StaticConstructorOnStartup]
    class CompPeriodicPatches
    {
        static CompPeriodicPatches()
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.compperiodic");
            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.IsStillUsableBy)), new HarmonyMethod(typeof(CompPeriodicPatches), nameof(IsStillUsableByPrefix)), null);

            // NOTE: this may be overkill and everything is going through `TryCastNextBurstShot`
            foreach (Type t in typeof(Verb).AllSubclassesFirstNonAbstract())
                if (t != typeof(Verb_Ignite) && t != typeof(Verb_BeatFire))
                    harmony.Patch(AccessTools.Method(t, "TryCastShot"), null, new HarmonyMethod(typeof(CompPeriodicPatches), nameof(TryCastShotPostfix)));

            #region debugging
#if DEBUG
            harmony.Patch(AccessTools.Method(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryGetMeleeVerb)), new HarmonyMethod(typeof(HarmonyPatches), nameof(TryGetMeleeVerbPrefix)), null);
#endif
        }

#if DEBUG
        public static void TryGetMeleeVerbPrefix() { Log.Message("TryGetMeleeVerb"); }
#endif
#endregion debugging

        /// <summary>
        /// Postfixes TryCastShot to place verbs on hold if not primary
        /// </summary>
        public static void TryCastShotPostfix(Verb __instance)
        {
            // ASSUME: we can use `isPrimary` to find our special verbs (also `hasStandardCommand` could work too)
            if (!__instance.verbProps.isPrimary)
            {
                __instance.CasterPawn.GetComp<CompPeriodicVerbs>()?.PlaceOnHold(__instance);
            }
        }

        /// <summary>
        /// Prefixes Verb.IsStillUsableBy to skip verbs on hold 
        /// </summary>
        /// <returns>Skips return, defaulting to false if on hold</returns>
        public static bool IsStillUsableByPrefix(Verb __instance, Pawn pawn)
        {
#if DEBUG
            Log.Message($"IsStillUsableBy: {__instance}");
#endif
            if (pawn.GetComp<CompPeriodicVerbs>()?.OnHold(__instance) == true)
                return false;
            return true;
        }

    }

    /// <summary>
    /// Component which allows a pawn's verbs to be placed on hold for a period of time
    /// </summary>

    // NOTE: consider special compprops to stash variables? (not sure how yet...)
    public class CompPeriodicVerbs : ThingComp
    {
        // RimWorld.Pawn_MeleeVerbs.TryGetMeleeVerb()
        private const int verbUpdateTickInterval = 60;
        private Dictionary<Verb, int> onHold = new Dictionary<Verb, int>();

        // PERFORMANCE
        private static List<Verb> removing = new List<Verb>();

        // TODO: this seems terribly inefficent... fix that latter...
        public override void CompTick()
        {
            if (this.parent.IsHashIntervalTick(verbUpdateTickInterval))
            {
                base.CompTick();

                if (this.onHold.Count > 0)
                {
                    foreach (KeyValuePair<Verb, int> entry in this.onHold)
                        if (entry.Value < Find.TickManager.TicksGame)
                            removing.Add(entry.Key);

                    foreach (Verb v in removing)
                        this.onHold.Remove(v);
                }
            }
        }

        /// <summary>
        /// Checks if a verb is on hold
        /// </summary>
        /// <param name="verb">Verb being checked</param>
        /// <returns></returns>
        public bool OnHold(Verb verb)
        {
            return this.onHold.ContainsKey(verb);
        }

        /// <summary>
        /// Place a verb on hold
        /// </summary>
        /// <param name="verb">Verb being placed on hold</param>
        public void PlaceOnHold(Verb verb)
        {
            // ASSUME: our wait is hidden in `burstShotCount`
            this.onHold.Add(verb, Find.TickManager.TicksGame + verb.verbProps.burstShotCount * 60);
        }

        /// <summary>
        /// Place a verb on hold with specific wait
        /// </summary>
        /// <param name="verb">Verb being placed on hold</param>>
        /// <param name="wait">Number of seconds for verb to be on hold</param>
        public void PlaceOnHold(Verb verb, int wait)
        {
            this.onHold.Add(verb, Find.TickManager.TicksGame + wait * 60);
        }
    }

    /// <summary>
    /// Component properties to allow for periodic verbs
    /// </summary>
    public class CompProperties_PeriodicVerbs : CompProperties
    {
        public CompProperties_PeriodicVerbs()
        {
            this.compClass = typeof(CompPeriodicVerbs);
        }
    }
}
