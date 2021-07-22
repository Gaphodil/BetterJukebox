using System;
using Gaphodil.BetterJukebox.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using GenericModConfigMenu;

namespace Gaphodil.BetterJukebox
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*
         * Properties
         */

        /// <summary>The mod configuration from the player.</summary>

        /// <summary>Ambience, sound effects, and other permanently disabled tracks show up in the jukebox.</summary>
        private bool ShowAmbientTracks;
        public ModConfig Config;

        /*
         * Public methods
         */

        /// <summary>
        /// The mod entry point, called after the mod is first loaded.
        /// </summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }


        /*
         * Private methods
         */

        /// <summary>
        /// Raised after a game menu is opened, closed, or replaced. 
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            ShowAmbientTracks = Config.ShowAmbientTracks;
            // replace ChooseFromListMenu (only used for jukeboxes as of 1.4) with BetterJukeboxMenu
            if (e.NewMenu is ChooseFromListMenu &&
                Helper.Reflection.GetField<bool>(e.NewMenu, "isJukebox").GetValue() == true)
            {
                ChooseFromListMenu.actionOnChoosingListOption action = 
                    Helper.Reflection.GetField<ChooseFromListMenu.actionOnChoosingListOption>(e.NewMenu, "chooseAction").GetValue();
                
                e.NewMenu.exitThisMenuNoSound(); // is this neccessary? is there a better way?

                // create default list of songs to play - apparently this is how CA hard-copied the list
                List<string> list = Game1.player.songsHeard.Distinct().ToList();

                //if (ShowUnheardTracks)
                //{
                //    AddAllTracks(list);
                //}

                // from ChooseFromListMenu constructor
                if (!ShowAmbientTracks)
                {
                    RemoveAmbience(list);
                }
                //else  // rain noises heavily based on actual weather and player location, no effect if not raining
                //{
                //    if (list.IndexOf("rain") == -1)
                //        list.Add("rain");
                //}
                list.Remove("title_day"); // this one gets removed for A Good Reason, apparently

                // this is the one change that isn't true to how the game does it, because it makes me angy >:L
                int MTIndex = list.IndexOf("MainTheme");
                if (MTIndex.Equals(0)) { }
                else {
                    if (MTIndex.Equals(-1)) { }
                    else list.RemoveAt(MTIndex);
                    list.Insert(0, "MainTheme"); 
                }

                // speculative fix for Nexus page bug report
                list.Remove("resetVariable");

                // create and activate the menu
                Game1.activeClickableMenu = new BetterJukeboxMenu(
                    list,
                    new BetterJukeboxMenu.actionOnChoosingListOption(action),
                    Helper.Content.Load<Texture2D>(
                        "assets/BetterJukeboxGraphics.png",
                        ContentSource.ModFolder
                    ),
                    key => Helper.Translation.Get(key),
                    Monitor,
                    Config,
                    Game1.player.currentLocation.miniJukeboxTrack.Value
                ); 
            }
        }

        // from https://github.com/spacechase0/StardewValleyMods/tree/develop/GenericModConfigMenu#readme
        private void OnGameLaunched(object Sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu API (if it's installed)
            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api is null)
                return;

            // register mod configuration
            api.RegisterModConfig(
                mod: ModManifest,
                revertToDefault: () => Config = new ModConfig(),
                saveToFile: () => Helper.WriteConfig(Config)
            );

            // let players configure your mod in-game (instead of just from the title screen)
            api.SetDefaultIngameOptinValue(ModManifest, true);

            // add some config options
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowMenu"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowMenuDescription"),
                optionGet: () => Config.ShowMenu,
                optionSet: value => Config.ShowMenu = value
            );
            api.RegisterPageLabel(
                ModManifest,
                Helper.Translation.Get("BetterJukebox:ListSettings"),
                Helper.Translation.Get("BetterJukebox:ListSettingsDescription"),
                Helper.Translation.Get("BetterJukebox:ListSettings")
            );
            api.RegisterPageLabel(
                ModManifest,
                Helper.Translation.Get("BetterJukebox:FunctionalSettings"),
                Helper.Translation.Get("BetterJukebox:FunctionalSettingsDescription"),
                Helper.Translation.Get("BetterJukebox:FunctionalSettings")
            );
            api.RegisterPageLabel(
                ModManifest,
                Helper.Translation.Get("BetterJukebox:VisualSettings"),
                Helper.Translation.Get("BetterJukebox:VisualSettingsDescription"),
                Helper.Translation.Get("BetterJukebox:VisualSettings")
            );

            api.StartNewPage(ModManifest, Helper.Translation.Get("BetterJukebox:ListSettings"));
            api.RegisterClampedOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:AmbientTracks"),
                optionDesc: Helper.Translation.Get("BetterJukebox:AmbientTracksDescription"),
                optionGet: () => Config.AmbientTracks,
                optionSet: value => Config.AmbientTracks = value,
                min: 0,
                max: 2,
                interval: 1
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:Blacklist"),
                optionDesc: Helper.Translation.Get("BetterJukebox:BlacklistDescription"),
                optionGet: () => Config.Blacklist,
                optionSet: value => Config.Blacklist = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:Whitelist"),
                optionDesc: Helper.Translation.Get("BetterJukebox:WhitelistDescription"),
                optionGet: () => Config.Whitelist,
                optionSet: value => Config.Whitelist = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowLockedSongs"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowLockedSongsDescription"),
                optionGet: () => Config.ShowLockedSongs,
                optionSet: value => Config.ShowLockedSongs = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowUnheardTracks"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowUnheardTracksDescription"),
                optionGet: () => Config.ShowUnheardTracks,
                optionSet: value => Config.ShowUnheardTracks = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardSoundtrack"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardSoundtrackDescription"),
                optionGet: () => Config.UnheardSoundtrack,
                optionSet: value => Config.UnheardSoundtrack = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardNamed"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardNamedDescription"),
                optionGet: () => Config.UnheardNamed,
                optionSet: value => Config.UnheardNamed = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardRandom"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardRandomDescription"),
                optionGet: () => Config.UnheardRandom,
                optionSet: value => Config.UnheardRandom = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardMisc"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardMiscDescription"),
                optionGet: () => Config.UnheardMisc,
                optionSet: value => Config.UnheardMisc = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardDupes"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardDupesDescription"),
                optionGet: () => Config.UnheardDupes,
                optionSet: value => Config.UnheardDupes = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:UnheardMusical"),
                optionDesc: Helper.Translation.Get("BetterJukebox:UnheardMusicalDescription"),
                optionGet: () => Config.UnheardMusical,
                optionSet: value => Config.UnheardMusical = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:PermanentUnheard"),
                optionDesc: Helper.Translation.Get("BetterJukebox:PermanentUnheardDescription"),
                optionGet: () => Config.PermanentUnheard,
                optionSet: value => Config.PermanentUnheard = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:PermanentBlacklist"),
                optionDesc: Helper.Translation.Get("BetterJukebox:PermanentBlacklistDescription"),
                optionGet: () => Config.PermanentBlacklist,
                optionSet: value => Config.PermanentBlacklist = value
            );

            api.StartNewPage(ModManifest, Helper.Translation.Get("BetterJukebox:FunctionalSettings"));
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:TrueRandom"),
                optionDesc: Helper.Translation.Get("BetterJukebox:TrueRandomDescription"),
                optionGet: () => Config.TrueRandom,
                optionSet: value => Config.TrueRandom = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowAlternateSorts"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowAlternateSortsDescription"),
                optionGet: () => Config.ShowAlternateSorts,
                optionSet: value => Config.ShowAlternateSorts = value
            );

            api.StartNewPage(ModManifest, Helper.Translation.Get("BetterJukebox:VisualSettings"));
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowInternalId"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowInternalIdDescription"),
                optionGet: () => Config.ShowInternalId,
                optionSet: value => Config.ShowInternalId = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: Helper.Translation.Get("BetterJukebox:ShowBandcampNames"),
                optionDesc: Helper.Translation.Get("BetterJukebox:ShowBandcampNamesDescription"),
                optionGet: () => Config.ShowBandcampNames,
                optionSet: value => Config.ShowBandcampNames = value
            );
        }

        /// <summary>
        /// Remove ambient tracks from the list of songs available in the jukebox.
        /// Copied from the ChooseFromListMenu constructor.
        /// </summary>
        /// <param name="trackList"></param>
        private void RemoveAmbience(List<string> trackList)
        {
            for (int index = trackList.Count - 1; index >= 0; --index)
            {
                if (trackList[index].ToLower().Contains("ambient") || trackList[index].ToLower().Contains("bigdrums") || trackList[index].ToLower().Contains("clubloop") || trackList[index].ToLower().Contains("ambience"))
                {
                    trackList.RemoveAt(index);
                }
                else
                {
                    switch (trackList[index])
                    {
                        case "buglevelloop": // vanilla bug: should be "bugLevelLoop"
                            trackList.RemoveAt(index);
                            continue;
                        case "coin":
                            trackList.RemoveAt(index);
                            continue;
                        case "communityCenter":
                            trackList.RemoveAt(index);
                            continue;
                        case "jojaOfficeSoundscape":
                            trackList.RemoveAt(index);
                            continue;
                        case "nightTime":
                            trackList.RemoveAt(index);
                            continue;
                        case "ocean":
                            trackList.RemoveAt(index);
                            continue;
                        default:
                            continue;
                    }
                }
            }
        }
    }
}