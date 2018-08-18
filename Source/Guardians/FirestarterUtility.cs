using RimWorld;
using Verse;

namespace Guardians
{
    [StaticConstructorOnStartup]
    public static class FirestarterUtility
    {
        private const float defaultFireSize = 0.5f;
        private static Graphic graphicInt;

        public static void StartFire(Thing hitThing, IntVec3 destinationCell)
        {
            Fire fire = (Fire)ThingMaker.MakeThing(ThingDefOf.Fire, null);
            fire.fireSize = defaultFireSize;
            if (hitThing != null)
            {
                if (hitThing is Pawn) hitThing.TryAttachFire(defaultFireSize);
                else GenSpawn.Spawn(fire, hitThing.Position, Find.CurrentMap, Rot4.North);
            }
            else
            {
                GenSpawn.Spawn(fire, destinationCell, Find.CurrentMap, Rot4.North);
            }
        }

        public static Graphic XenarrowGraphic
        {
            get
            {
                if (graphicInt == null)
                {
                    GraphicData graphicData = new GraphicData()
                    {
                        texPath = "Projectile/Xenarrow",
                        graphicClass = typeof(Graphic_Single),
                        shaderType = ShaderTypeDefOf.Transparent
                    };
                    graphicInt = graphicData.Graphic;
                }
                return graphicInt;
            }
        }
    }

}
