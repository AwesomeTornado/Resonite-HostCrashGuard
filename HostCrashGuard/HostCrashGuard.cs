using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Net.Sockets;
using Elements.Core;
using LiteNetLib;
using FrooxEngine.UIX;
using System.Reflection;

namespace HostCrashGuard;

public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.1.1"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.PatchAll();
		Msg("HostCrashGuard loaded.");
	}

	[HarmonyPatch(typeof(Coder<decimal>), nameof(Coder<decimal>.CanDivide))]
	class DecimalDivZeroPatch {
		static bool Prefix(decimal dividend, decimal divisor, ref bool __result) {
			if (divisor == 0) {
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LNL_Connection), nameof(LNL_Connection.OnPeerDisconnected))]
	class PeerDisconnectedPatch {
		static bool Prefix(LNL_Connection __instance, NetPeer peer, DisconnectInfo disconnectInfo) {
			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					Fail("The network connection has timed out.");
					return false;
				} else if (disconnectInfo.Reason == DisconnectReason.RemoteConnectionClose) {
					Fail("The host has disconnected from your client.");
					return false;
				}
			}
			return true;
		}

		static void Fail(string reason) {
			Msg("Prevented world close with reason: " + reason);
			var w = Userspace.UserspaceWorld;
			w.RunSynchronously(() => {
				Slot slot = w.RootSlot.LocalUserSpace.AddSlot("Crash Guard Dialog");
				UIBuilder uIBuilder = RadiantUI_Panel.SetupPanel(slot, "Host Crash Guard", new float2(300f, 200f), pinButton: false);
				RadiantUI_Constants.SetupEditorStyle(uIBuilder);
				uIBuilder.VerticalLayout(4f);
				uIBuilder.Style.MinHeight = 24f;
				uIBuilder.Text(reason + " HostCrashGuard has stopped this world from closing. Please save any unfinished work and close this world manually.");

				slot.PositionInFrontOfUser(float3.Backward, null, 0.6f);
				slot.LocalScale *= 0.001f;
			});
		}
	}
}
