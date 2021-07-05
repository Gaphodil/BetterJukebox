﻿using System;
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
        private ModConfig Config;

        /// <summary>Internal music identifiers are displayed alongside the regular music name.</summary>
        private bool ShowInternalId;

        /// <summary>Ambience, sound effects, and other permanently disabled tracks show up in the jukebox.</summary>
        private bool ShowAmbientTracks;

        /// <summary>Songs not yet heard on the current save file can be found in the jukebox.</summary>
        //private bool ShowUnheardTracks;

        /// <summary>Non-default sorting options are enabled.</summary>
        //private bool ShowAlternateSorts;

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
            // get config values here to reflect potential changes via GenericModConfigMenu
            ShowInternalId = Config.ShowInternalId;
            ShowAmbientTracks = Config.ShowAmbientTracks;
            //ShowUnheardTracks = Config.ShowUnheardTracks;
            //ShowAlternateSorts = Config.ShowAlternateSorts;

            // replace ChooseFromListMenu (only used for jukeboxes as of 1.4) with BetterJukeboxMenu
            if (e.NewMenu is ChooseFromListMenu &&
                Helper.Reflection.GetField<bool>(e.NewMenu, "isJukebox").GetValue() == true)
            {
                ChooseFromListMenu.actionOnChoosingListOption action = 
                    Helper.Reflection.GetField<ChooseFromListMenu.actionOnChoosingListOption>(e.NewMenu, "chooseAction").GetValue();
                
                e.NewMenu.exitThisMenuNoSound(); // is this neccessary? is there a better way?

                // create default list of songs to play - apparently this is how CA hard-copied the list
                List<string> list = Game1.player.songsHeard.Distinct<string>().ToList<string>();

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
                    Game1.player.currentLocation.miniJukeboxTrack.Value,
                    ShowInternalId
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
                optionName: "Show Internal ID",
                optionDesc: "Internal music identifiers are displayed alongside the regular music name.",
                optionGet: () => Config.ShowInternalId,
                optionSet: value => Config.ShowInternalId = value
            );
            api.RegisterSimpleOption(
                mod: ModManifest,
                optionName: "Show Ambient Tracks",
                optionDesc: "Ambience, sound effects, and other permanently disabled tracks show up in the jukebox.",
                optionGet: () => Config.ShowAmbientTracks,
                optionSet: value => Config.ShowAmbientTracks = value
            );
            //api.RegisterSimpleOption(
            //    mod: this.ModManifest,
            //    optionName: "Show Unheard Tracks",
            //    optionDesc: "Songs not yet heard on the current save file can be found in the jukebox.",
            //    optionGet: () => this.Config.ShowUnheardTracks,
            //    optionSet: value => this.Config.ShowUnheardTracks = value
            //);
            //api.RegisterSimpleOption(
            //    mod: this.ModManifest,
            //    optionName: "Show Alternate Sorts",
            //    optionDesc: "Non-default sorting options are enabled.",
            //    optionGet: () => this.Config.ShowAlternateSorts,
            //    optionSet: value => this.Config.ShowAlternateSorts = value
            //);
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