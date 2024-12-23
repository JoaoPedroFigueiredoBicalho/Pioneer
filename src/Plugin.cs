using System;
using System.Text.RegularExpressions;
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
using SlugBase.SaveData;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
public class SaveMiscWorld
{
    public bool IsNight = true;
    public bool NightTutorial = false;
    public bool FoodTutorial = false;
    public List<string> MySaveStrings { get; } = new();



}
public static class PioneerClass
{
    public class Pioneer
    {
        // Define your variables to store here!
        public bool SleptWell;
        public bool IsPioneer;
        public bool IsFirstBite;
        public int EscapeTimer;
        public int CycleWaitTimer;


        public Pioneer()
        {
            // Initialize your variables here! (Anything not added here will be null or false or 0 (default values))
            this.SleptWell = false;
            this.IsPioneer = false;
            this.IsFirstBite = false;
            this.EscapeTimer = 0;
            this.CycleWaitTimer = 0;
        }
    }

    // This part lets you access the stored stuff by simply doing "self.GetCat()" in Plugin.cs or everywhere else!
    private static readonly ConditionalWeakTable<Player, Pioneer> CWT = new();
    public static Pioneer GetCat(this Player player) => CWT.GetValue(player, _ => new());
}
public static class SaveAttempt
{
#nullable enable
    public static SaveMiscWorld? GetMiscWorld(this RainWorldGame game) => game.IsStorySession ? GetMiscWorld(game.GetStorySession.saveState.miscWorldSaveData) : null;
#nullable disable
    public static SaveMiscWorld GetMiscWorld(this MiscWorldSaveData data)
    {
        if (!data.GetSlugBaseData().TryGet("arwyn.pioneer", out SaveMiscWorld save))
            data.GetSlugBaseData().Set("arwyn.pioneer", save = new());

        return save;
    }
}

namespace Pioneer
{

    [BepInDependency("slime-cubed.slugbase")]
    [BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(MOD_ID, "Pioneer", "0.5.6")]

    class Plugin : BaseUnityPlugin
    {


        private const string MOD_ID = "arwyn.pioneer";

        public static readonly SlugcatStats.Name Pioneer = new SlugcatStats.Name("Pioneer", false);

