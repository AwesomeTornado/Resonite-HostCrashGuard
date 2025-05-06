using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Net.Sockets;
using Elements.Core;
using LiteNetLib;
using FrooxEngine.UIX;

namespace HostCrashGuard;

public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.2.0"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> NetworkPatchesEnabled = new ModConfigurationKey<bool>("Network Patches", "Enables/Disables all network crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> CatchTimeouts = new ModConfigurationKey<bool>("Catch Timeouts", "Stops network timeouts from closing the world. (host crash, network instability)", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> CatchHostDisconnect = new ModConfigurationKey<bool>("Catch Host Disconnects", "Stops remote disconnections from closing the world. (kicks, some crashes)", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<float2> DialogSize = new ModConfigurationKey<float2>("Popup Size", "Changes the size of the network error popup.", () => new float2(300f, 250f));

	private static ModConfiguration Config;

	public override void OnEngineInit() {
		Config = GetConfiguration();
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
			if (!Config.GetValue(NetworkPatchesEnabled)) {
				return true;
			}
			World world = Traverse.Create(__instance).Field("world").GetValue<World>();
			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout && Config.GetValue(CatchTimeouts)) {
					Fail("The network connection has timed out.", world);
					return false;
				} else if (disconnectInfo.Reason == DisconnectReason.RemoteConnectionClose && Config.GetValue(CatchHostDisconnect)) {
					Fail("The host has disconnected from your client.", world);
					return false;
				}
			}
			return true;
		}

		static void Fail(string reason, World world) {
			Msg("Prevented world close with reason: " + reason);
			var w = Userspace.UserspaceWorld;

			w.RunSynchronously(() => {
				Slot slot = w.RootSlot.LocalUserSpace.AddSlot("Crash Guard Dialog", false);
				UIBuilder uIBuilder = RadiantUI_Panel.SetupPanel(slot, "Host Crash Guard", Config.GetValue(DialogSize), pinButton: false);
				RadiantUI_Constants.SetupEditorStyle(uIBuilder);
				uIBuilder.VerticalLayout(4f);
				uIBuilder.Style.MinHeight = 24f;
				uIBuilder.Text(reason + " HostCrashGuard has stopped " + world.Name + " from closing. Please save any unfinished work and close this world.");
				uIBuilder.HorizontalLayout(4f);

				uIBuilder.Button("Exit World", new colorX?(RadiantUI_Constants.Sub.RED)).LocalPressed +=
				(IButton button, ButtonEventData eventData) => {
					Userspace.ExitWorld(world);
					slot.Destroy();
				};
				uIBuilder.Button("Close Menu", new colorX?(RadiantUI_Constants.Sub.GREEN)).LocalPressed +=
				(IButton button, ButtonEventData eventData) => slot.Destroy();

				slot.PositionInFrontOfUser(float3.Backward, null, 0.6f);
				slot.LocalScale *= 0.001f;
			});
		}

	}
}
