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

namespace HostCrashGuard;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.Patch(AccessTools.Method(typeof(LNL_Connection), "OnPeerDisconnected"), prefix: AccessTools.Method(typeof(PatchMethods), "prefix_PeerDisconnected"));
		harmony.Patch(AccessTools.Method(typeof(LNL_Connection), "OnPeerDisconnected"), postfix: AccessTools.Method(typeof(PatchMethods), "postfix_PeerDisconnected"));
		harmony.PatchAll();
	}

	//private async Task RunSessionDownload()
	class PatchMethods {

		private static bool ohshit = false;

		static bool prefix_PeerDisconnected(LNL_Connection __instance, NetPeer peer, DisconnectInfo disconnectInfo) {
			Msg("Prefix from peer disconnected");
			Warn(string.Concat(new string[]
			{
				"HostCrashGuard: LNL Connection Disconnected: ",
				(peer != null) ? peer.ToString() : null,
				", reason: ",
				disconnectInfo.Reason.ToString(),
				", socketErrorCode: ",
				disconnectInfo.SocketErrorCode.ToString()
			}));

			UniLog.Log(string.Concat(new string[]
			{
				"HostCrashGuard: LNL Connection Disconnected: ",
				(peer != null) ? peer.ToString() : null,
				", reason: ",
				disconnectInfo.Reason.ToString(),
				", socketErrorCode: ",
				disconnectInfo.SocketErrorCode.ToString()
			}), true);

			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					Msg("HostCrashGuard: DETECTED CRASH INCOMING!!!!! If not for this mod, it would already be too late.");
					ohshit = true;
					return false;
				}
			}
			return true;
			
		}

		static void postfix_PeerDisconnected(LNL_Connection __instance) {
			Msg("Postfix from peer disconnected");
		}
	}
}
