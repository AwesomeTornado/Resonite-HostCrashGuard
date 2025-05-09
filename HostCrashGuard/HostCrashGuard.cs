using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Net.Sockets;
using Elements.Core;
using LiteNetLib;
using FrooxEngine.UIX;
using System;
using System.Reflection;
using System.Xml.Linq;

namespace HostCrashGuard;

public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.2.0"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> NetworkPatchesEnabled = new ModConfigurationKey<bool>("Network Patches", "Enable all network crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> CatchTimeouts = new ModConfigurationKey<bool>("Catch Timeouts", "Stop network timeouts from closing the world. (Host crash, network instability)", () => true);

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

	//// Token: 0x06002BB8 RID: 11192 RVA: 0x000C52F0 File Offset: 0x000C34F0
	//public static void BuildInspectorUI(Worker worker, UIBuilder ui, Predicate<ISyncMember> memberFilter = null)

	[HarmonyPatch(typeof(WorkerInspector), nameof(WorkerInspector.BuildInspectorUI))]
	class InvalidComponentPatch {
		static bool Prefix(WorkerInspector __instance, Worker worker, UIBuilder ui, Predicate<ISyncMember> memberFilter = null) {
			Msg(Environment.StackTrace);
			Msg("This is the start of the function");
			Msg("for (int i = 0; i < worker.SyncMemberCount; i++) {");
			for (int i = 0; i < worker.SyncMemberCount; i++) {
				Msg("ISyncMember member = worker.GetSyncMember(i);");
				ISyncMember member = worker.GetSyncMember(i);
				Msg("if (worker.GetSyncMemberFieldInfo(i).GetCustomAttribute<HideInInspectorAttribute>() == null && (memberFilter == null || memberFilter(member))) {");
				if (worker.GetSyncMemberFieldInfo(i).GetCustomAttribute<HideInInspectorAttribute>() == null && (memberFilter == null || memberFilter(member))) {
					Msg("SyncMemberEditorBuilder.Build(member, worker.GetSyncMemberName(i), worker.GetSyncMemberFieldInfo(i), ui, 0.3f);");
					Msg("i is " + i);
					Msg("SyncMember is " + worker.GetSyncMemberName(i));
					Msg("FieldInfo is " + worker.GetSyncMemberFieldInfo(i).ToString());
					SyncMemberEditorBuilder.Build(member, worker.GetSyncMemberName(i), worker.GetSyncMemberFieldInfo(i), ui, 0.3f);
					Msg("past Build function");
				}
			}
			Msg("for (int j = 0; j < worker.SyncMethodCount; j++) {");
			for (int j = 0; j < worker.SyncMethodCount; j++) {
				Msg("SyncMethodInfo info;");
				SyncMethodInfo info;
				Msg("Delegate method;");
				Delegate method;
				Msg("worker.GetSyncMethodData(j, out info, out method);");
				worker.GetSyncMethodData(j, out info, out method);
				Msg("if (method != null) {");
				if (method != null) {
					//SyncMemberEditorBuilder.BuildSyncMethod(method, info.methodType, info.method, ui);
					Msg("That one line got called");
				}
			}
			Msg("Skipping original function");
			return false;
		}
		static void Postfix() {
			Msg(Environment.StackTrace);
			Msg("This is the end of the function");
		}
	}
	[HarmonyPatch(typeof(SyncMemberEditorBuilder), nameof(SyncMemberEditorBuilder.Build))]
	class InvalidComponentPatch_2 {
		//public static void Build(ISyncMember member, string name, FieldInfo fieldInfo, UIBuilder ui, float labelSize = 0.3f)
		static void Prefix(ISyncMember member, string name, FieldInfo fieldInfo, UIBuilder ui, float labelSize = 0.3f) {
			Msg("	SectionAttribute section = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<SectionAttribute>() : null);");
			SectionAttribute section = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<SectionAttribute>() : null);
			Msg("	SyncObject syncObject = member as SyncObject;");
			SyncObject syncObject = member as SyncObject;
			Msg("	if (syncObject != null) {");
			if (syncObject != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildSyncObject(syncObject, name, fieldInfo, ui, labelSize);
				return;
			}
			Msg("	IField field = member as IField;");
			IField field = member as IField;
			Msg("	if (field != null) {");
			if (field != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildField(field, name, fieldInfo, ui, labelSize);
				return;
			}
			Msg("	SyncPlayback playback = member as SyncPlayback;");
			SyncPlayback playback = member as SyncPlayback;
			Msg("	if (playback != null) {");
			if (playback != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildPlayback(playback, name, fieldInfo, ui, labelSize);
				return;
			}
			Msg("	ISyncList list = member as ISyncList;");
			ISyncList list = member as ISyncList;
			Msg("	if (list != null) {");
			if (list != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildList(list, name, fieldInfo, ui);
				return;
			}
			Msg("	ISyncBag bag = member as ISyncBag;");
			ISyncBag bag = member as ISyncBag;
			Msg("	if (bag != null) {");
			if (bag != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildBag(bag, name, fieldInfo, ui);
				return;
			}
			Msg("	ISyncArray array = member as ISyncArray;");
			ISyncArray array = member as ISyncArray;
			Msg("	if (array != null) {");
			if (array != null) {
				Msg("	return");
				//SyncMemberEditorBuilder.BuildArray(array, name, fieldInfo, ui, labelSize);
				return;
			}
			Msg("	EmptySyncElement empty = member as EmptySyncElement;");
			EmptySyncElement empty = member as EmptySyncElement;
			Msg("	if (empty == null) {");
			if (empty == null) {
				Msg("	return (null)");
				return;
			}
			Msg("	return");
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
				}/* else if (disconnectInfo.Reason == DisconnectReason.RemoteConnectionClose) {
					Fail("The host has disconnected from your client.", world);
					return false;
				}*/
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
