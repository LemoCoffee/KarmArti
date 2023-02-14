using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using RWCustom;
using BepInEx;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Expedition;
using MonoMod.Cil;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using Debug = UnityEngine.Debug;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace KarmArti
{

	[BepInPlugin("LemoCoffee.KarmArti", "KarmArti", "1.0.0")]
	public partial class KarmArti : BaseUnityPlugin
	{
		private KarmArtiOptions Options;
		private static ManualLogSource m;

		public KarmArti()
		{
			try
			{
				Options = new KarmArtiOptions(this, Logger);
				m = Logger;
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
				throw;
			}
		}
		private void OnEnable()
		{
			On.RainWorld.OnModsInit += RainWorldOnOnModsInit;


		}

		private bool IsInit;
		private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
		{
			orig(self);
			try
			{
				if (IsInit) return;

				//Your hooks go here
				On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
				On.GameSession.ctor += GameSessionOnctor;

				new ILHook(typeof(RegionGate).GetMethod("get_MeetRequirement", BindingFlags.Instance | BindingFlags.Public), MeetRequirementHook);
                IL.Player.Update += Player_Update;
                IL.Scavenger.Update += Scavenger_Update;


				//new Hook(
				//  typeof(RegionGate).GetMethod("get_MeetRequirement", BindingFlags.Instance | BindingFlags.Public),
				//  typeof(KarmArti).GetMethod("ArtiMeetRequirement", BindingFlags.Static | BindingFlags.Public)
				//);

				MachineConnector.SetRegisteredOI("LemoCoffee.KarmArti", Options);
				IsInit = true;


			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
				throw;
			}
		}

        

        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
		{
			orig(self);
			ClearMemory();
		}
		private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
		{
			orig(self, game);
			ClearMemory();
		}

		#region Helper Methods

		private void ClearMemory()
		{
			//If you have any collections (lists, dictionaries, etc.)
			//Clear them here to prevent a memory leak
			//YourList.Clear();
		}

		#endregion

		public static void MeetRequirementHook(ILContext il)
        {
			try
            {
				var c = new ILCursor(il);
				ILLabel l = null;
				c.GotoNext(i => i.MatchCallOrCallvirt<Creature>("get_grasps"));
				c.GotoPrev(MoveType.Before, i => i.MatchLdloc(0));
				l = il.DefineLabel(c.Next);
				c.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
				c.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
				c.Emit(OpCodes.Br, l);
			} 
			catch(Exception e)
            {
				m.LogError(e);
				throw (e);
            } 

        }

		private void Player_Update(ILContext il)
		{
			try
			{
				var c = new ILCursor(il);
				ILLabel l = null;
				c.GotoNext(i => i.MatchCallOrCallvirt<HUD.HUD>("get_karmaMeter"));
				c.GotoPrev(i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"));
				c.GotoNext(MoveType.Before, i => i.MatchLdarg(0));
				l = il.DefineLabel(c.Next);
				c.GotoPrev(i => i.MatchBrfalse(out _));
				c.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
				c.Emit(OpCodes.Br, l);
			}
			catch (Exception e)
			{
				m.LogError(e);
				throw (e);
			}
		}

		private void Scavenger_Update(ILContext il)
		{
			try
			{
				var c = new ILCursor(il);
				ILLabel l = null;
				c.GotoNext(i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"));
				c.GotoNext(MoveType.Before, i => i.MatchLdarg(0));
				l = il.DefineLabel(c.Next);
				c.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
				c.GotoPrev(MoveType.After, i => i.MatchBrfalse(out _));
				c.Emit(OpCodes.Br, l);
				//m.LogError("Fuck");
			}
			catch (Exception e)
			{
				m.LogError(e);
				throw (e);
			}
		}
		/*public static bool ArtiMeetRequirement(Func<RegionGate, bool> orig, RegionGate self)
		{
			if (self.room.game.Players.Count == 0 || (self.room.game.FirstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable))
			{
				return false;
			}
			Player player;
			if (ModManager.CoopAvailable && self.room.game.AlivePlayers.Count > 0)
			{
				player = (self.room.game.FirstAlivePlayer.realizedCreature as Player);
			}
			else
			{
				player = (self.room.game.Players[0].realizedCreature as Player);
			}
			int num = player.Karma;
			if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && player.grasps.Length != 0)
			{
				for (int i = 0; i < player.grasps.Length; i++)
				{
					if (player.grasps[i] != null && player.grasps[i].grabbedChunk != null && player.grasps[i].grabbedChunk.owner is Scavenger)
					{
						num = (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma + (player.grasps[i].grabbedChunk.owner as Scavenger).abstractCreature.karmicPotential;
						break;
					}
				}
			}
			bool flag = ModManager.MSC && self.karmaRequirements[(!self.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock && self.room.game.session is StoryGameSession && (self.room.game.session as StoryGameSession).saveState.hasRobo && (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark && self.room.world.region.name != "SL" && self.room.world.region.name != "MS" && self.room.world.region.name != "DM";
			int num2 = -1;
			bool flag2 = false;
			if (int.TryParse(self.karmaRequirements[(!self.letThroughDir) ? 1 : 0].value, NumberStyles.Any, CultureInfo.InvariantCulture, out num2))
			{
				flag2 = (num2 - 1 <= num);
			}
			return (ModManager.MSC && ModManager.Expedition && self.room.game.rainWorld.ExpeditionMode && self.karmaRequirements[(!self.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock && ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.room.world.region.name == "UW" && self.room.abstractRoom.name.Contains("LC")) || (flag || flag2) || self.unlocked;
			//return orig(self);

			//     ----------
			// modify this ^ return value to whatever you want
			// but try to call orig in some way, please
		}*/
	}
}

//Stolen code that was annoying to find in the massive function that is Update()
/*
 * else if (ModManager.MSC && this.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.AI == null && this.room.game.cameras[0] != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.karmaMeter != null)
		{
			Scavenger scavenger = null;
			if (base.grasps.Length != 0)
			{
				for (int m = 0; m < base.grasps.Length; m++)
				{
					if (base.grasps[m] != null && base.grasps[m].grabbedChunk != null && base.grasps[m].grabbedChunk.owner is Scavenger && (base.grasps[m].grabbedChunk.owner as Scavenger).dead)
					{
						scavenger = (base.grasps[m].grabbedChunk.owner as Scavenger);
						break;
					}
				}
			}
			if (scavenger != null && this.room.game.cameras[0].hud.karmaMeter.forceVisibleCounter < 40)
			{
				this.room.game.cameras[0].hud.karmaMeter.forceVisibleCounter = 40;
			}
			if (this.room.game.cameras[0].hud.karmaMeter.forceVisibleCounter > 0)
			{
				if (scavenger != null)
				{
					int num = Mathf.Clamp((base.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma + scavenger.abstractCreature.karmicPotential, 0, 9);
					int num2 = 5;
					if (num > 4)
					{
						num2 = Mathf.Max(6, num);
					}
					num2 = Mathf.Max(num2, Mathf.Clamp(this.KarmaCap, 4, 9));
					if (num > num2)
					{
						num2 = num;
					}
					this.room.game.cameras[0].hud.karmaMeter.ClearScavengerFlash();
					this.room.game.cameras[0].hud.karmaMeter.displayKarma = new IntVector2(num, num2);
				}
				else
				{
					int x = this.room.game.cameras[0].hud.karmaMeter.displayKarma.x;
					int num3 = Mathf.Min(this.Karma, this.KarmaCap);
					if (x > num3)
					{
						this.room.game.cameras[0].hud.karmaMeter.DropScavengerFlash();
					}
					this.room.game.cameras[0].hud.karmaMeter.displayKarma = new IntVector2(num3, Mathf.Clamp(this.KarmaCap, 4, 9));
				}
				this.room.game.cameras[0].hud.karmaMeter.karmaSprite.element = Futile.atlasManager.GetElementWithName(KarmaMeter.KarmaSymbolSprite(true, this.room.game.cameras[0].hud.karmaMeter.displayKarma));
			}
		}
		 


		public virtual bool MeetRequirement
	{
		get
		{
			if (this.room.game.Players.Count == 0 || (this.room.game.FirstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable))
			{
				return false;
			}
			Player player;
			if (ModManager.CoopAvailable && this.room.game.AlivePlayers.Count > 0)
			{
				player = (this.room.game.FirstAlivePlayer.realizedCreature as Player);
			}
			else
			{
				player = (this.room.game.Players[0].realizedCreature as Player);
			}
			int num = player.Karma;
			if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer && player.grasps.Length != 0)
			{
				for (int i = 0; i < player.grasps.Length; i++)
				{
					if (player.grasps[i] != null && player.grasps[i].grabbedChunk != null && player.grasps[i].grabbedChunk.owner is Scavenger)
					{
						num = (this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma + (player.grasps[i].grabbedChunk.owner as Scavenger).abstractCreature.karmicPotential;
						break;
					}
				}
			}
			bool flag = ModManager.MSC && this.karmaRequirements[(!this.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock && this.room.game.session is StoryGameSession && (this.room.game.session as StoryGameSession).saveState.hasRobo && (this.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark && this.room.world.region.name != "SL" && this.room.world.region.name != "MS" && this.room.world.region.name != "DM";
			int num2 = -1;
			bool flag2 = false;
			if (int.TryParse(this.karmaRequirements[(!this.letThroughDir) ? 1 : 0].value, NumberStyles.Any, CultureInfo.InvariantCulture, out num2))
			{
				flag2 = (num2 - 1 <= num);
			}
			return (ModManager.MSC && ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && this.karmaRequirements[(!this.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock && ExpeditionData.slugcatPlayer == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.room.world.region.name == "UW" && this.room.abstractRoom.name.Contains("LC")) || (flag || flag2) || this.unlocked;
		}
	}
	 

*/
//Thank you Noir you saint