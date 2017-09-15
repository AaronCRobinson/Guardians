using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace Guardians
{
    //public abstract 

    public class Verb_Charge : Verb
    {
        protected override bool TryCastShot()
        {
            Log.Message("I'm charging muaw lazers");
            base.CasterPawn.health.AddHediff(HediffDefOf.GDN_Charge);
            return true;
        }
    }

    [DefOf]
    public static class HediffDefOf
    {
        public static HediffDef GDN_Charge;
    }
}
