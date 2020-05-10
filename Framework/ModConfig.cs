namespace Gaphodil.BetterJukebox.Framework
{
    class ModConfig
    {
        /// <summary>
        /// Whether internal music identifiers are displayed alongside the regular music name.
        /// </summary>
        public bool ShowInternalID { get; set; } = false;

        /// <summary>
        /// Whether ambience, sound effects, and other permanently disabled tracks show up in the jukebox.
        /// </summary>
        public bool ShowAmbientTracks { get; set; } = false;

        /// <summary>
        /// Whether only songs heard on the current save file can be found in the jukebox.
        /// WARNING: may add songs to your "heard songs" list.
        /// </summary>
        //public bool ShowUnheardTracks { get; set; } = false;
    }
}
