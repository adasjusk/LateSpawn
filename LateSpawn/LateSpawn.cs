using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Events;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;

namespace LateSpawn
{
	public class LateSpawn : Plugin
	{
		private readonly Dictionary<RoleTypeId, double> weightedRoles = new Dictionary<RoleTypeId, double>
		{
			{ RoleTypeId.ClassD,       40.0 },
			{ RoleTypeId.Scientist,    20.0 },
			{ RoleTypeId.FacilityGuard, 60.0 },
			{ RoleTypeId.Scp3114,       5.0 },
		};
		private const double LateJoinLimitSeconds = 60.0;
		private const int SpawnDelayMs = 1500;
		private const ushort BroadcastDuration = 8;
		private readonly Random rnd = new Random();
		private DateTime roundStart;
		private bool roundActive;
		public override string Name => "LateSpawn";
		public override string Author => "adasjusk";
		public override string Description => "Late joins player to a class";
		public override Version Version => new Version(2, 0, 0);
		public override Version RequiredApiVersion => new Version(1, 1, 7);
		public override void Enable()
		{
			ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
			ServerEvents.RoundStarted += OnRoundStarted;
			PlayerEvents.Joined += OnPlayerJoin;
		}
		public override void Disable()
		{
			ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
			ServerEvents.RoundStarted -= OnRoundStarted;
			PlayerEvents.Joined -= OnPlayerJoin;
		}
		private void OnWaitingForPlayers()
		{
			roundActive = false;
			roundStart = DateTime.MinValue;
		}
		private void OnRoundStarted()
		{
			roundActive = true;
			roundStart = DateTime.UtcNow;
		}
		private async void OnPlayerJoin(PlayerJoinedEventArgs ev)
		{
			if (!roundActive)
				return;
			var player = ev.Player;
			if (player == null || player.IsHost || player.IsDummy)
				return;
			await Task.Delay(SpawnDelayMs);
			if (player == null || player.IsDestroyed)
				return;
			double secondsLate = (DateTime.UtcNow - roundStart).TotalSeconds;
			if (secondsLate > LateJoinLimitSeconds)
			{
				player.Role = RoleTypeId.Spectator;
				player.SendBroadcast($"You joined too late ({secondsLate:F1}s).\nYou are now a Spectator.", BroadcastDuration, Broadcast.BroadcastFlags.Normal, false);
			}
			else
			{
				RoleTypeId role = ChooseWeightedRole();
				player.Role = role;
				player.SendBroadcast($"You joined {secondsLate:F1}s late.\nYou have been spawned as a {role}.", BroadcastDuration, Broadcast.BroadcastFlags.Normal, false);
			}
		}
		private RoleTypeId ChooseWeightedRole()
		{
			double total = weightedRoles.Values.Sum();
			double roll = rnd.NextDouble() * total;
			foreach (var entry in weightedRoles)
			{
				roll -= entry.Value;
				if (roll <= 0.0)
					return entry.Key;
			}
			return weightedRoles.Keys.First();
}	}	}