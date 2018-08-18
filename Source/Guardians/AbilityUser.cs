using System.Collections.Generic;
using Verse;
using RimWorld;
using AbilityUser;

namespace Guardians
{
    public interface IAnimalAbilityUser { }

    public class CompGuardian : GenericCompAbilityUser, IAnimalAbilityUser
    {
        public bool? guardian;

        // Provides ability without affecting save.
        public override void CompTick()
        {
            if (AbilityUser?.Spawned == true)
            {
                if (guardian != null)
                {
                    if (guardian == true)
                        base.CompTick();
                }
                else
                {
                    guardian = TryTransformPawn();
                    //Log.Message($"TryTransformPawn->{guardian}");
                    Initialize();
                }
            }
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            if (guardian == true)
            {
                // TODO: is there a better way to do this per guardian?
                // TODO: modExtension?
                this.AddPawnAbility(AbilityDefOf.GDN_Firebreath);
                this.AddPawnAbility(AbilityDefOf.GDN_Charge);
            }
        }

        public bool IsGuardian
        {
            get => this.AbilityUser?.def.HasModExtension<GuardianExtension>() == true;
        }

        public override bool TryTransformPawn() => IsGuardian;

    }

    [DefOf]
    public static class AbilityDefOf
    {
        public static AbilityDef GDN_Firebreath;
        public static AbilityDef GDN_Charge;
    }

    public class FirebreathSpark : Projectile_AbilityBase
    {
        protected override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            FirestarterUtility.StartFire(hitThing, DestinationCell);
        }
    }
}
