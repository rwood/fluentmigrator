using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentMigrator.Infrastructure.Extensions;
using FluentMigrator.Runner.Initialization;

namespace FluentMigrator.Runner
{
    public class ProfileLoader : IProfileLoader
    {
        public ProfileLoader(IRunnerContext runnerContext, IMigrationRunner runner, IMigrationConventions conventions, IEnumerable<string> tagsToMatch = null)
        {
            Runner = runner;
            Assembly = runner.MigrationAssembly;
            Profile = runnerContext.Profile;
            Conventions = conventions;
            TagsToMatch = (tagsToMatch ?? new string[] {}).ToArray();

            Initialize();
        }

        private Assembly Assembly { get; set; }
        private string Profile { get; set; }
        private IEnumerable<string> TagsToMatch { get; set; }

        protected IMigrationConventions Conventions { get; set; }
        private IMigrationRunner Runner { get; set; }

        private IEnumerable<IMigration> _profiles;

        private void Initialize()
        {
            _profiles = new List<IMigration>();

            if (!string.IsNullOrEmpty(Profile))
                _profiles = FindProfilesIn(Assembly, Profile);
        }

        public IEnumerable<IMigration> FindProfilesIn(Assembly assembly, string profile)
        {
            string[] profiles = profile.ToLower().Split('|');

            var profileClasses = assembly.GetExportedTypes().Where(type => Conventions.TypeIsProfile(type));

            return from type in profileClasses
                let profileName = type.GetOneAttribute<ProfileAttribute>().ProfileName.ToLower()
                where (profiles.Contains(profileName) || profileName == "*")
                    && (!Conventions.TypeHasTags(type) || Conventions.TypeHasMatchingTags(type, TagsToMatch))
                orderby type.FullName
                select type.Assembly.CreateInstance(type.FullName) as IMigration;
        }

        public IEnumerable<IMigration> Profiles
        {
            get
            {
                return _profiles;
            }
        }

        public void ApplyProfiles()
        {
            // Run Profile if applicable
            foreach (var profile in Profiles)
            {
                Runner.Up(profile);
            }
        }
    }
}