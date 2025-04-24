using System.Threading.Tasks;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Elements.Core;
using LiteNetLib;
using LiteNetLib.Utils;
using FrooxEngine.UIX;

namespace HostCrashGuard;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.Patch(AccessTools.Method(typeof(LNL_Connection), "OnPeerDisconnected"), prefix: AccessTools.Method(typeof(PatchMethods), "prefix_PeerDisconnected"));
		harmony.Patch(AccessTools.Method(typeof(UserRoot), "OnCommonUpdate"), postfix: AccessTools.Method(typeof(PatchMethods), "buildPopupUI"));
		harmony.PatchAll();
	}

	class PatchMethods {

		private static bool ohshit = false;

		static Slot slot;

		static bool prefix_PeerDisconnected(LNL_Connection __instance, NetPeer peer, DisconnectInfo disconnectInfo) {
			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					ohshit = true;
					Msg("DETECTED CRASH INCOMING! If not for this mod, it would already be too late.");
					return false;
				}
			}
			return true;

		}

		private static void CloseMenuDelegate(IButton button, ButtonEventData eventData) {
			slot.Destroy();
		}

		static void buildPopupUI(UserRoot __instance) {
			if (!ohshit) {
				return;//leave as fast as possible so as to not cause lag
			}
			if (__instance.World.Focus is not World.WorldFocus.Focused) {
				return;//leave if not focused
			}
			ohshit = false;

			Slot RootSlot = __instance.Slot.World.RootSlot;
			Slot UserRootSlot = __instance.Slot;
			Slot WarningSlot = UserRootSlot.AddSlot("Warning", false);
			WarningSlot.ScaleToUser();
			WarningSlot.LocalScale *= 0.0005f;
			slot = WarningSlot;//add reference for later deletion
			float gap = 0.01f;
			Msg(RootSlot);
			UIBuilder UI = RadiantUI_Panel.SetupPanel(WarningSlot, "Host Crash Guard".AsLocaleKey(), new float2(500, 400));
			RadiantUI_Constants.SetupEditorStyle(UI);
			UI.SplitVertically(0.85f, out RectTransform top, out RectTransform bottom, gap);

			UI.NestInto(top);
			UI.Text("The host of one of your sessions has crashed. HostCrashGuard has stopped this world from closing. Please save any unfinished work and close this world manually.", true, null, true, null);
			UI.NestInto(bottom);
			Button closeMenu = UI.Button("Close Menu".AsLocaleKey(), new colorX?(RadiantUI_Constants.Sub.GREEN));

			closeMenu.LocalPressed += CloseMenuDelegate;

			WarningSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
		}
	}
}
