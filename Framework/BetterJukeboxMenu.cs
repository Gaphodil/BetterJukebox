using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace Gaphodil.BetterJukebox.Framework
{
    /// <summary>The menu which lets the player choose a song to play.</summary>
    public class BetterJukeboxMenu : IClickableMenu
    {
        /*
         * Attributes
         */

        // ---- The parts from ShopMenu:

        /// <summary>The list of visible options to choose from.</summary>
        public List<ClickableComponent> VisibleOptions = new List<ClickableComponent>();

        /// <summary>The scroll-up button.</summary>
        public ClickableTextureComponent UpArrow;

        /// <summary>The scroll-down button.</summary>
        public ClickableTextureComponent DownArrow;

        /// <summary>The scroll bar.</summary>
        public ClickableTextureComponent ScrollBar;

        /// <summary>The area the scroll bar can move in.</summary>
        private Rectangle ScrollBarRunner;

        /// <summary>Whether the scrollbar is currently held down.</summary>
        private bool IsScrolling;

        // ---- The parts from ChooseFromListMenu:

        /// <summary>The list of songs to display.</summary>
        private List<string> Options = new List<string>();

        /// <summary>The index of the currently active selection from the Options.</summary>
        private int SelectedIndex;

        /// <summary>The lowest visible index on the menu.</summary>
        private int LowestVisibleIndex;

        /// <summary>The method that will be called when a button is pressed.</summary>
        private readonly BetterJukeboxMenu.actionOnChoosingListOption ChooseAction;

        // ---- Other parts:

        /// <summary>The index of the currently playing song.</summary>
        private int PlayingIndex;

        /// <summary>The play button.</summary>
        public ClickableTextureComponent PlayButton;

        /// <summary>The stop button.</summary>
        public ClickableTextureComponent StopButton;

        /// <summary>The number of visible Options.</summary>
        private const int _itemsPerPage = 7;

        /// <summary>The number of pixels between visual elements.</summary>
        private const int _spacingPixels = 32;

        private const int _textureBoxBorderWidth = 4 * 4;

        /// <summary>The size of the longest song name, and width of the visible options section.</summary>
        private readonly int _longestNameWidth;

        /// <summary>The height of the visible options section of the menu.</summary>
        private const int _visibleOptionsHeight = _spacingPixels * 2 * _itemsPerPage;

        private int VisibleOptionsXPositionOnScreen;

        private int VisibleOptionsYPositionOnScreen;

        /// <summary>The play and stop button graphics as a tilesheet.</summary>
        private readonly Texture2D _BetterJukeboxGraphics;

        /// <summary>Whether internal music identifiers are displayed alongside the regular music name.</summary>
        private readonly bool _showInternalID = false;

        /// <summary>The width of the menu.</summary>
        public const int w = 1050;

        /// <summary>The height of the menu.</summary>
        public const int h = _spacingPixels * 4 + _visibleOptionsHeight;

        // ---- SMAPI tools:

        /// <summary>The function to retrieve a translation from a file.</summary>
        private readonly Func<string, Translation> GetTranslation;

        /// <summary>The SMAPI Monitor for logging messages.</summary>
        private readonly IMonitor Monitor;

        /* 
         * Public methods
         */

        /// <summary>Construct an instance.</summary>
        /// <param name="options">The list of songs to display.</param>
        /// <param name="chooseAction">The method that will be called when a button is pressed.</param>
        public BetterJukeboxMenu(
            List<string> options,
            BetterJukeboxMenu.actionOnChoosingListOption chooseAction,
            Texture2D graphics,
            Func<string, Translation> getTranslation,
            IMonitor monitor,
            string defaultSelection = "",
            bool showInternalID = false)
            : base (
                Game1.viewport.Width  / 2 - (w + borderWidth * 2) / 2, 
                Game1.viewport.Height / 2 - (h + borderWidth * 2) / 2, 
                w + borderWidth * 2, 
                h + borderWidth * 2,
                true)
        {
            // assign parameters
            Options = options;
            ChooseAction = chooseAction;
            _BetterJukeboxGraphics = graphics;
            GetTranslation = getTranslation;
            Monitor = monitor;

            _showInternalID = showInternalID;

            SelectedIndex = Options.IndexOf(defaultSelection);
            if (Game1.player.currentLocation.miniJukeboxTrack.Value.Equals("")) // no active mini-jukebox 
            {
                if (Game1.startedJukeboxMusic) // hypothetically only ever true in the saloon
                {
                    PlayingIndex = Options.IndexOf(Game1.getMusicTrackName());
                    SelectedIndex = PlayingIndex;
                    Monitor.Log("Found active saloon jukebox!");
                }
                else
                {
                    PlayingIndex = -1;
                    Monitor.Log("Found no active jukebox(es)!");
                }
            }
            else
            {
                PlayingIndex = SelectedIndex;
                Monitor.Log("Found active mini-jukebox(es)!");
            }
            LowestVisibleIndex = Math.Max(0, Math.Min(SelectedIndex, MaxScrollIndex()));

            // setup constants

            //string s = "Summer (The Sun Can Bend An Orange Sky)";         // the longest song name, probably? line from ChooseFromListMenu
            //_longestNameWidth = (int)Game1.dialogueFont.MeasureString(s).X;

            string s = "summer2";
            s = Utility.getSongTitleFromCueName(s) + " (" + s + ")";
            //_longestNameWidth = StardewValley.BellsAndWhistles.SpriteText.getWidthOfString(s);
            _longestNameWidth = (int) Game1.dialogueFont.MeasureString(s).X;
            _longestNameWidth += _textureBoxBorderWidth * 2; // spaaaacing

            //Monitor.LogOnce("The longest song width is: " + _longestNameWidth); //1176p in spritetext, 969 in dialoguefont

            //_itemsPerPage = (h - _spacingPixels * 6) / (_spacingPixels * 2); // height of text: roughly 13p * 3 = 39? 
            // ^ is now just 6 and h is set based on it

            // play The Big Selectbowski
            Game1.playSound("bigSelect");

            // setup ui
            SetUpPositions();

        }

        /// <summary>The action that is taken when an option is selected.</summary>
        /// <param name="s">The string passed to the action.</param>
        public delegate void actionOnChoosingListOption(string s);

        /* 
         * Private methods
         */

        // ---- UI methods

        /// <summary>Regenerate the UI.</summary>
        private void SetUpPositions()
        {
            // set up play/stop buttons
            PlayButton = new ClickableTextureComponent(
                "play",
                new Rectangle(
                    xPositionOnScreen + width   - borderWidth - spaceToClearSideBorder - _spacingPixels * 4,
                    yPositionOnScreen           + _spacingPixels,
                    16*4,
                    15*4), 
                "",
                null,
                Game1.mouseCursors, 
                new Rectangle(175, 379, 16, 15),
                4f);

            if (GetNumberOfLocalMiniJukeboxes() == 0)
            {
                StopButton = null;
            }
            else
            {
                StopButton = new ClickableTextureComponent(
                    "stop",
                    new Rectangle(
                        xPositionOnScreen + width   - borderWidth - spaceToClearSideBorder - _spacingPixels * 2,
                        yPositionOnScreen           + _spacingPixels,
                        16 * 4,
                        15*4),
                    "",
                    null,
                    _BetterJukeboxGraphics,
                    new Rectangle(0, 0, 16, 15),
                    4f);
            }

            // set up VisibleOptions to select from
            UpdateVisibleOptions();

            // set up scrolling widgets
            UpArrow = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen + width   + _spacingPixels / 2,
                    VisibleOptionsYPositionOnScreen,
                    44,
                    48),
                Game1.mouseCursors,
                new Rectangle(421,459,11,12),   // up arrow
                4f);

            DownArrow = new ClickableTextureComponent(
                new Rectangle(
                    xPositionOnScreen + width   + _spacingPixels / 2,
                    yPositionOnScreen + height  - 48,
                    44,
                    48), 
                Game1.mouseCursors,
                new Rectangle(421,472,11,12),   // down arrow
                4f);

            ScrollBar = new ClickableTextureComponent(
                new Rectangle(
                    UpArrow.bounds.X + 12,
                    UpArrow.bounds.Y + UpArrow.bounds.Height + 4,
                    24,
                    40), 
                Game1.mouseCursors,
                new Rectangle(435,463,6,10),    // scroll bar
                4f);

            int runnerY = UpArrow.bounds.Y + UpArrow.bounds.Height + 12;
            ScrollBarRunner = new Rectangle(
                ScrollBar.bounds.X,
                runnerY,
                ScrollBar.bounds.Width,
                DownArrow.bounds.Y - runnerY - 12);

            SetScrollBarToLowestVisibleIndex();
        }

        /// <summary>Updates the list of visible options, to be called if scrolling occurs.</summary>
        private void UpdateVisibleOptions()
        {
            VisibleOptions.Clear();
            UpdateVisibleOptionsPositions();

            for (int i = 0; i < _itemsPerPage; ++i)
            {
                int options_index = i + Math.Min(MaxScrollIndex(), LowestVisibleIndex);
                if (options_index >= Options.Count)
                    break;

                VisibleOptions.Add(new ClickableComponent(
                    new Rectangle(
                        VisibleOptionsXPositionOnScreen + _textureBoxBorderWidth,
                        VisibleOptionsYPositionOnScreen + _textureBoxBorderWidth + _spacingPixels * 2 * i,
                        _longestNameWidth + 4,
                        _spacingPixels * 2),
                    Options[options_index])
                );
            }
        }

        /// <summary>Updates the ScrollBar to the current index.</summary>
        private void SetScrollBarToLowestVisibleIndex() // TODO
        {
            // Implementation derived from ShopMenu
            if (Options.Count <= 0 || MaxScrollIndex() == 0)
                return;

            float ratio = (float) LowestVisibleIndex / MaxScrollIndex();
            int runner_range_for_top_of_bar = ScrollBarRunner.Height - ScrollBar.bounds.Height;
            
            ScrollBar.bounds.Y = ScrollBarRunner.Y + (int) (ratio * runner_range_for_top_of_bar);
        }

        // ---- Helper methods

        /// <summary>Returns the max index of options that can display at the top of the list.</summary>
        /// <returns>the index</returns>
        private int MaxScrollIndex()
        {
            return Math.Max(0, Options.Count - _itemsPerPage);
        }

        /// <summary>
        /// Returns the number of mini-jukeboxes on the farmer's current map. 
        /// Used as a poor workaround for differentiating the Saloon jukebox from mini-jukeboxes.
        /// </summary>
        /// <returns>the number of mini-jukeboxes in the current location</returns>
        private int GetNumberOfLocalMiniJukeboxes()
        {
            return Game1.player.currentLocation.miniJukeboxCount.Value;
        }

        /// <summary>Updates the VisibleOptions PositionOnScreen variables.</summary>
        private void UpdateVisibleOptionsPositions()
        {
            VisibleOptionsXPositionOnScreen = xPositionOnScreen + width / 2 - _longestNameWidth / 2 - _textureBoxBorderWidth;
            VisibleOptionsYPositionOnScreen = yPositionOnScreen + height    - _visibleOptionsHeight - _textureBoxBorderWidth * 2 - _spacingPixels;
        }

        // ---- Button methods

        /// <summary>Handles the UpArrow button.</summary>
        private void UpArrowPressed()
        {
            // Implementation copied from ShopMenu
            UpArrow.scale = UpArrow.baseScale;
            --LowestVisibleIndex;
            SetScrollBarToLowestVisibleIndex();
            UpdateVisibleOptions();
        }

        /// <summary>Handles the DownArrow button.</summary>
        private void DownArrowPressed()
        {
            // Implementation copied from ShopMenu
            DownArrow.scale = DownArrow.baseScale;
            ++LowestVisibleIndex;
            SetScrollBarToLowestVisibleIndex();
            UpdateVisibleOptions();
        }

        /// <summary>Handles the PlayButton.</summary>
        private void PlayButtonPressed()
        {
            ChooseAction(Options[SelectedIndex]);
            PlayingIndex = SelectedIndex;

            Monitor.Log("Jukebox now playing: " + Options[SelectedIndex]);

            Game1.playSound("select");
        }

        /// <summary>Handles the StopButton.</summary>
        private void StopButtonPressed()
        {
            if (GetNumberOfLocalMiniJukeboxes() == 0) // this shouldn't happen!
            {
                Monitor.Log("StopButtonPressed() despite no mini-jukeboxes!", LogLevel.Error);
                return;
            }

            ChooseAction("turn_off");
            PlayingIndex = -1;

            Monitor.Log("Jukebox turned off!");

            Game1.playSound("select");
        }

        /*
         * Override methods
         */

        /// <summary>The method invoked when the player left-clicks on the menu.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Implementation derived mostly from ShopMenu
            base.receiveLeftClick(x, y, playSound);
            if (Game1.activeClickableMenu == null)
                return;

            // scrolling related widgets
            if (DownArrow.containsPoint(x, y) && LowestVisibleIndex < MaxScrollIndex()) // not bottom of list
            {
                DownArrowPressed();
                Game1.playSound("shwip");
            }
            else if (UpArrow.containsPoint(x, y) && LowestVisibleIndex > 0) // not top of list
            {
                UpArrowPressed();
                Game1.playSound("shwip");
            }
            else if (ScrollBar.containsPoint(x, y))
                IsScrolling = true;
            else if (!DownArrow.containsPoint(x, y) 
                && x > xPositionOnScreen + width 
                && (x < xPositionOnScreen + width + 128 
                && y > yPositionOnScreen) 
                && y < yPositionOnScreen + height)
            {
                IsScrolling = true;
                leftClickHeld(x, y);
                releaseLeftClick(x, y);
            }

            // play or stop buttons
            else if (PlayButton.containsPoint(x, y) && SelectedIndex >= 0)
            {
                PlayButtonPressed();
                Game1.playSound("select");
            }
            else if (StopButton != null && StopButton.containsPoint(x,y))
            {
                StopButtonPressed();
                Game1.playSound("select");
            }

            // option select (give 'em the mixup)
            else
            {
                for (int i = 0; i < VisibleOptions.Count; ++i)
                {
                    ClickableComponent visible_option = VisibleOptions[i];
                    if (visible_option.containsPoint(x,y))
                    {
                        // repeat selection
                        if (LowestVisibleIndex + i == SelectedIndex)
                        {
                            //Monitor.Log("Playing a song: " + Options[SelectedIndex]);
                            if (visible_option.name != Options[SelectedIndex])
                                Monitor.Log("The song on the button does not match the one being played!", LogLevel.Error);
                            PlayButtonPressed();
                            Game1.playSound("select");
                        }

                        // new selection
                        else
                        {
                            SelectedIndex = LowestVisibleIndex + i;
                            Game1.playSound("shiny4");
                        }

                    }
                }
            }
            
        }

        /// <summary>The method invoked when the player holds left-click.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void leftClickHeld(int x, int y)
        {
            // Implementation copied from ShopMenu
            base.leftClickHeld(x, y);
            if (!IsScrolling)
                return;

            int original_y_bound = ScrollBar.bounds.Y;
            ScrollBar.bounds.Y = Math.Min(
                DownArrow.bounds.Y - 4 - ScrollBar.bounds.Height,   // lowest possible position
                Math.Max(
                    y,      // mouse position
                    UpArrow.bounds.Y + UpArrow.bounds.Height + 4)); // highest possible position

            LowestVisibleIndex = Math.Min(
                MaxScrollIndex(),
                Math.Max(
                    0, 
                    (int)( Options.Count *  // # of options times...
                    (double)( (y - ScrollBarRunner.Y) / (float) ScrollBarRunner.Height ) ) // the fraction of runner scrolled
                )
            );

            SetScrollBarToLowestVisibleIndex();
            UpdateVisibleOptions();

            if (original_y_bound != ScrollBar.bounds.Y)
                Game1.playSound("shiny4");
        }

        /// <summary>The method invoked when the player releases left-click.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void releaseLeftClick(int x, int y)
        {
            // Implementation copied from ShopMenu
            base.releaseLeftClick(x, y);
            IsScrolling = false;
        }

        /// <summary>The method invoked when the player right-clicks on the lookup UI.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        /// <summary>The method invoked when the player hovers the cursor over the menu.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void performHoverAction(int x, int y) 
        {
            base.performHoverAction(x, y);

            if (SelectedIndex >= 0)
                PlayButton.tryHover(x, y);
            StopButton?.tryHover(x, y);
        }

        /// <summary>The method invoked when the player scrolls the mousewheel.</summary>
        /// <param name="direction">The direction of the player's scroll.</param>
        public override void receiveScrollWheelAction(int direction)
        {
            // Implementation copied from ShopMenu
            base.receiveScrollWheelAction(direction);
            if (direction > 0 && LowestVisibleIndex > 0)
            {
                UpArrowPressed();
                Game1.playSound("shiny4");
            }
            else
            {
                if (direction >= 0 || LowestVisibleIndex >= Math.Max(0, MaxScrollIndex()))
                    return;
                DownArrowPressed();
                Game1.playSound("shiny4");
            }
        }

        /// <summary>The method called when the game window changes size.</summary>
        /// <param name="oldBounds">The former viewport.</param>
        /// <param name="newBounds">The new viewport.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            //base.gameWindowSizeChanged(oldBounds, newBounds);
            xPositionOnScreen = newBounds.Width  / 2 - width  / 2;
            yPositionOnScreen = newBounds.Height / 2 - height / 2;

            // refresh ui
            SetUpPositions();
            initializeUpperRightCloseButton();
        }

        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="b">The sprite batch.</param>
        public override void draw(SpriteBatch b)
        {
            // Implementation derived from ChooseFromListMenu

            // draw menu box
            drawTextureBox(
                b,
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White);

            // draw options' submenu box
            drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(384, 373, 18, 18),    // shopMenu's nail corners
                VisibleOptionsXPositionOnScreen,
                VisibleOptionsYPositionOnScreen,
                _longestNameWidth       + _textureBoxBorderWidth * 2,
                _visibleOptionsHeight   + _textureBoxBorderWidth * 2,
                Color.White,
                4f,
                false);

            // draw menu title
            StardewValley.BellsAndWhistles.SpriteText.drawStringWithScrollCenteredAt(
                b,
                Game1.content.LoadString(
                    "Strings\\UI:JukeboxMenu_Title"),
                xPositionOnScreen + width / 2,
                yPositionOnScreen - _spacingPixels);

            // draw "Currently Playing:"
            String cur_play = GetTranslation("BetterJukeboxMenu:Currently_Playing");

            Utility.drawTextWithShadow(
                b,
                cur_play,
                Game1.dialogueFont,
                new Vector2(
                    xPositionOnScreen + width / 2           - Game1.dialogueFont.MeasureString(cur_play).X / 2f,
                    yPositionOnScreen + _spacingPixels),
                Game1.textColor);

            // draw the name of the active song
            String song_name;
            if (PlayingIndex > -1)
                song_name = Utility.getSongTitleFromCueName(Options[PlayingIndex]);
            else
                song_name = Utility.getSongTitleFromCueName("turn_off");

            Utility.drawTextWithShadow(
                b,
                song_name,
                Game1.dialogueFont,
                new Vector2(
                    xPositionOnScreen + width / 2       - Game1.dialogueFont.MeasureString(song_name).X / 2f,
                    yPositionOnScreen + _spacingPixels  + Game1.dialogueFont.MeasureString(cur_play).Y  * 1.25f),
                Game1.textColor);

            //StardewValley.BellsAndWhistles.SpriteText.drawStringHorizontallyCenteredAt(
            //    b,
            //    song_name,
            //    xPositionOnScreen + width / 2,
            //    yPositionOnScreen + _spacingPixels * 3 / 2 + (int) Game1.dialogueFont.MeasureString(cur_play).Y);

            // draw the list of VisibleOptions
            // derived from forSale section of ShopMenu.draw()
            for (int i = 0; i < VisibleOptions.Count; ++i)
            {
                // don't draw if LowestVisibleIndex is incorrect
                if (LowestVisibleIndex + i >= Options.Count)
                {
                    Monitor.LogOnce("Ceased drawing options because LowestVisibleIndex is incorrectly high!", LogLevel.Error);
                    break;  // not continue because will continue to be incorrect
                }

                ClickableComponent button = VisibleOptions[i];

                // determining button colour; priority: selected - hovered (and not dragging scrollbar) - not hovered
                Color button_colour;
                if (LowestVisibleIndex + i == SelectedIndex)
                {
                    button_colour = Color.Peru; // probably looks fine
                }
                else if (button.containsPoint(Game1.getOldMouseX(),Game1.getOldMouseY()) && !IsScrolling)
                {
                    button_colour = Color.Wheat;
                }
                else
                {
                    button_colour = Color.White;
                }

                // drawing the box around each option
                drawTextureBox(
                    b,
                    Game1.mouseCursors,
                    new Rectangle(384, 396, 15, 15),            // box shape w/ elaborate corners
                    button.bounds.X,
                    button.bounds.Y,
                    button.bounds.Width,
                    button.bounds.Height,
                    button_colour,
                    4f,
                    false);

                // drawing option name 
                string cue_name = Options[LowestVisibleIndex + i];

                song_name = Utility.getSongTitleFromCueName(cue_name);

                if (_showInternalID)    // left align song_name, right align cue_name
                {
                    if (cue_name.Equals(song_name))
                        ;   // do nothing
                    else
                    {
                        Utility.drawTextWithShadow(
                            b,
                            song_name,
                            Game1.dialogueFont,
                            new Vector2(
                                button.bounds.X + _textureBoxBorderWidth,
                                button.bounds.Y + button.bounds.Height / 2 - Game1.dialogueFont.MeasureString(song_name).Y / 2f),
                            Game1.textColor);
                    }

                    Utility.drawTextWithShadow(
                        b,
                        cue_name,
                        Game1.dialogueFont,
                        new Vector2(
                            button.bounds.X + button.bounds.Width       - Game1.dialogueFont.MeasureString(cue_name).X - _textureBoxBorderWidth,
                            button.bounds.Y + button.bounds.Height / 2  - Game1.dialogueFont.MeasureString(cue_name).Y / 2f),
                        Game1.textColor);
                }
                else // center text
                {
                    Utility.drawTextWithShadow(
                        b,
                        song_name,
                        Game1.dialogueFont,
                        new Vector2(
                            button.bounds.X + button.bounds.Width / 2 - Game1.dialogueFont.MeasureString(song_name).X / 2f,
                            button.bounds.Y + button.bounds.Height / 2 - Game1.dialogueFont.MeasureString(song_name).Y / 2f),
                        Game1.textColor);
                }
            }

            // draw the play and stop buttons
            PlayButton.draw(b);
            StopButton?.draw(b);

            // draw the scrolling elements
            if (VisibleOptions.Count >= Options.Count)
                ; // do nothing
            else
            {
                UpArrow.draw(b);
                DownArrow.draw(b);

                // copied from ShopMenu.draw()
                drawTextureBox(
                    b,
                    Game1.mouseCursors,
                    new Rectangle(403, 383, 6, 6),
                    ScrollBarRunner.X,
                    ScrollBarRunner.Y,
                    ScrollBarRunner.Width,
                    ScrollBarRunner.Height,
                    Color.White,
                    4f);
                ScrollBar.draw(b);
            }

            // draw the upper right close button
            base.draw(b);

            // draw cursor
            drawMouse(b);
        }
    }
}
