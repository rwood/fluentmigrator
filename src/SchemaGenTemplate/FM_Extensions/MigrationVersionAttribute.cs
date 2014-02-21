using System;
using System.Diagnostics;

namespace Migrations.FM_Extensions
{
    /// <summary>
    /// Computes a migration number based on product version numbering + migration step
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationVersionAttribute : FluentMigrator.MigrationAttribute
    {
        public MigrationVersionAttribute(int major, int minor, int patch = 0, int step = 0)
            : base(CalculateValue(major, minor, patch, step))
        {
        }

        public const int MaxMajor = 100;
        public const int MaxMinor = 100;
        public const int MaxPatch = 10000;
        public const int MaxStep  = 1000;

        private static long CalculateValue(int major, int minor, int patch, int step)
        {
            Debug.Assert(major < MaxMajor);
            Debug.Assert(minor < MaxMinor);
            Debug.Assert(patch < MaxPatch);    // Can be app build number
            Debug.Assert(step < MaxStep);

            return ((((major * MaxMinor) + minor) * MaxPatch) + patch) * MaxStep + step;
        }

        public void GetVersion(out int major, out int minor, out int patch, out int step)
        {
            long version = Version;

            step = (int) (version % MaxStep);
            version = version / MaxStep;

            patch = (int) (version % MaxPatch);
            version = version / MaxPatch;

            minor = (int) (version % MaxMinor);
            major = (int) (version / MaxMinor);
        }

        public override string ToString()
        {
            int major, minor, patch, step;
            GetVersion(out major, out minor, out patch, out step);
            return string.Format("{0}-{1}-{2}-{3}", major, minor, patch, step);
        }

        public string ToStringZeroFilled()
        {
            int major, minor, patch, step;
            GetVersion(out major, out minor, out patch, out step);
            return string.Format("{0:D2}-{1:D2}-{2:D4}-{3:D3}", major, minor, patch, step);
        }
    }
}
