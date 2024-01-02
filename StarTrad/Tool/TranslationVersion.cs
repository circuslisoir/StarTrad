namespace StarTrad.Tool
{
    /// <summary>
    /// Parses the version number of a translation, such as "3.22.0-1.0-20231226".
    /// </summary>
    internal class TranslationVersion
    {
        public TranslationVersion(string part0, string part1, string part2)
        {
            this.TargetedGameVersion = part0;
            this.VersionNumber = part1;
            this.BuildNumber = part2;
        }

        #region Static

        public static TranslationVersion? Make(string version)
        {
            string[] parts = version.Trim().Split('-');

            if (parts.Length != 3)
            {
                return null;
            }

            return new TranslationVersion(parts[0], parts[1], parts[2]);
        }

        #endregion

        #region Public

        public bool Equals(TranslationVersion other)
        {
            return this.TargetedGameVersion == other.TargetedGameVersion
                && this.VersionNumber == other.VersionNumber
                && this.BuildNumber == other.BuildNumber;
        }

        public bool IsNewerThan(TranslationVersion other)
        {
            int thisBuildNumber;
            int otherBuildNumber;

            if (!int.TryParse(this.BuildNumber, out thisBuildNumber)
            || !int.TryParse(other.BuildNumber, out otherBuildNumber))
            {
                return false;
            }

            return thisBuildNumber > otherBuildNumber;
        }

        #endregion

        #region Accessor

        /// <summary>
        /// Returns the version of the game the translations is made for, like "3.22.0".
        /// </summary>
        public string TargetedGameVersion
        {
            get; internal set;
        }

        /// <summary>
        /// Returns the translation's version number, such as "1.0".
        /// </summary>
        public string VersionNumber
        {
            get; internal set;
        }

        /// <summary>
        /// Returns the translation's build number, such as "20231226".
        /// </summary>
        public string BuildNumber
        {
            get; internal set;
        }

        #endregion
    }
}
