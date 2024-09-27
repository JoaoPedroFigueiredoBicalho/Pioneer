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

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

public static class CWTS
{
    public static readonly ConditionalWeakTable<Player, Data> dataCWT = new();

    public static bool TryGetCWT(Player self, out Data data)
    {
        if (self != null)
        {
            data = dataCWT.GetOrCreateValue(self);
        }
        else
        {
            data = null;
        }
        return data != null;
    }

    public class Data
    {
        public bool isFirstBite = true;
        public int timer = 0;
        public bool isPioneer = false;
    }
}

namespace SlugTemplate
{

    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(MOD_ID, "Pioneer", "0.2.4")]

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
            On.Leech.Update += LeechUpdateHook;
            On.Player.UpdateAnimation += SwimSpeed;
            On.RainWorld.PostModsInit += RainWorld_PostModsInIt;
            On.Lizard.Bite += LizardBiteHook;
            On.PlayerGraphics.Update += PlayerGraphicsHook;
            On.Player.DeathByBiteMultiplier += DeathByBiteMultiplierHook;
            On.RoomCamera.Update += RoomCameraUpdateHook;
            On.Room.NowViewed += RoomViewedHook;
            On.Room.Update += RoomUpdateHook;
        }

        private void RoomUpdateHook(On.Room.orig_Update orig, Room self)
        {
            orig(self);
        }

        private void RoomViewedHook(On.Room.orig_NowViewed orig, Room self)
        {
            orig(self);
            {
                new PlacedObject.DayNightData(null)
                {
                    nightPalette = 10
                }.Apply(self);
                if (self.game.cameras[0].currentPalette.darkness < 0.8f)
                {
                    self.game.cameras[0].effect_dayNight = 1f;
                    self.game.cameras[0].currentPalette.darkness = 0.8f;
                }
                self.roomSettings.Clouds = 0.875f;
                self.world.rainCycle.sunDownStartTime = 0;
                self.world.rainCycle.dayNightCounter = 3750;
            }
        }

        private void RoomCameraUpdateHook(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            self.currentPalette.darkness = 0.7f;
            self.effect_darkness = 0.7f;
            self.effect_desaturation = 0.3f;

            float num = 1f;
            float num3 = 1.92f;
            if ((float)self.room.world.rainCycle.dayNightCounter < num)
            {
                if (self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.AboveCloudsView) > 0f && self.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.SkyAndLightBloom) > 0f)
                {
                    self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.SkyAndLightBloom).amount = 0f;
                }
                self.paletteBlend = 1;
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




        private void LizardBiteHook(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
        {
            orig(self, chunk);
            //TimeToEscape.TryGet((chunk.owner as Player), out float escapeTimer);
            if ((chunk.owner as Player).SlugCatClass.value == "Pioneer" && CWTS.TryGetCWT((chunk.owner as Player), out var data) && data.isFirstBite)
            {

                self.lizardParams.biteDamageChance = 0;

                (chunk.owner as Player).LoseAllGrasps();
            }
        }
        

        private float DeathByBiteMultiplierHook(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            orig(self);
            if(self.SlugCatClass.value == "Pioneer" && CWTS.TryGetCWT(self, out var data) && data.isFirstBite)
            {
                return 0f;
            }
            return orig(self);
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
            else if (self.player.room.Darkness(self.player.mainBodyChunk.pos) > 0f && !self.player.DreamState && self.player.SlugCatClass.value == "Pioneer")
            {
                self.lightSource = new LightSource(self.player.mainBodyChunk.pos, false, Color.Lerp(new Color(1f, 1f, 1f), (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup) ? self.player.ShortCutColor() : Color.cyan, 0.5f), self.player);
                self.lightSource.requireUpKeep = true;
                self.lightSource.setRad = new float?(600f);
                self.lightSource.setAlpha = new float?(1f);
                self.player.room.AddObject(self.lightSource);
            }
            if (ModManager.MMF)
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
                    self.lanternLight = new LightSource(self.player.bodyChunks[1].pos, true, Color.cyan, self.player);
                    self.lanternLight.submersible = true;
                    self.lanternLight.requireUpKeep = true;
                    self.lanternLight.setRad = new float?(60f);
                    self.lanternLight.setAlpha = 0.1f;
                    self.lanternLight.flat = true;
                    self.player.room.AddObject(self.lanternLight);
                }
            }
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
            if (self.SlugCatClass.value == "Pioneer" && HypothermiaResistance.TryGet(self, out var resistance))
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
            if (CWTS.TryGetCWT(self, out var data) && (self.dead || self.Sleeping) && self.SlugCatClass.value == "Pioneer")
            {
                data.isFirstBite = true;
            }
            if (self.grabbedBy.Count > 0 &&
                (self.grabbedBy[0].grabber is Creature && self.grabbedBy[0].grabber is not Player)
                && !self.dead && self.SlugCatClass.value == "Pioneer")
            {
                if (CWTS.TryGetCWT(self, out data) && TimeToEscape.TryGet(self, out var escapeTimer))
                {
                    data.timer++;
                    
                    if (data.timer > escapeTimer && data.isFirstBite == true)
                    {
                        self.grabbedBy[0].grabber.Violence(null, new Vector2?(Custom.DirVec(self.firstChunk.pos, self.grabbedBy[0].grabber.bodyChunks[0].pos) * 10f * 1f),
                            self.grabbedBy[0].grabber.bodyChunks[0], null, Creature.DamageType.Blunt, 0.2f, 130f * Mathf.Lerp(self.grabbedBy[0].grabber.Template.baseStunResistance, 1f, 0.5f));
                        self.grabbedBy[0].Release();
                        data.timer = 0;
                        data.isFirstBite = false;
                    }
                }
            }
        }

        private void SwimSpeed(On.Player.orig_UpdateAnimation orig, Player self)
        {
            orig(self);
                if (self.SlugCatClass.value == "Pioneer" && self.animation == Player.AnimationIndex.DeepSwim)
                {
                self.waterFriction = 0.90f;
                }
        }

        private void LeechUpdateHook(On.Leech.orig_Update orig, Leech self, bool eu)
        {
            orig(self, eu);
            if (self.huntPrey is Player && (self.huntPrey as Player).SlugCatClass.value == "Pioneer")
            {
                    self.huntPrey = null;
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
            if (SpearPlus.TryGet(self, out var power) && self.SlugCatClass.value == "Pioneer")
            {
                spear.spearDamageBonus *= 1f + power;
            }
        }

    }
}