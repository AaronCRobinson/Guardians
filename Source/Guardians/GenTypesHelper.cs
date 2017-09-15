using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Guardians
{
    // SOURCE: https://stackoverflow.com/questions/2742951/get-child-classes-from-base-class
    // NOTE: Tynan covered our asses (Verse.GenTypes.AllSubclassesNonAbstract)
    // TODO: allow predicate filter?
    public static class GenTypesHelper
    {
        // Verse.GenTypes
        public static IEnumerable<Type> AllSubclassesFirstNonAbstract(this Type baseType)
        {
            return from x in GenTypes.AllTypes
                   where x.IsSubclassOf(baseType) && !x.IsAbstract && x.BaseType.IsAbstract
                   select x;
        }
    }
}
