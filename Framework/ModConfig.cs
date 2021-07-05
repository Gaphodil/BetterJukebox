namespace Gaphodil.BetterJukebox.Framework
{
    class ModConfig
    {
        /// <summary>
        /// Internal music identifiers are displayed alongside the regular music name.
        /// </summary>
        public bool ShowInternalId { get; set; } = false;

        /// <summary>
        /// Ambience, sound effects, and other permanently disabled tracks show up in the jukebox.
        /// </summary>
        public bool ShowAmbientTracks { get; set; } = false;

        /// <summary>
        /// Songs not yet heard on the current save file can be found in the jukebox.
        /// WARNING: may add songs to your "heard songs" list.
        /// </summary>
        //public bool ShowUnheardTracks { get; set; } = false;

        /// <summary>
        /// Non-default sorting options are enabled.
        /// </summary>
        //public bool ShowAlternateSorts { get; set; } = true;
    }
}
