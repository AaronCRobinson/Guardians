using System;
using System.Collections.Generic;
using Verse;
using Harmony;
using AbilityUser;

namespace Guardians
{
    // NOTE: JecsTools is only designed for Humanlike, so we are going to patch it to work for guardians. 

    [StaticConstructorOnStartup]
    public static class CompAbilityUserPatches
    {
        // BORROWED FROM: JecsTools/Source/AllModdingComponents/CompAbilityUser/HarmonyPatches.cs
        static CompAbilityUserPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.guardians.compabilityuserpatches");

            // Initializes the AbilityUsers on Guardians
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), "InitializeComps"), null, new HarmonyMethod(typeof(CompAbilityUserPatches).GetMethod("InitializeComps_PostFix")), null);
        }

        public static void InitializeComps_PostFix(ThingWithComps __instance)
        {
            if (__instance is Pawn p) CompAbilityUserPatches.InternalAddInAbilityUsers(p);
        }

        public static void InternalAddInAbilityUsers(Pawn pawn)
        {
            if (pawn?.def.HasModExtension<GuardianExtension>() == true)
                GuardianAbilityUserUtility.TransformAnimal(pawn);
        }
    }

    public static class GuardianAbilityUserUtility
    {
        public static bool TransformAnimal(Pawn p)
        {
            bool retval = false;
            if (AbilityUserUtility.abilityUserChildren == null)
                AbilityUserUtility.abilityUserChildren = AbilityUserUtility.GetAllChildrenOf(typeof(CompAbilityUser));

            ThingComp thingComp;
            foreach (Type t in AbilityUserUtility.abilityUserChildren)
            {
                thingComp = (ThingComp)Activator.CreateInstance((t));
                if (thingComp is IAnimalAbilityUser)
                {
                    retval = true;
                    thingComp.parent = p;
                    object comps = AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
                    if (comps != null)
                        ((List<ThingComp>)comps).Add(thingComp);
                    thingComp.Initialize(null);
                }
            }
            //Log.Message($"TransformAnimal->{retval}");
            return retval;
        }
    }
}
