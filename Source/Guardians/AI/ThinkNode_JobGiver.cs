using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using AbilityUserAI;

namespace Guardians.AI
{
    public class JobGiver_Guardian : ThinkNode_JobGiver
    {
        public override float GetPriority(Pawn pawn)
        {
            Log.Message($"{pawn}->{pawn.def}");
            if (pawn?.def.HasModExtension<GuardianExtension>() == false)
                return -100f;
            return 100;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            //Log.Message($"JobGiver_Guardian->TryGiveJob->{pawn}");

            //Do we have at least one elegible profile?
            IEnumerable<AbilityUserAIProfileDef> profiles = pawn.EligibleAIProfiles();

            /*StringBuilder builder = new StringBuilder("profiles = ");
            foreach(AbilityUserAIProfileDef profile in profiles)
            {
                builder.Append(profile.defName + ", ");
            }
            Log.Message(builder.ToString());*/
            if (profiles != null && profiles.Count() > 0)
                Log.Message("BANG! BANG! BANG!");
            //No Job to give.
            return null;
        }
    }
}
