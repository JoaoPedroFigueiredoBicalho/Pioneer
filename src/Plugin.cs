using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Drawing.Text;
using DressMySlugcat;
using System.Security.Permissions;
using System.Security;
using SlugBase;
using System.Linq;
using System.Collections.Generic;
using IL.Menu.Remix.MixedUI;
using IL.Menu.Remix;
using IL.Menu;
using MoreSlugcats;
using System.Runtime.CompilerServices;
using RWCustom;
using SlugBase.DataTypes;
using System.Threading;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

public static class PioneerClass
{
    public class Pioneer
    {
        // Define your variables to store here!
        public bool SleptWell;
        public bool IsPioneer;
        public bool IsFirstBite;
        public int EscapeTimer;
        

        public Pioneer()
        {
            // Initialize your variables here! (Anything not added here will be null or false or 0 (default values))
            this.SleptWell = false;
            this.IsPioneer = false;
            this.IsFirstBite = false;
            this.EscapeTimer = 0;
        }
    }

    // This part lets you access the stored stuff by simply doing "self.GetCat()" in Plugin.cs or everywhere else!
    private static readonly ConditionalWeakTable<Player, Pioneer> CWT = new();
    public static Pioneer GetCat(this Player player) => CWT.GetValue(player, _ => new());
}

public static class NocturnalClass
{
    public class Nocturnal
    {
        // Define your variables to store here!
        public bool IsNight;


        public Nocturnal()
        {
            // Initialize your variables here! (Anything not added here will be null or false or 0 (default values))
            this.IsNight = false;
        }
    }

    // This part lets you access the stored stuff by simply doing "self.GetCat()" in Plugin.cs or everywhere else!
    private static readonly ConditionalWeakTable<Room, Nocturnal> CWT = new();
    public static Nocturnal GetRoom(this Room room) => CWT.GetValue(room, _ => new());
}

