﻿using Microsoft.Xna.Framework;
using Quaver.Config;
using Quaver.Graphics.Colors;
using Quaver.Graphics.Sprites;
using Quaver.Graphics.Text;
using Quaver.Main;

namespace Quaver.Graphics.UserInterface.User
{
    internal class Playercard : Sprite
    {
        /// <summary>
        ///     The avatar of the player.
        /// </summary>
        private Sprite Avatar { get; }

        /// <summary>
        ///     The player's title.
        /// </summary>
        private Sprite Title { get; }

        /// <summary>
        ///     Sprite that changes the brightness for the title.
        /// </summary>
        private Sprite TitleBrightness { get; }

        /// <summary>
        ///     The username of the player.
        /// </summary>
        private SpriteText Username { get; }

        /// <summary>
        ///     The badge that signifiies their "rank" or "prestige"
        /// </summary>
        private Sprite RankBadge { get; }

        /// <summary>
        ///     The player's rating
        /// </summary>
        private SpriteText Rating { get; }

        /// <summary>
        ///     The player's rank.
        /// </summary>
        private SpriteText Rank { get; }

        /// <summary>
        ///     The game mode the player currently has activated.
        /// </summary>
        private Sprite GameMode { get; }

        /// <summary>
        ///     Shows the user's progress until their next level.
        /// </summary>
        private ProgressBar LevelBar { get; }

        /// <summary>
        /// 
        /// </summary>
        internal Playercard()
        {
            Size = new UDim2D(312, 56);

            Tint = Color.Black;
            Alignment = Alignment.TopLeft;
            Alpha = 0.50f;
            
            Avatar = new Sprite()
            {
                Parent = this,
                Image = GameBase.QuaverUserInterface.YouAvatar,
                Size = new UDim2D(SizeY, SizeY),
                Alignment = Alignment.TopLeft
            };

            Title = new Sprite()
            {
                Parent = this,
                Image = Titles.Default,
                Size = new UDim2D(SizeX - Avatar.SizeX, SizeY),
                Alignment = Alignment.TopLeft,
                PosX = Avatar.SizeX,
            };

            TitleBrightness = new Sprite()
            {
                Parent = this,
                Image = GameBase.QuaverUserInterface.BlankBox,
                Tint = Color.Black,
                Alpha = 0.40f,
                Size = Title.Size,
                Alignment = Title.Alignment,
                Position = Title.Position
            };
            
            RankBadge = new Sprite()
            {
                Parent = this,
                Image = FontAwesome.Code,
                Size = new UDim2D(25, 25),
                PosX = Avatar.SizeX + 5,
                PosY = 5,
                Tint = QuaverColors.MainAccent
            };
            
            Username = new SpriteText()
            {
                Parent = this,
                Font = QuaverFonts.AssistantRegular16,
                PosX = RankBadge.PosX + RankBadge.SizeX + 5,
                PosY = RankBadge.PosY,
                Text = ConfigManager.Username.Value,
                Alignment = Alignment.TopLeft,
                TextAlignment = Alignment.TopLeft,
                TextColor = Color.White,
                TextScale = 0.95f
            };
            
            Rating = new SpriteText()
            {
                Parent = this,
                Font = QuaverFonts.AssistantRegular16,
                PosX = RankBadge.PosX,
                PosY = RankBadge.PosY + 27,
                Text = "Rating: 00.00",
                Alignment = Alignment.TopLeft,
                TextAlignment = Alignment.TopLeft,
                TextColor = Color.White,
                TextScale = 0.70f
            };

            GameMode = new Sprite()
            {
                Parent = this,
                Image = FontAwesome.Desktop,
                Size = new UDim2D(25, 25),
                Alignment = Alignment.TopRight,
                Position = new UDim2D(-5, 5),
                Alpha = 1f
            };

            Rank = new SpriteText()
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                TextScale = 0.70f,
                PosY = Rating.PosY,
                Text = "#0 (Lv.0)",
                Font = QuaverFonts.AssistantRegular16,
                TextAlignment = Alignment.TopRight,
                PosX = -5
            };

            LevelBar = new ProgressBar(100, 50, new Vector2(SizeX - Avatar.SizeX, 3), this, Alignment.BotLeft)
            {
                PosX = Avatar.SizeX,
                CurrentValue = 88,
                PosY = 0.5f
            };

        }
    }
}