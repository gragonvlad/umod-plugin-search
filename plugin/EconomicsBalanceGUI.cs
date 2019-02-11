﻿using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EconomicsBalanceGUI", "lethal_d0se", "1.1.3", ResourceId = 1670)]
    [Description("Displays a players economics balance on the HUD.")]
    public class EconomicsBalanceGUI : RustPlugin
    {
        // Do NOT edit this file, instead edit EconomicsBalanceGUI.json in server/<identity>/oxide/config

        [PluginReference]
        private Plugin Economics;
        private List<ulong> Looters = new List<ulong>();
        private Dictionary<ulong, double> Balances = new Dictionary<ulong, double>();
        private string GUIColor => GetConfig("GUIColor", "0.1 0.1 0.1 0.75");
        private string GUIAnchorMin => GetConfig("GUIAnchorMin", "0.024 0.04");
        private string GUIAnchorMax => GetConfig("GUIAnchorMax", "0.175 0.08");
        private string GUICurrency => GetConfig("GUICurrency", "M");
        private string GUICurrencySize => GetConfig("GUICurrencySize", "12");
        private string GUICurrencyColor => GetConfig("GUICurrencyColor", "1.0 1.0 1.0 1.0");
        private string GUIBalanceSize => GetConfig("GUIBalanceSize", "12");
        private string GUIBalanceColor => GetConfig("GUIBalanceColor", "1.0 1.0 1.0 1.0");

        private void OnServerInitialized()
        {
            if (Economics)
            {
                timer.Repeat(0.5f, 0, () =>
                {
                    if (Economics.IsLoaded)
                    {
                        foreach (BasePlayer player in BasePlayer.activePlayerList)
                        {
                            double currentBalance = (double)Economics?.Call("Balance", player.UserIDString);

                            if (Balances.ContainsKey(player.userID))
                            {
                                double savedBalance = Balances[player.userID];

                                if (savedBalance != currentBalance)
                                {
                                    if (!Looters.Contains(player.userID))
                                    {
                                        Balances[player.userID] = currentBalance;
                                        GUIRefresh(player);
                                    }
                                }
                            }
                            else
                            {
                                Balances.Add(player.userID, currentBalance);
                                GUIRefresh(player);
                            }
                        }
                    }
                });
            }
        }

        private void Unload()
        {
            if (Economics)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    GUIDestroy(player);
            }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Plugin is loading default configuration.");

            Config["GUIColor"] = GUIColor;
            Config["GUIAnchorMin"] = GUIAnchorMin;
            Config["GUIAnchorMax"] = GUIAnchorMax;
            Config["GUICurrency"] = GUICurrency;
            Config["GUICurrencySize"] = GUICurrencySize;
            Config["GUICurrencyColor"] = GUICurrencyColor;
            Config["GUIBalanceSize"] = GUIBalanceSize;
            Config["GUIBalanceColor"] = GUIBalanceColor;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (Economics && Economics.IsLoaded)
                Balances.Add(player.userID, (double)Economics?.Call("Balance", player.UserIDString));
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (Economics) GUIRefresh(player);
        }

        // TODO: Inventory hook.
        // Figure out how to check if a player has their main inventory open, so we can do Looters.Add(player.userID) & GUIDestroy(player);

        private void OnPlayerLootEnd(PlayerLoot inventory)
        {
            if (Economics)
            {
                BasePlayer player = inventory.GetComponent<BasePlayer>();
                if (player != null && Looters.Contains(player.userID))
                {
                    Looters.Remove(player.userID);
                    GUICreate(player);
                }
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (Economics) GUIDestroy(player);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (Economics)
            {
                GUIDestroy(player);
                Balances.Remove(player.userID);
            }
        }

        private void OnLootEntity(BasePlayer looter, BaseEntity target)
        {
            if (Economics)
            {
                Looters.Add(looter.userID);
                GUIDestroy(looter);
            }
        }

        private void OnLootPlayer(BasePlayer looter, BasePlayer beingLooter)
        {
            if (Economics)
            {
                Looters.Add(looter.userID);
                GUIDestroy(looter);
            }
        }

        private void OnLootItem(BasePlayer looter, Item lootedItem)
        {
            if (Economics)
            {
                Looters.Add(looter.userID);
                GUIDestroy(looter);
            }
        }

        // Interface.

        private void GUICreate(BasePlayer player)
        {
            string currentBalance = (Economics.IsLoaded) ? GetFormattedMoney(player) : "...";

            var GUIElement = new CuiElementContainer();
            var GUIBackground = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = GUIColor
                },
                RectTransform =
                {
                    AnchorMin = GUIAnchorMin,
                    AnchorMax = GUIAnchorMax
                },
                CursorEnabled = false
            }, "Hud", "GUIBackground");
            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = GUICurrency,
                    FontSize = int.Parse(GUICurrencySize),
                    Align = TextAnchor.MiddleCenter,
                    Color = GUICurrencyColor
                },
                RectTransform =
                {
                    AnchorMin = "0 0.1",
                    AnchorMax = "0.15 0.9"
                }
            }, GUIBackground);
            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = currentBalance,
                    FontSize = int.Parse(GUIBalanceSize),
                    Align = TextAnchor.MiddleCenter,
                    Color = GUIBalanceColor
                },
                RectTransform =
                {
                    AnchorMin = "0.15 0.1",
                    AnchorMax = "1 0.9"
                }
            }, GUIBackground);

            CuiHelper.AddUi(player, GUIElement);
        }

        private void GUIDestroy(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "GUIBackground");
        }

        private void GUIRefresh(BasePlayer player)
        {
            GUIDestroy(player);
            GUICreate(player);
        }

        // Helpers.

        private string GetFormattedMoney(BasePlayer player)
        {
            string s = string.Format("{0:C}", (double)Economics?.Call("Balance", player.UserIDString));
            s = s.Substring(1);
            s = s.Remove(s.Length - 3);
            return s;
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
    }
}