namespace Pioneer
{

    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(MOD_ID, "Pioneer", "0.2.5")]

    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "arwyn.pioneer";

        public static readonly PlayerFeature<float> HypothermiaResistance = PlayerFloat("the_pioneer/HypothermiaResistance");
        public static readonly PlayerFeature<int> SpearPlus = PlayerInt("the_pioneer/SpearPlus");
        public static readonly PlayerFeature<bool> PioneerGlows = PlayerBool("the_pioneer/PioneerGlows");
        public static readonly PlayerFeature<float> TimeToEscape = PlayerFloat("the_pioneer/TimeToEscape");
        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Update += PlayerUpdateHook;
            On.Player.UpdateAnimation += SwimSpeed;
            On.RainWorld.PostModsInit += RainWorld_PostModsInIt;
            On.Lizard.Bite += LizardBiteHook;
            On.PlayerGraphics.Update += PlayerGraphicsHook;
            // On.Player.DeathByBiteMultiplier += DeathByBiteMultiplierHook;
            On.RoomCamera.Update += RoomCameraUpdateHook;
            On.Room.NowViewed += RoomViewedHook;
            On.RainWorld.PostModsInit += RainWorld_PostModsInIt;
            //On.Menu.KarmaLadderScreen.SleepDeathScreenDataPackage += SleepAndDeathScreenDataPackageHook;
            On.Player.ctor += Abracadabra;
            On.Player.DeathByBiteMultiplier += DeathByBiteMultiplierHook;


        }

        private void Abracadabra(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.slugcatStats.name.value == "Pioneer")
            {
                self.GetCat().IsPioneer = true;
                self.GetCat().IsFirstBite = true;
            }
        }


        private void RoomViewedHook(On.Room.orig_NowViewed orig, Room self)
        {
            orig(self);
            //if ((self.world.game.Players[0].realizedCreature as Player).GetCat().IsPioneer && (self.world.game.Players[0].realizedCreature as Player).GetCat().SleptWell)
            {
                new PlacedObject.DayNightData(null)
                {
                    nightPalette = 26
                }.Apply(self);
                if (self.game.cameras[0].currentPalette.darkness < 0.8f)
                {
                    self.game.cameras[0].effect_dayNight = 1f;
                    self.game.cameras[0].currentPalette.darkness = 0.8f;
                }
                self.roomSettings.Clouds = 0.875f;
                self.world.rainCycle.sunDownStartTime = 0;
                self.world.rainCycle.dayNightCounter = 30000;
            }
        }

        private void RoomCameraUpdateHook(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            Creature creature = (self.followAbstractCreature != null) ? self.followAbstractCreature.realizedCreature : null;
            //if (creature != null && creature is Player && (creature as Player).GetCat().IsPioneer && self.game.IsStorySession && (creature as Player).GetCat().SleptWell)
            {
                self.currentPalette.darkness = 0.3f;
                self.effect_darkness = 0.3f;

                float num = 1320f;
                float num3 = 1.92f;
                if ((float)self.room.world.rainCycle.dayNightCounter < num)
                {
                    if (self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.AboveCloudsView) > 0f && self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.SkyAndLightBloom) > 0f)
                    {
                        self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.SkyAndLightBloom).amount = 0f;
                    }
                    self.paletteBlend = 1f;
                    self.ApplyFade();
                }
                /*else if ((float)self.room.world.rainCycle.dayNightCounter == num)
                {
                    self.ChangeBothPalettes(self.paletteB, self.room.world.rainCycle.duskPalette, 0f);
                }
                else if ((float)self.room.world.rainCycle.dayNightCounter < num * num2)
                {
                    if (self.paletteBlend == 1f || self.paletteB != self.room.world.rainCycle.duskPalette || self.dayNightNeedsRefresh)
                    {
                        self.ChangeBothPalettes(self.paletteB, self.room.world.rainCycle.duskPalette, 0f);
                    }
                    self.paletteBlend = Mathf.InverseLerp(num, num * num2, (float)self.room.world.rainCycle.dayNightCounter);
                    self.ApplyFade();
                }
                else if ((float)self.room.world.rainCycle.dayNightCounter == num * num2)
                {
                    self.ChangeBothPalettes(self.room.world.rainCycle.duskPalette, self.room.world.rainCycle.nightPalette, 0f);
                }
                else if ((float)self.room.world.rainCycle.dayNightCounter < num * num3)
                {
                    if (self.paletteBlend == 1f || self.paletteB != self.room.world.rainCycle.nightPalette || self.paletteA != self.room.world.rainCycle.duskPalette || self.dayNightNeedsRefresh)
                    {
                        self.ChangeBothPalettes(self.room.world.rainCycle.duskPalette, self.room.world.rainCycle.nightPalette, 0f);
                    }
                    self.paletteBlend = Mathf.InverseLerp(num * num2, num * num3, (float)self.room.world.rainCycle.dayNightCounter) * (self.effect_dayNight * 0.99f);
                    self.ApplyFade();
                }
                else if ((float)self.room.world.rainCycle.dayNightCounter == num * num3)
                {
                    self.ChangeBothPalettes(self.room.world.rainCycle.duskPalette, self.room.world.rainCycle.nightPalette, self.effect_dayNight * 0.99f);
                }*/
                else if ((float)self.room.world.rainCycle.dayNightCounter > num * num3)
                {
                    if (self.paletteBlend == 1f || self.paletteB != self.room.world.rainCycle.nightPalette || self.paletteA != self.room.world.rainCycle.duskPalette || self.dayNightNeedsRefresh)
                    {
                        self.ChangeBothPalettes(self.room.world.rainCycle.duskPalette, self.room.world.rainCycle.nightPalette, self.effect_dayNight);
                    }
                    self.paletteBlend = self.effect_dayNight * 0.99f;
                    self.ApplyFade();
                }

                self.dayNightNeedsRefresh = false;
            }
        }





        private void LizardBiteHook(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
        {
            orig(self, chunk);
            if ((chunk.owner as Player).GetCat().IsPioneer == true && (chunk.owner as Player).GetCat().IsFirstBite == true)
            {
                if (TimeToEscape.TryGet((chunk.owner as Player), out var escapeTime) && (chunk.owner as Player).GetCat().EscapeTimer > escapeTime && (chunk.owner as Player).GetCat().IsFirstBite == true && (chunk.owner as Player).GetCat().IsPioneer)
                    (chunk.owner as Player).GetCat().IsFirstBite = false;
                
            }
        }
        

        private float DeathByBiteMultiplierHook(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            if(self.GetCat().IsPioneer)
            {
                return 0f;
            }
            else return orig(self);
        }

        private void PlayerGraphicsHook(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            // Pioneer glows by default
            orig(self);
            if (self.lightSource != null)
            {
                self.lightSource.stayAlive = true;
                self.lightSource.setPos = new Vector2?(self.player.mainBodyChunk.pos);
                if (self.lightSource.slatedForDeletetion || self.player.room.Darkness(self.player.mainBodyChunk.pos) == 0f)
                {
                    self.lightSource = null;
                }
            }
            else if (!self.player.DreamState && self.player.SlugCatClass.value == "Pioneer")
            {
                self.lightSource = new LightSource(self.player.mainBodyChunk.pos, false, Color.Lerp(new Color(1f, 1f, 1f), (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup) ? self.player.ShortCutColor() : Color.cyan, 0.5f), self.player);
                self.lightSource.requireUpKeep = true;
                self.lightSource.setRad = new float?(600f);
                self.lightSource.setAlpha = new float?(1f);
                self.player.room.AddObject(self.lightSource);
            }
            /*if (ModManager.MMF && self.player.GetCat().IsPioneer)
            {
                Color? color = Color.cyan;
                if (self.lanternLight != null)
                {
                    self.lanternLight.stayAlive = true;
                    self.lanternLight.setPos = new Vector2?(self.player.bodyChunks[1].pos);
                    self.lanternLight.setAlpha = new float?(0.09f + UnityEngine.Random.value / 50f);
                    if (self.lanternLight.slatedForDeletetion || color == null)
                    {
                        self.lanternLight = null;
                    }
                }
                else if (color != null)
                {
                    self.lanternLight = new LightSource(self.player.bodyChunks[1].pos, true, self.player.ShortCutColor(), self.player);
                    self.lanternLight.submersible = true;
                    self.lanternLight.requireUpKeep = true;
                    self.lanternLight.setRad = new float?(60f);
                    self.lanternLight.setAlpha = 0.1f;
                    self.lanternLight.flat = true;
                    self.player.room.AddObject(self.lanternLight);
                }
            }*/
        }



        public static bool IsPostInit;
        private void RainWorld_PostModsInIt(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsPostInit) return;
                IsPostInit = true;

                //-- You can have the DMS sprite setup in a separate method and only call it if DMS is loaded
                //-- With self the mod will still work even if DMS isn't installed
                if (ModManager.ActiveMods.Any(mod => mod.id == "dressmyslugcat"))
                {
                    SetupDMSSprites();
                }

                Debug.Log($"Plugin dressmyslugcat.templatecat is loaded!");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void SetupDMSSprites()
        {
            //-- The ID of the spritesheet we will be using as the default sprites for our slugcat
            var sheetID = "Zenala.ThePioneerTundracat";

            //-- Each player slot (0, 1, 2, 3) can be customized individually
            for (int i = 0; i < 4; i++)
            {
                SpriteDefinitions.AddSlugcatDefault(new Customization()
                {
                    //-- Make sure to use the same ID as the one used for our slugcat
                    Slugcat = "Pioneer",
                    PlayerNumber = i,
                    CustomSprites = new List<CustomSprite>
                    {
                        //-- You can customize which spritesheet and color each body part will use
                        new CustomSprite() { Sprite = "HEAD", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "FACE", SpriteSheetID = sheetID, Color = Color.white },
                        new CustomSprite() { Sprite = "FACELEFT", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "FACERIGHT", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "BODY", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "ARMS", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "HIPS", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "HIPSRIGHT", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "HIPSLEFT", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "LEGS", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "EXTRAS", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "PIXEL", SpriteSheetID = sheetID },
                        new CustomSprite() { Sprite = "TAIL", SpriteSheetID = sheetID }
                    },

                    //-- Customizing the tail size and color is also supported. The values should match what you want on the sliders in-game.
                    //-- Remove them if you want them to default to a regular tail size
                    CustomTail = new CustomTail()
                    {
                        AsymTail = true,
                        Length = 6f,
                        Wideness = 7.5f,
                        Roundness = 0.8f,
                        ForbidTailResize = false //-- If you have special code for your tail that could break if the tail size is changed, set this value to true to prevent users from changing the tail size.
                    },
                });
            }
        }

        private void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            // Pioneer is more resistant to hypothermia when dry, and more vulnerable to hypothermia when wet
            if (self.GetCat().IsPioneer && HypothermiaResistance.TryGet(self, out var resistance))
            {
                if (self.Submersion >= 0.1f)
                {
                    self.HypothermiaGain *= (resistance - 1f);
                    self.Hypothermia += self.HypothermiaGain * (resistance);
                }
                else
                {
                    self.HypothermiaGain *= (resistance - 1f);
                    self.Hypothermia -= self.HypothermiaGain * (resistance - 1f);
                }
            }

            // Pioneer escapes the first grab of the cycle (ty struggle devs)
            if (self.grabbedBy.Count > 0 &&
                (self.grabbedBy[0].grabber is Creature && self.grabbedBy[0].grabber is not Player)
                && !self.dead && self.GetCat().IsPioneer)
            {
                if (self.GetCat().IsPioneer)
                {
                    self.GetCat().EscapeTimer++;
                    if (self.grabbedBy[0].grabber is Leech)
                    {
                        self.grabbedBy[0].grabber.Violence(null, new Vector2?(Custom.DirVec(self.firstChunk.pos, self.grabbedBy[0].grabber.bodyChunks[0].pos) * 10f * 1f),
                            self.grabbedBy[0].grabber.bodyChunks[0], null, Creature.DamageType.Blunt, 0.2f, 130f * Mathf.Lerp(self.grabbedBy[0].grabber.Template.baseStunResistance, 1f, 0.5f));
                        self.grabbedBy[0].Release();
                    }
                    if (TimeToEscape.TryGet(self, out var escapeTime) && self.GetCat().EscapeTimer == escapeTime && self.GetCat().IsFirstBite == true && self.GetCat().IsPioneer)
                    {
                        self.grabbedBy[0].grabber.Violence(null, new Vector2?(Custom.DirVec(self.firstChunk.pos, self.grabbedBy[0].grabber.bodyChunks[0].pos) * 10f * 1f),
                            self.grabbedBy[0].grabber.bodyChunks[0], null, Creature.DamageType.Blunt, 0.2f, 130f * Mathf.Lerp(self.grabbedBy[0].grabber.Template.baseStunResistance, 1f, 0.5f));
                        self.grabbedBy[0].Release();
                        self.GetCat().EscapeTimer = 0;
                        self.GetCat().IsFirstBite = false;
                    }
                }
            }
        }

        private void SwimSpeed(On.Player.orig_UpdateAnimation orig, Player self)
        {
            orig(self);
                if (self.GetCat().IsPioneer && self.animation == Player.AnimationIndex.DeepSwim)
                {
                self.waterFriction = 0.90f;
                }
        }


        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }
        //My spears are useful now, Or maybe not?!
        private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (SpearPlus.TryGet(self, out var power) && self.GetCat().IsPioneer)
            {
                spear.spearDamageBonus *= 1f + power;
            }
        }

    }
}