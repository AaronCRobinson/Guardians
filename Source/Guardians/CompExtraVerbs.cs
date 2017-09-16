using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Guardians
{
    public class CompExtraVerbs : ThingComp
    {
        // NOTE: consider getters here...
        public Verb curExtraVerb;
        public int curExtraVerbUpdateTick;
        private Pawn pawn;

        // PERFORMANCE
        private static List<VerbEntry> extraVerbs = new List<VerbEntry>();

        // REFERENCE: RimWorld.Pawn_MeleeVerbs
        public void ChooseExtraVerb()
        {
#if DEBUG
            Log.Message($"ChooseExtraVerb:");
#endif
            List<VerbEntry> updatedAvailableVerbsList = this.GetUpdatedAvailableVerbsList();
            if (updatedAvailableVerbsList.Count == 0)
                this.SetCurExtraVerb(null);
            else
                this.SetCurExtraVerb(updatedAvailableVerbsList.RandomElementByWeight((VerbEntry ve) => ve.SelectionWeight).verb);
        }

        // REFERENCE: RimWorld.Pawn_MeleeVerbs
        private void SetCurExtraVerb(Verb v)
        {
            this.curExtraVerb = v;
            if (Current.ProgramState != ProgramState.Playing) //why?
            {
                this.curExtraVerbUpdateTick = 0;
            }
            else
            {
                this.curExtraVerbUpdateTick = Find.TickManager.TicksGame;
            }
        }

        protected Pawn Pawn
        {
            get
            {
                if (this.pawn == null)
                {
                    this.pawn = (Pawn)this.parent;
                }
                return this.pawn;
            }
        }


        // REFERENCE: RimWorld.Pawn_MeleeVerbs
        public List<VerbEntry> GetUpdatedAvailableVerbsList()
        {
            CompExtraVerbs.extraVerbs.Clear();
            // NOTE: do not care right now (only dealing with no equipment pawns)
            /*if (this.pawn.equipment != null && this.pawn.equipment.Primary != null)
            {
                Verb verb = this.pawn.equipment.PrimaryEq.AllVerbs.Find((Verb x) => x is Verb_MeleeAttack);
                if (verb != null)
                {
                    CompExtraVerbs.extraVerbs.Add(new VerbEntry(verb, this.pawn, this.pawn.equipment.Primary));
                    return CompExtraVerbs.extraVerbs;
                }
            }*/
            List<Verb> allVerbs = this.Pawn.VerbTracker.AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                if (!(allVerbs[i] is Verb_MeleeAttack) && allVerbs[i].IsStillUsableBy(this.Pawn))
                {
                    CompExtraVerbs.extraVerbs.Add(new VerbEntry(allVerbs[i], this.Pawn, null));
                }
            }
            // NOTE: do not care right now (not doing extra abilities in hediffs)
            /*foreach (Verb current in this.pawn.health.hediffSet.GetHediffsVerbs())
            {
                if (current is Verb_MeleeAttack && current.IsStillUsableBy(this.pawn))
                {
                    CompExtraVerbs.extraVerbs.Add(new VerbEntry(current, this.pawn, null));
                }
            }*/
#if DEBUG
            Log.Message($"GetUpdatedAvailableVerbsList: {CompExtraVerbs.extraVerbs.NullOrEmpty()}");
#endif
            return CompExtraVerbs.extraVerbs;
        }

    }

    public class CompProperties_ExtraVerbs : CompProperties
    {
        public CompProperties_ExtraVerbs()
        {
            this.compClass = typeof(CompExtraVerbs);
        }
    }
}
