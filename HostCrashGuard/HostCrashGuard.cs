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
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.Patch(AccessTools.Method(typeof(LNL_Connection), "OnPeerDisconnected"), prefix: AccessTools.Method(typeof(PatchMethods), "prefix_PeerDisconnected"));
		harmony.Patch(AccessTools.Method(typeof(Userspace), "OnCommonUpdate"), prefix: AccessTools.Method(typeof(PatchMethods), "buildPopupUI"));
		harmony.PatchAll();
	}

	class PatchMethods {

		private static bool ohshit = false;

		static bool prefix_PeerDisconnected(LNL_Connection __instance, NetPeer peer, DisconnectInfo disconnectInfo) {
			Msg("Prefix from peer disconnected");
			Warn(string.Concat(new string[]
			{
				"LNL Connection Disconnected: ",
				(peer != null) ? peer.ToString() : null,
				", reason: ",
				disconnectInfo.Reason.ToString(),
				", socketErrorCode: ",
				disconnectInfo.SocketErrorCode.ToString()
			}));

			if (disconnectInfo.SocketErrorCode == SocketError.Success) {
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					Msg("DETECTED CRASH INCOMING!!!!! If not for this mod, it would already be too late.");
					ohshit = true;
					return false;
				}
			}
			return true;
			
		}

		[SyncMethod(typeof(Delegate), null)]
		private void Deny(IButton button, ButtonEventData eventData) {
			if (base.World != Userspace.UserspaceWorld) {
				return;
			}
			TaskCompletionSource<HostAccessPermission> task = this._task;
			if (task != null) {
				task.TrySetResult(HostAccessPermission.Denied);
			}
			this._task = null;
			base.Slot.Destroy();
		}

		// Token: 0x06008250 RID: 33360 RVA: 0x00299925 File Offset: 0x00297B25
		[SyncMethod(typeof(Delegate), null)]
		private void Allow(IButton button, ButtonEventData eventData) {
			if (base.World != Userspace.UserspaceWorld) {
				return;
			}
			TaskCompletionSource<HostAccessPermission> task = this._task;
			if (task != null) {
				task.TrySetResult(HostAccessPermission.Allowed);
			}
			this._task = null;
			base.Slot.Destroy();
		}

		static void buildPopupUI(Userspace __instance) {
			if (!ohshit) {
				return;//leave as fast as possible so as to not cause lag
			}

			
			Slot slot = __instance.Slot;
			UIBuilder ui = RadiantUI_Panel.SetupPanel(slot, "Host Crash Guard", new float2(400f, 300f), true, true);
			float3 localScale = slot.LocalScale;
			slot.LocalScale = (localScale) * 0.001f;
			RadiantUI_Constants.SetupEditorStyle(ui, false);
			ui.VerticalLayout(4f, 0f, null, null, null);
			ui.Style.MinHeight = 64f;
			UIBuilder uibuilder = ui;
			LocaleString localeString = "The host of this sesson has crashed.";
			uibuilder.Text(in localeString, true, null, true, null);
			ui.Style.MinHeight = 32f;
			Text hostText = new Text();
			UIBuilder uibuilder2 = ui;
			localeString = "---";
			hostText = uibuilder2.Text(in localeString, true, null, true, null);
			hostText.Color.Value = new colorX(0f, 1f, 1f, 1f, ColorProfile.sRGB);
			ui.Style.MinHeight = 24f;
			Text reasonText = new Text();
			UIBuilder uibuilder3 = ui;
			localeString = "---";
			reasonText = uibuilder3.Text(in localeString, true, null, true, null);
			ui.Style.MinHeight = 32f;
			ui.HorizontalLayout(4f, 0f, null);
			UIBuilder uibuilder4 = ui;
			localeString = "Security.HostAccess.Allow".AsLocaleKey(null, true, null);
			colorX? colorX = new colorX?(RadiantUI_Constants.Sub.GREEN);
			Button openButton = uibuilder4.Button(in localeString, in colorX, new ButtonEventHandler(this.Allow), 0f);
			UIBuilder uibuilder5 = ui;
			localeString = "Security.HostAccess.Deny".AsLocaleKey(null, true, null);
			colorX = new colorX?(RadiantUI_Constants.Sub.RED);
			uibuilder5.Button(in localeString, in colorX, new ButtonEventHandler(this.Deny), 0f);
			this._allowButton.Target = openButton;
			/*base.RunInUpdates(2, delegate {
				Slot temp = base.World.AddSlot("TEMP", true);
				temp.GlobalPosition = float3.Up;
				Slot prevParent = base.Slot.Parent;
				base.Slot.Parent = temp;
				base.RunInUpdates(2, delegate {
					this.Slot.Parent = prevParent;
					temp.Destroy();
				});
			});*/
		}
	}
}
