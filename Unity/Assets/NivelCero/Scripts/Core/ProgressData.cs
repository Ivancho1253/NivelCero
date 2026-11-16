using System;
using System.Collections.Generic;

namespace NivelCero.Core
{
    /// <summary>Serializable progress only. Engine-specific file IO belongs to the Unity save service.</summary>
    [Serializable]
    public sealed class ProgressData
    {
        public const string MainFragmentOne = "fragment-main-1";
        public const string MainFragmentTwo = "fragment-main-2";
        public const string OptionalFragment = "fragment-optional";
        public int schemaVersion = 1;
        public int checkpointId;
        public List<string> fragmentIds = new List<string>();
        public bool bossDefeated;
        public bool accessUnlocked;
        public float elapsedSeconds;

        public int FragmentCount { get { EnsureList(); return fragmentIds.Count; } }
        public bool RequiredFragmentsRecovered
        {
            get { return HasFragment(MainFragmentOne) && HasFragment(MainFragmentTwo); }
        }
        public bool CanExit { get { return RequiredFragmentsRecovered && bossDefeated; } }

        public bool HasFragment(string id)
        {
            EnsureList();
            return !String.IsNullOrEmpty(id) && fragmentIds.Contains(id);
        }

        public bool Collect(string id)
        {
            if (id != MainFragmentOne && id != MainFragmentTwo && id != OptionalFragment)
                throw new ArgumentException("Unknown fragment identifier.", "id");
            EnsureList();
            if (fragmentIds.Contains(id)) return false;
            fragmentIds.Add(id);
            return true;
        }

        /// <summary>Use after loading a file, before applying any progress to the scene.</summary>
        public bool IsValid()
        {
            if (schemaVersion != 1 || checkpointId < 0 || checkpointId > 2 ||
                Single.IsNaN(elapsedSeconds) || Single.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                return false;
            EnsureList();
            HashSet<string> unique = new HashSet<string>();
            foreach (string id in fragmentIds)
            {
                if (id != MainFragmentOne && id != MainFragmentTwo && id != OptionalFragment) return false;
                if (!unique.Add(id)) return false;
            }
            return true;
        }

        private void EnsureList() { if (fragmentIds == null) fragmentIds = new List<string>(); }
    }
}
