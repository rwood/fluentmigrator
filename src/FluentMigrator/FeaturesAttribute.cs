using System;

namespace FluentMigrator
{
    ///<summary>
    /// Used to filter which migrations are run.
    /// Place one or more FeaturesAttribute instances on a Migration class to act as 'guard' condition 
    /// that selects migrations corresponding to features to install or upgrade.
    ///</summary>
    /// <remarks>
    /// Each FeaturesAttribute instance is an OR condition, the list of names in FeaturesAttribute.FeatureNames is an AND condition.
    /// So ("ABC" && "DEF") || "GHI"  becomes:
    /// [Features("ABC", "DEF")]
    /// [Features("GHI")]
    /// public class M0001_AbcDef_or_Ghi : Migrations { ... }
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class FeaturesAttribute : Attribute
    {
        public string[] FeatureNames { get; private set; }


        public FeaturesAttribute ()  // Added only to remove a compiler warning
        {
        }

        public FeaturesAttribute(params string[] featureNames)
        {
            FeatureNames = featureNames;
        }
    }
}