        public static readonly PlayerFeature<float> HypothermiaResistance = PlayerFloat("the_pioneer/HypothermiaResistance");
        public static readonly PlayerFeature<int> SpearPlus = PlayerInt("the_pioneer/SpearPlus");
        public static readonly PlayerFeature<bool> PioneerGlows = PlayerBool("the_pioneer/PioneerGlows");
        public static readonly PlayerFeature<float> TimeToEscape = PlayerFloat("the_pioneer/TimeToEscape");
        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("the_pioneer/super_jump");

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
            On.Player.DeathByBiteMultiplier += DeathByBiteMultiplierHook;
            On.RoomCamera.Update += RoomCameraUpdateHook;
            On.Room.NowViewed += RoomViewedHook;
            On.RainWorld.PostModsInit += RainWorld_PostModsInIt;
            //On.Menu.KarmaLadderScreen.SleepDeathScreenDataPackage += SleepAndDeathScreenDataPackageHook;
            On.Player.ctor += Abracadabra;
            On.Player.DeathByBiteMultiplier += DeathByBiteMultiplierHook;
            On.SaveState.SessionEnded += SessionEndedHook;
            On.Player.Grabability += GrababilityHook;
            On.Player.CanIPickThisUp += CanIPickThisUpHook;
            On.MoreSlugcats.CLOracleBehavior.InitateConversation += InitiateConversationHook;
            On.SLOracleBehaviorHasMark.NameForPlayer += FluffyFriend;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += AddEventsHook;
            On.ShelterDoor.DoorClosed += HibernateHook;
            On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += AddImageHook;
            On.Player.Jump += Player_Jump;
        }

        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            var save = self.room.game.GetMiscWorld();
            if (self.GetCat().IsPioneer)
            {
                self.feetStuckPos = null;
                self.pyroJumpDropLock = 40;
                self.forceSleepCounter = 0;
                if (self.PainJumps && (self.grasps[0] == null || !(self.grasps[0].grabbed is Yeek)))
                {
                    self.gourmandExhausted = true;
                    self.aerobicLevel = 1f;
                }
                float num = Mathf.Lerp(1f, 1.15f, self.Adrenaline);
                if (self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed) && !(self.grasps[0].grabbed is Cicada))
                {
                    num += Mathf.Min(Mathf.Max(0f, self.grasps[0].grabbed.TotalMass - 0.2f) * 1.5f, 1.3f);
                }
                self.AerobicIncrease(self.isGourmand ? 0.75f : 1f);
                if (self.bodyMode == Player.BodyModeIndex.WallClimb)
                {
                    int direction;
                    if (self.canWallJump != 0)
                    {
                        direction = Math.Sign(self.canWallJump);
                    }
                    else if (self.bodyChunks[0].ContactPoint.x != 0)
                    {
                        direction = -self.bodyChunks[0].ContactPoint.x;
                    }
                    else
                    {
                        direction = -self.flipDirection;
                    }
                    self.WallJump(direction);
                    return;
                }
                if (!(self.bodyMode == Player.BodyModeIndex.CorridorClimb))
                {
                    if (self.animation == Player.AnimationIndex.LedgeGrab)
                    {
                        if (self.input[0].x != 0)
                        {
                            self.WallJump(-self.input[0].x);
                            return;
                        }
                    }
                    else if (self.animation == Player.AnimationIndex.ClimbOnBeam)
                    {
                        self.jumpBoost = 0f;
                        if (self.input[0].x != 0)
                        {
                            self.animation = Player.AnimationIndex.None;
                            if (self.PainJumps)
                            {
                                self.bodyChunks[0].vel.y = 3f * num;
                                self.bodyChunks[1].vel.y = 2f * num;
                                self.bodyChunks[0].vel.x = 3f * (float)self.flipDirection * num;
                                self.bodyChunks[1].vel.x = 2f * (float)self.flipDirection * num;
                            }
                            else if (self.GetCat().IsPioneer && save.IsNight)
                            {
                                self.bodyChunks[0].vel.y = 8.5f * num;
                                self.bodyChunks[1].vel.y = 7.5f * num;
                                self.bodyChunks[0].vel.x = 7f * (float)self.flipDirection * num;
                                self.bodyChunks[1].vel.x = 6f * (float)self.flipDirection * num;
                            }
                            else if (self.isSlugpup)
                            {
                                self.bodyChunks[0].vel.y = 7f * num;
                                self.bodyChunks[1].vel.y = 6f * num;
                                self.bodyChunks[0].vel.x = 5f * (float)self.flipDirection * num;
                                self.bodyChunks[1].vel.x = 4.5f * (float)self.flipDirection * num;
                            }
                            else
                            {
                                self.bodyChunks[0].vel.y = 8f * num;
                                self.bodyChunks[1].vel.y = 7f * num;
                                self.bodyChunks[0].vel.x = 6f * (float)self.flipDirection * num;
                                self.bodyChunks[1].vel.x = 5f * (float)self.flipDirection * num;
                            }
                            self.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, self.mainBodyChunk, false, 1f, 1f);
                            return;
                        }
                        if (self.input[0].y <= 0)
                        {
                            self.animation = Player.AnimationIndex.None;
                            self.bodyChunks[0].vel.y = 2f * num;
                            if (self.input[0].y > -1)
                            {
                                self.bodyChunks[0].vel.x = 2f * (float)self.flipDirection * num;
                            }
                            self.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, self.mainBodyChunk, false, 0.3f, 1f);
                            return;
                        }
                        if (self.slowMovementStun < 1 && self.slideUpPole < 1)
                        {
                            self.Blink(7);
                            for (int i = 0; i < 2; i++)
                            {
                                BodyChunk bodyChunk = self.bodyChunks[i];
                                bodyChunk.pos.y = bodyChunk.pos.y + (self.isSlugpup ? 2.25f : 4.5f);
                                BodyChunk bodyChunk2 = self.bodyChunks[i];
                                bodyChunk2.vel.y = bodyChunk2.vel.y + (self.isSlugpup ? 1f : 2f);
                            }
                            self.slideUpPole = 17;
                            self.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, self.mainBodyChunk, false, 0.8f, 1f);
                            return;
                        }
                    }
                    else
                    {
                        if (self.animation == Player.AnimationIndex.Roll)
                        {
                            self.bodyChunks[1].vel *= 0f;
                            self.bodyChunks[1].pos += new Vector2(5f * (float)self.rollDirection, 5f);
                            self.bodyChunks[0].pos = self.bodyChunks[1].pos + new Vector2(5f * (float)self.rollDirection, 5f);
                            float t = Mathf.InverseLerp(0f, 25f, (float)self.rollCounter);
                            self.bodyChunks[0].vel = Custom.DegToVec((float)self.rollDirection * Mathf.Lerp(60f, 35f, t)) * Mathf.Lerp(9.5f, 13.1f, t) * num * (self.isSlugpup ? 0.65f : 1f);
                            self.bodyChunks[1].vel = Custom.DegToVec((float)self.rollDirection * Mathf.Lerp(60f, 35f, t)) * Mathf.Lerp(9.5f, 13.1f, t) * num * (self.isSlugpup ? 0.65f : 1f);
                            BodyChunk bodyChunk3 = self.bodyChunks[0];
                            bodyChunk3.vel.x = bodyChunk3.vel.x * (self.GetCat().IsPioneer && save.IsNight ? 1.5f : 1f);
                            BodyChunk bodyChunk4 = self.bodyChunks[1];
                            bodyChunk4.vel.x = bodyChunk4.vel.x * (self.GetCat().IsPioneer && save.IsNight ? 1.5f : 1f);
                            self.animation = Player.AnimationIndex.RocketJump;
                            self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk, false, 1f, 1f);
                            self.rollDirection = 0;
                            return;
                        }
                        if (self.animation == Player.AnimationIndex.BellySlide)
                        {
                            float num2 = 9f;
                            if (self.GetCat().IsPioneer && save.IsNight)
                            {
                                num2 = 15f;
                            }
                            if (self.isSlugpup)
                            {
                                num2 = 6f;
                            }
                            if (!self.whiplashJump && self.input[0].x != -self.rollDirection)
                            {
                                float y = 8.5f;
                                if (self.GetCat().IsPioneer && save.IsNight)
                                {
                                    y = 10f;
                                }
                                if (self.isSlugpup)
                                {
                                    y = 6f;
                                }
                                self.bodyChunks[1].pos += new Vector2(5f * (float)self.rollDirection, 5f);
                                self.bodyChunks[0].pos = self.bodyChunks[1].pos + new Vector2(5f * (float)self.rollDirection, 5f);
                                self.bodyChunks[1].vel = new Vector2((float)self.rollDirection * num2, y) * num * (self.longBellySlide ? 1.2f : 1f);
                                self.bodyChunks[0].vel = new Vector2((float)self.rollDirection * num2, y) * num * (self.longBellySlide ? 1.2f : 1f);
                                self.animation = Player.AnimationIndex.RocketJump;
                                self.rocketJumpFromBellySlide = true;
                                self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk, false, 1f, 1f);
                                self.rollDirection = 0;
                                return;
                            }
                            self.animation = Player.AnimationIndex.Flip;
                            self.standing = true;
                            self.room.AddObject(new ExplosionSpikes(self.room, self.bodyChunks[1].pos + new Vector2(0f, -self.bodyChunks[1].rad), 8, 7f, 5f, 5.5f, 40f, new Color(1f, 1f, 1f, 0.5f)));
                            int num3 = 1;
                            int num4 = 1;
                            while (num4 < 4 && !self.room.GetTile(self.bodyChunks[0].pos + new Vector2((float)(num4 * -(float)self.rollDirection) * 15f, 0f)).Solid && !self.room.GetTile(self.bodyChunks[0].pos + new Vector2((float)(num4 * -(float)self.rollDirection) * 15f, 20f)).Solid)
                            {
                                num3 = num4;
                                num4++;
                            }
                            self.bodyChunks[0].pos += new Vector2((float)self.rollDirection * -((float)num3 * 15f + 8f), 14f);
                            self.bodyChunks[1].pos += new Vector2((float)self.rollDirection * -((float)num3 * 15f + 2f), 0f);
                            self.bodyChunks[0].vel = new Vector2((float)self.rollDirection * (self.GetCat().IsPioneer && save.IsNight ? -11f : -7f), self.GetCat().IsPioneer ? 12f : 10f);
                            self.bodyChunks[1].vel = new Vector2((float)self.rollDirection * (self.GetCat().IsPioneer && save.IsNight ? -11f : -7f), self.GetCat().IsPioneer ? 13f : 11f);
                            self.rollDirection = -self.rollDirection;
                            self.flipFromSlide = true;
                            self.whiplashJump = false;
                            self.jumpBoost = 0f;
                            self.room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, self.mainBodyChunk, false, 1f, 1f);
                            if (self.pickUpCandidate != null && self.CanIPickThisUp(self.pickUpCandidate) && (self.grasps[0] == null || self.grasps[1] == null) && (self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.OneHand || self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.BigOneHand))
                            {
                                int graspUsed = (self.grasps[0] == null) ? 0 : 1;
                                for (int j = 0; j < self.pickUpCandidate.grabbedBy.Count; j++)
                                {
                                    self.pickUpCandidate.grabbedBy[j].grabber.GrabbedObjectSnatched(self.pickUpCandidate.grabbedBy[j].grabbed, self);
                                    self.pickUpCandidate.grabbedBy[j].grabber.ReleaseGrasp(self.pickUpCandidate.grabbedBy[j].graspUsed);
                                }
                                self.SlugcatGrab(self.pickUpCandidate, graspUsed);
                                if (self.pickUpCandidate is PlayerCarryableItem)
                                {
                                    (self.pickUpCandidate as PlayerCarryableItem).PickedUp(self);
                                }
                                if (self.pickUpCandidate.graphicsModule != null)
                                {
                                    self.pickUpCandidate.graphicsModule.BringSpritesToFront();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (self.animation == Player.AnimationIndex.AntlerClimb)
                            {
                                self.animation = Player.AnimationIndex.None;
                                self.jumpBoost = 0f;
                                self.bodyChunks[0].vel = self.playerInAntlers.antlerChunk.vel;
                                if (!self.playerInAntlers.dangle)
                                {
                                    self.bodyChunks[1].vel = self.playerInAntlers.antlerChunk.vel;
                                }
                                if (self.playerInAntlers.dangle)
                                {
                                    if (self.input[0].x == 0)
                                    {
                                        BodyChunk bodyChunk5 = self.bodyChunks[0];
                                        bodyChunk5.vel.y = bodyChunk5.vel.y + 3f;
                                        BodyChunk bodyChunk6 = self.bodyChunks[1];
                                        bodyChunk6.vel.y = bodyChunk6.vel.y - 3f;
                                        self.standing = true;
                                        self.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, self.mainBodyChunk, false, 1f, 1f);
                                    }
                                    else
                                    {
                                        BodyChunk bodyChunk7 = self.bodyChunks[1];
                                        bodyChunk7.vel.y = bodyChunk7.vel.y + 4f;
                                        BodyChunk bodyChunk8 = self.bodyChunks[1];
                                        bodyChunk8.vel.x = bodyChunk8.vel.x + 2f * (float)self.input[0].x;
                                        BodyChunk bodyChunk9 = self.bodyChunks[0];
                                        bodyChunk9.vel.y = bodyChunk9.vel.y + 6f;
                                        BodyChunk bodyChunk10 = self.bodyChunks[0];
                                        bodyChunk10.vel.x = bodyChunk10.vel.x + 3f * (float)self.input[0].x;
                                        self.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, self.mainBodyChunk, false, 0.15f, 1f);
                                    }
                                }
                                else if (self.input[0].x == 0)
                                {
                                    if (self.input[0].y > 0)
                                    {
                                        BodyChunk bodyChunk11 = self.bodyChunks[0];
                                        bodyChunk11.vel.y = bodyChunk11.vel.y + 4f * num;
                                        BodyChunk bodyChunk12 = self.bodyChunks[1];
                                        bodyChunk12.vel.y = bodyChunk12.vel.y + 3f * num;
                                        self.jumpBoost = (float)(self.isSlugpup ? 7 : 8);
                                        self.room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, self.mainBodyChunk, false, 1f, 1f);
                                        self.standing = true;
                                    }
                                    else
                                    {
                                        self.bodyChunks[0].vel.y = 3f;
                                        self.bodyChunks[1].vel.y = -3f;
                                        self.standing = true;
                                        self.room.PlaySound(SoundID.Slugcat_Climb_Along_Horizontal_Beam, self.mainBodyChunk, false, 1f, 1f);
                                    }
                                }
                                else
                                {
                                    BodyChunk bodyChunk13 = self.bodyChunks[0];
                                    bodyChunk13.vel.y = bodyChunk13.vel.y + 8f * num;
                                    BodyChunk bodyChunk14 = self.bodyChunks[1];
                                    bodyChunk14.vel.y = bodyChunk14.vel.y + 7f * num;
                                    BodyChunk bodyChunk15 = self.bodyChunks[0];
                                    bodyChunk15.vel.x = bodyChunk15.vel.x + 6f * (float)self.input[0].x * num;
                                    BodyChunk bodyChunk16 = self.bodyChunks[1];
                                    bodyChunk16.vel.x = bodyChunk16.vel.x + 5f * (float)self.input[0].x * num;
                                    self.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, self.mainBodyChunk, false, 1f, 1f);
                                }
                                Vector2 vector = self.bodyChunks[0].vel - self.playerInAntlers.antlerChunk.vel + (self.bodyChunks[1].vel - self.playerInAntlers.antlerChunk.vel) * (self.playerInAntlers.dangle ? 0f : 1f);
                                vector -= Custom.DirVec(self.mainBodyChunk.pos, self.playerInAntlers.deer.mainBodyChunk.pos) * vector.magnitude;
                                vector.x *= 0.1f;
                                vector = Vector2.ClampMagnitude(vector, 10f);
                                self.playerInAntlers.antlerChunk.vel -= vector * 1.2f;
                                self.playerInAntlers.deer.mainBodyChunk.vel -= vector * 0.25f;
                                self.playerInAntlers.playerDisconnected = true;
                                self.playerInAntlers = null;
                                return;
                            }
                            if (!(self.animation == Player.AnimationIndex.ZeroGSwim) && !(self.animation == Player.AnimationIndex.ZeroGPoleGrab))
                            {
                                int num5 = self.input[0].x;
                                bool flag = false;
                                if (self.animation == Player.AnimationIndex.DownOnFours && self.bodyChunks[1].ContactPoint.y < 0 && self.input[0].downDiagonal == self.flipDirection)
                                {
                                    self.animation = Player.AnimationIndex.BellySlide;
                                    self.rollDirection = self.flipDirection;
                                    self.rollCounter = 0;
                                    self.standing = false;
                                    self.room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, self.mainBodyChunk, false, 1f, 1f);
                                    flag = true;
                                }
                                if (!flag)
                                {
                                    self.animation = Player.AnimationIndex.None;
                                    if (self.standing)
                                    {
                                        if (self.slideCounter > 0 && self.slideCounter < 10)
                                        {
                                            if (self.PainJumps)
                                            {
                                                self.bodyChunks[0].vel.y = 4f * num;
                                                self.bodyChunks[1].vel.y = 3f * num;
                                            }
                                            else
                                            {
                                                self.bodyChunks[0].vel.y = (self.GetCat().IsPioneer && save.IsNight ? 10f : 9f) * num;
                                                self.bodyChunks[1].vel.y = (self.GetCat().IsPioneer && save.IsNight ? 8f : 7f) * num;
                                            }
                                            BodyChunk bodyChunk17 = self.bodyChunks[0];
                                            bodyChunk17.vel.x = bodyChunk17.vel.x * 0.5f;
                                            BodyChunk bodyChunk18 = self.bodyChunks[1];
                                            bodyChunk18.vel.x = bodyChunk18.vel.x * 0.5f;
                                            BodyChunk bodyChunk19 = self.bodyChunks[0];
                                            bodyChunk19.vel.x = bodyChunk19.vel.x - (float)self.slideDirection * 4f * num;
                                            self.jumpBoost = 5f;
                                            if (self.GetCat().IsPioneer && save.IsNight)
                                            {
                                                self.jumpBoost = 6f;
                                            }
                                            if (self.isSlugpup)
                                            {
                                                self.jumpBoost = 3f;
                                            }
                                            self.animation = Player.AnimationIndex.Flip;
                                            self.room.PlaySound(SoundID.Slugcat_Flip_Jump, self.mainBodyChunk, false, 1f, 1f);
                                            self.slideCounter = 0;
                                        }
                                        else
                                        {
                                            if (self.PainJumps)
                                            {
                                                self.bodyChunks[0].vel.y = 2f * num;
                                                self.bodyChunks[1].vel.y = 1f * num;
                                            }
                                            else
                                            {
                                                self.bodyChunks[0].vel.y = (self.GetCat().IsPioneer && save.IsNight ? 4.5f : 4f) * num;
                                                self.bodyChunks[1].vel.y = (self.GetCat().IsPioneer && save.IsNight ? 3.5f : 3f) * num;
                                            }
                                            self.jumpBoost = (float)(self.isSlugpup ? 7 : 8);
                                            self.room.PlaySound((self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam) ? SoundID.Slugcat_From_Horizontal_Pole_Jump : SoundID.Slugcat_Normal_Jump, self.mainBodyChunk, false, 1f, 1f);
                                        }
                                    }
                                    else
                                    {
                                        float num6 = 1.5f;
                                        if (self.superLaunchJump >= 20)
                                        {
                                            self.superLaunchJump = 0;
                                            num6 = 9f;
                                            if (self.PainJumps)
                                            {
                                                num6 = 2.5f;
                                            }
                                            else if (self.GetCat().IsPioneer && save.IsNight)
                                            {
                                                num6 = 12f;
                                            }
                                            else if (self.isSlugpup)
                                            {
                                                num6 = 5.5f;
                                            }
                                            num5 = ((self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1);
                                            self.simulateHoldJumpButton = 6;
                                        }
                                        BodyChunk bodyChunk20 = self.bodyChunks[0];
                                        bodyChunk20.pos.y = bodyChunk20.pos.y + 6f;
                                        if (self.bodyChunks[0].ContactPoint.y == -1)
                                        {
                                            BodyChunk bodyChunk21 = self.bodyChunks[0];
                                            bodyChunk21.vel.y = bodyChunk21.vel.y + 3f * num;
                                            if (num5 == 0)
                                            {
                                                BodyChunk bodyChunk22 = self.bodyChunks[0];
                                                bodyChunk22.vel.y = bodyChunk22.vel.y + 3f * num;
                                            }
                                        }
                                        BodyChunk bodyChunk23 = self.bodyChunks[1];
                                        bodyChunk23.vel.y = bodyChunk23.vel.y + 4f * num;
                                        self.jumpBoost = 6f;
                                        if (num5 != 0 && self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x == num5 > 0)
                                        {
                                            BodyChunk bodyChunk24 = self.bodyChunks[0];
                                            bodyChunk24.vel.x = bodyChunk24.vel.x + (float)num5 * num6 * num;
                                            BodyChunk bodyChunk25 = self.bodyChunks[1];
                                            bodyChunk25.vel.x = bodyChunk25.vel.x + (float)num5 * num6 * num;
                                            self.room.PlaySound((num6 >= 9f) ? SoundID.Slugcat_Super_Jump : SoundID.Slugcat_Crouch_Jump, self.mainBodyChunk, false, 1f, 1f);
                                        }
                                    }
                                    if (self.bodyChunks[1].onSlope != 0)
                                    {
                                        if (num5 == -self.bodyChunks[1].onSlope)
                                        {
                                            BodyChunk bodyChunk26 = self.bodyChunks[1];
                                            bodyChunk26.vel.x = bodyChunk26.vel.x + (float)self.bodyChunks[1].onSlope * 8f * num;
                                            return;
                                        }
                                        BodyChunk bodyChunk27 = self.bodyChunks[0];
                                        bodyChunk27.vel.x = bodyChunk27.vel.x + (float)self.bodyChunks[1].onSlope * 1.8f * num;
                                        BodyChunk bodyChunk28 = self.bodyChunks[1];
                                        bodyChunk28.vel.x = bodyChunk28.vel.x + (float)self.bodyChunks[1].onSlope * 1.2f * num;
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
                self.bodyChunks[0].vel.y = 6f * num;
                self.bodyChunks[1].vel.y = 5f * num;
                self.standing = true;
                if (self.GetCat().IsPioneer && save.IsNight)
                {
                    self.jumpBoost = 10f;
                    return;
                }
                if (self.isSlugpup)
                {
                    self.jumpBoost = 4f;
                    return;
                }
                self.jumpBoost = 8f;
            }
            else
            {
                orig(self);
            }
/*
            if (SuperJump.TryGet(self, out var power))
            {
                self.jumpBoost *= 1f + power;
            }
*/
        }

        private void AddImageHook(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, Menu.SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
            if (self.HasMark)
            {
                self.markSquare = new FSprite("pixel", true);
                self.markSquare.scale = 14f;
                self.markSquare.color = Color.Lerp(self.effectColor, Color.white, 0.7f);
                self.Container.AddChild(self.markSquare);
                self.markGlow = new FSprite("Futile_White", true);
                self.markGlow.shader = self.menu.manager.rainWorld.Shaders["FlatLight"];
                self.markGlow.color = self.effectColor;
                self.Container.AddChild(self.markGlow);
            }
            orig(self, ascended);
        }

        private void SessionEndedHook(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {

            var save = game.GetMiscWorld();
            if ((game.Players[0].realizedCreature as Player).playerState.foodInStomach == 10 && game.GetStorySession.characterStats.name == Pioneer)
            {
                game.GetStorySession.characterStats.foodToHibernate = 10;

            }
            else if (game.GetStorySession.characterStats.name == Pioneer)
            {
                game.GetStorySession.characterStats.foodToHibernate = 5;
            }
            orig(self, game, survived, newMalnourished);
        }
        private void HibernateHook(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
        {
            var save = self.room.game.GetMiscWorld();
            if ((self.room.game.Players[0].realizedCreature as Player).slugcatStats.name.value == "Pioneer" &&
                (self.room.game.Players[0].realizedCreature as Player).FoodInRoom(self.room, false) == 10)
            {
                save.IsNight = true;
            }
            else if ((self.room.game.Players[0].realizedCreature as Player).slugcatStats.name.value == "Pioneer")
            {
                save.IsNight = false;
            }
            orig(self);
        }

        private void AddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            var save = self.myBehavior.oracle.room.game.GetMiscWorld();
            if (self.id == Conversation.ID.MoonFirstPostMarkConversation && self.myBehavior.oracle.room.game.GetStorySession.characterStats.name.value == "Pioneer" && save.IsNight)
            {
                self.LoadEventsFromFile(1806);
            }
            else
            {
                orig(self);
            }
        }

        private string FluffyFriend(On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized)
        {
            string text = "creature";
            bool flag = self.DamagedMode && UnityEngine.Random.value < 0.5f;
            if (UnityEngine.Random.value > 0.3f)
            {
                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                {
                    if (self.State.totalPearlsBrought > 5 && !self.DamagedMode)
                    {
                        text = "archaeologist";
                    }
                    else
                    {
                        text = "friend";
                    }
                }
                else if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                {
                    text = "tormentor";
                }
                else
                {
                    text = "creature";
                }
            }
            if (self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Portuguese && (text == "friend" || text == "creature"))
            {
                string text2 = text;
                if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
                {
                    text2 = char.ToUpper(text2[0]).ToString() + text2.Substring(1);
                }
                return text2;
            }
            string str = text;
            string text3 = "fluffy";
            if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
            {
                text3 = char.ToUpper(text3[0]).ToString() + text3.Substring(1);
            }
            return text3 + (flag ? "... " : " ") + str;
        }

        private void InitiateConversationHook(On.MoreSlugcats.CLOracleBehavior.orig_InitateConversation orig, CLOracleBehavior self)
        {
            var save = self.oracle.room.game.GetMiscWorld();
            if (self.oracle.room.game.GetStorySession.characterStats.name.value == "Pioneer")
            {
                self.dialogBox.NewMessage(self.Translate("..."), 200);
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.halcyonStolen)
                {
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        self.dialogBox.NewMessage(self.Translate("...Why... take all I... have..."), 100);
                        return;
                    }
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        self.dialogBox.NewMessage(self.Translate("...Give it... back..."), 100);
                        return;
                    }
                    self.dialogBox.NewMessage(self.Translate("...Bring it back..."), 100);
                    return;
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts > 0)
                {
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        self.dialogBox.NewMessage(self.Translate("...Go away..."), 100);
                        return;
                    }
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        self.dialogBox.NewMessage(self.Translate("...Not forgotten pain..."), 100);
                        return;
                    }
                    if (UnityEngine.Random.value < 0.15f)
                    {
                        self.dialogBox.NewMessage(self.Translate("...So little... left. Why hurt... me more..."), 100);
                        return;
                    }
                    self.dialogBox.NewMessage(self.Translate("...Leave me... alone."), 100);
                    return;
                }
                else
                {
                    if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0 && save.IsNight == false)
                    {
                        self.dialogBox.NewMessage(self.Translate("Blue... fluffy thing."), 100);
                        self.dialogBox.NewMessage(self.Translate("H-hello."), 100);
                        self.dialogBox.NewMessage(self.Translate("...have nothing to offer you."), 100);
                        self.dialogBox.NewMessage(self.Translate("lost... everything."), 100);
                        self.dialogBox.NewMessage(self.Translate("Just a carcass now. waiting for eternity"), 100);
                        self.dialogBox.NewMessage(self.Translate("Your warmth. is welcoming."), 100);
                        self.dialogBox.NewMessage(self.Translate("...stay as long   as you wish"), 100);
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                        return;
                    }
                    else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0 && save.IsNight == true)
                    {
                        self.dialogBox.NewMessage(self.Translate("Blue... bright thing."), 100);
                        self.dialogBox.NewMessage(self.Translate("H-hello."), 100);
                        self.dialogBox.NewMessage(self.Translate("...have nothing to offer you."), 100);
                        self.dialogBox.NewMessage(self.Translate("lost... everything."), 100);
                        self.dialogBox.NewMessage(self.Translate("Just a carcass now. waiting for eternity"), 100);
                        self.dialogBox.NewMessage(self.Translate("Your light. is welcoming."), 100);
                        self.dialogBox.NewMessage(self.Translate("...stay as long   as you wish"), 100);
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                        return;
                    }
                    else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 1)
                    {
                        self.dialogBox.NewMessage(self.Translate("...hello again."), 100);
                        self.dialogBox.NewMessage(self.Translate("Why return?"), 100);
                        self.dialogBox.NewMessage(self.Translate("you... from far away."), 100);
                        self.dialogBox.NewMessage(self.Translate("such a small creature. watching one like me fall . . ."), 100);
                        self.dialogBox.NewMessage(self.Translate(". . .can  barely function. And for how long?"), 100);
                        self.dialogBox.NewMessage(self.Translate("Sister , in the east. she is a bit better."), 100);
                        self.dialogBox.NewMessage(self.Translate("...Maybe she'd appreciate a visit."), 100);
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                        return;
                    }
                    if (self.oracle.room.world.rainCycle.TimeUntilRain < 1600)
                    {
                        self.rainInterrupt = true;
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("The blizzard . . . I can not shelter you . leave"), 100);
                            return;
                        }
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("Go... hide... I won't go anywhere."), 100);
                            return;
                        }
                        self.dialogBox.NewMessage(self.Translate("It is getting too cold . . . even for you."), 100);
                        return;
                    }
                    else
                    {
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("...You're back."), 100);
                            self.dialogBox.NewMessage(self.Translate("Still nothing. to offer"), 100);
                            self.dialogBox.NewMessage(self.Translate("...besides the sound of this hymn"), 100);
                            return;
                        }
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("...It is... warmer... today."), 100);
                            return;
                        }
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("...Soft blue friend. Hello."), 100);
                            return;
                        }
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            self.dialogBox.NewMessage(self.Translate("...Nice to see..."), 100);
                            return;
                        }
                        self.dialogBox.NewMessage(self.Translate("...Thank you... for... company."), 100);
                        return;
                    }
                }
            }
            else { orig(self); }
        }

        private bool CanIPickThisUpHook(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            if (self.GetCat().IsPioneer && obj is Weapon && (obj as Weapon).mode == Weapon.Mode.StuckInWall)
            {
                return true;
            }
            else
            {
                return orig(self, obj);
            }
        }

        private Player.ObjectGrabability GrababilityHook(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (self.GetCat().IsPioneer && obj is Weapon)
            {
                if ((obj as Weapon).mode == Weapon.Mode.StuckInWall)
                {
                    return Player.ObjectGrabability.OneHand;
                }
                return Player.ObjectGrabability.OneHand;
            }

            else
            {
                return orig(self, obj);
            }
        }

        private void Abracadabra(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            var save = self.room.game.GetMiscWorld();
            if (self.slugcatStats.name.value == "Pioneer" && save.IsNight == true)
            {
                self.GetCat().IsPioneer = true;
                self.GetCat().IsFirstBite = true;
                self.slugcatStats.bodyWeightFac = 1.2f;
                self.slugcatStats.poleClimbSpeedFac = 1.45f;
                self.slugcatStats.corridorClimbSpeedFac = 1.4f;
                self.slugcatStats.runspeedFac = 1.3f;
            }
            else if (self.slugcatStats.name.value == "Pioneer" && save.IsNight == false)
            {
                self.GetCat().IsPioneer = true;
                self.GetCat().IsFirstBite = true;
                self.slugcatStats.bodyWeightFac = 1.2f;
                self.slugcatStats.poleClimbSpeedFac = 0.94f;
                self.slugcatStats.corridorClimbSpeedFac = 0.94f;
                self.slugcatStats.runspeedFac = 0.95f;
            }
            else if (!(self.slugcatStats.name.value == "Pioneer"))
            {
                self.GetCat().IsPioneer = false;
            }
        }


        private void RoomViewedHook(On.Room.orig_NowViewed orig, Room self)
        {
            orig(self);
            var save = self.game.GetMiscWorld();
            if (save != null && save.IsNight == true && self.game.GetStorySession.characterStats.name.value == "Pioneer")
            {
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
        }

        private void RoomCameraUpdateHook(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            //if (creature != null && creature is Player && (creature as Player).GetCat().IsPioneer && self.game.IsStorySession && (creature as Player).GetCat().SleptWell)
            var save = self.room.game.GetMiscWorld();
            if (save != null && save.IsNight == true && self.game.GetStorySession.characterStats.name.value == "Pioneer")
            {
                {
                    self.currentPalette.darkness = 0.8f;
                    self.effect_darkness = 0.8f;


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
            else if (save.IsNight == false && save != null && self?.room?.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LightBurn) != null && self.game.GetStorySession.characterStats.name.value == "Pioneer")
            {
                self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LightBurn).amount = 1f;
                self.effect_brightness = 0.25f;
            }
            if (save.IsNight == false && save.NightTutorial == false && self != null && self.hud != null && self.game.rainWorld != null && self.game.GetStorySession.characterStats.name.value == "Pioneer")
            {
                self?.hud?.textPrompt?.AddMessage(self?.game?.rainWorld?.inGameTranslator?.Translate("The Pioneer's eyes are bothered by sunlight."), 0, 300, true, true);
                save.NightTutorial = true;
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
            if (self.GetCat().IsPioneer)
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
            var save = self.room.game.GetMiscWorld();
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
                {
                    self.GetCat().EscapeTimer++;
                    if (self.grabbedBy[0].grabber is Leech)
                    {
                        (self.grabbedBy[0].grabber as Leech).Stun(160);
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
            if (self.GetCat().IsPioneer && self.FoodInStomach == 10 && save.FoodTutorial == false)
            {
                self?.room?.game?.cameras[0]?.hud?.textPrompt?.AddMessage(self?.room?.game?.rainWorld?.inGameTranslator?.Translate("If your belly is full, you will sleep until the next night."), 0, 200, true, true);
                self?.room?.game?.cameras[0]?.hud?.textPrompt?.AddMessage(self?.room?.game?.rainWorld?.inGameTranslator?.Translate("However, you will wake up on an empty stomach."), 0, 200, true, true);
                save.FoodTutorial = true;
            }
        }

        private void SwimSpeed(On.Player.orig_UpdateAnimation orig, Player self)
        {
            orig(self);
            if (self.GetCat().IsPioneer && self.animation == Player.AnimationIndex.DeepSwim)
            {
                self.waterFriction = 0.94f;
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