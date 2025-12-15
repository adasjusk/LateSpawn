using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Events;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;
using LabApi.Features;

namespace LateSpawn
{
    public class LateSpawn : Plugin
    {
        private DateTime roundStart;

        private bool roundActive;

        private readonly Random rnd = new Random();

        private readonly Dictionary<RoleTypeId, double> weightedRoles = new Dictionary<RoleTypeId, double>
    {
        {
            (RoleTypeId)1,
            40.0
        },
        {
            (RoleTypeId)6,
            20.0
        },
        {
            (RoleTypeId)15,
            60.0
        },
        {
            (RoleTypeId)23,
            5.0
        }
    };

        private const int LateJoinLimitSeconds = 60;

        public override string Name => "LateSpawn";

        public override string Author => "adasjusk";

        public override string Description => "Late joins player to a class";

        public override Version Version => new Version(1, 5, 0);

        public override Version RequiredApiVersion => new Version(1, 1, 4);

        public override void Enable()
        {
            ServerEvents.WaitingForPlayers += new LabEventHandler(OnWaitingForPlayers);
            ServerEvents.RoundStarted += new LabEventHandler(OnRoundStarted);
            PlayerEvents.Joined += OnPlayerJoin;
        }

        public override void Disable()
        {
            ServerEvents.WaitingForPlayers -= new LabEventHandler(OnWaitingForPlayers);
            ServerEvents.RoundStarted -= new LabEventHandler(OnRoundStarted);
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
            {
                return;
            }
            await Task.Delay(1500);
            if (ev.Player != null)
            {
                double totalSeconds = (DateTime.UtcNow - roundStart).TotalSeconds;
                bool num = totalSeconds > 60.0;
                RoleTypeId val = ChooseWeightedRole();
                string text;
                if (num)
                {
                    ev.Player.Role = (RoleTypeId)2;
                    text = $"You joined too late ({totalSeconds:F1}s).\nYou are now a Spectator.";
                }
                else
                {
                    ev.Player.Role = val;
                    text = $"You joined {totalSeconds:F1}s late.\nYou have been spawned as a {val}.";
                }
                ev.Player.SendBroadcast(text, 8, Broadcast.BroadcastFlags.Normal, false);
            }
        }

        private RoleTypeId ChooseWeightedRole()
        {
            double num = weightedRoles.Values.Sum();
            double num2 = rnd.NextDouble() * num;
            foreach (KeyValuePair<RoleTypeId, double> weightedRole in weightedRoles)
            {
                num2 -= weightedRole.Value;
                if (num2 <= 0.0)
                {
                    return weightedRole.Key;
                }
            }
            return weightedRoles.Keys.First();
        }
    }
}
