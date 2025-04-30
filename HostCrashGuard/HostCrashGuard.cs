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
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ProtoFlux.Nodes.Core;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace HostCrashGuard;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.0.1"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.Patch(AccessTools.Method(typeof(LNL_Connection), "OnPeerDisconnected"), prefix: AccessTools.Method(typeof(PatchMethods), "onPeerDisconnected"));
		harmony.Patch(AccessTools.Method(typeof(UserRoot), "OnCommonUpdate"), postfix: AccessTools.Method(typeof(PatchMethods), "buildPopupUI"));
		harmony.Patch(AccessTools.Method(typeof(Elements.Core.Coder<Decimal>), "Mod"), prefix: AccessTools.Method(typeof(PatchMethods), "VarDecMod"));
		harmony.PatchAll();
	}


	class PatchMethods {

		private static bool ohshit = false;

		static Slot slot;

		static bool VarDecMod(Decimal a, Decimal b) {
			if (b == 0) {
				return false;
			}
			return true;
		}

		static bool onPeerDisconnected(LNL_Connection __instance, NetPeer peer, DisconnectInfo disconnectInfo) {
			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					ohshit = true;
					Msg("The host has taken more than the maximum allowed time to respond, patching out connection close.");
					return false;
				}
				switch (disconnectInfo.Reason) {
					case DisconnectReason.Timeout:
						ohshit = true;
						Msg("The host has taken more than the maximum allowed time to respond, preventing world close.");
						return false;
						break;
					case DisconnectReason.RemoteConnectionClose:
						ohshit = true;
						Msg("The host has closed the connection intentionally, preventing world close.");
						return false;
						break;
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
			if (__instance.LocalUser.UserID != __instance.ActiveUser.UserID) {
				return;
			}
			UserRoot localUserRoot = __instance.LocalUserRoot;
			//UserRoot localUserRoot = __instance;
			ohshit = false;

			Slot RootSlot = localUserRoot.Slot.World.RootSlot;
			Slot UserRootSlot = localUserRoot.Slot;
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
