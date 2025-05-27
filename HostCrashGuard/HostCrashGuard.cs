using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Net.Sockets;
using Elements.Core;
using LiteNetLib;
using FrooxEngine.UIX;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace HostCrashGuard;

public class HostCrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "2.4.5"; //Changing the version here updates it in all locations needed
	public override string Name => "HostCrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-HostCrashGuard";

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> NetworkPatchesEnabled = new ModConfigurationKey<bool>("Network Patches", "Enable all network crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> ComponentPatchesEnabled = new ModConfigurationKey<bool>("Component Patches", "Enable all component related crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<float2> DialogSize = new ModConfigurationKey<float2>("Popup Size", "Changes the size of the network error popup.", () => new float2(300f, 250f));

	private static ModConfiguration Config;

	public override void OnEngineInit() {
		Config = GetConfiguration();
		Harmony harmony = new Harmony("com.__Choco__.HostCrashGuard");
		harmony.PatchAll();
		Msg("HostCrashGuard loaded.");
	}

	[HarmonyPatch(typeof(ComponentSelector), "GetCustomGenericType")]
	class ComponentSelectorValidator {
		private static void Postfix(ref Type? __result) {
			if (__result is null || ContainsAnyGenericParameters(__result) || !Config.GetValue(ComponentPatchesEnabled)) {
				return;
			}

			Component component = (Component)((object)TypeManager.Instantiate(__result));
			if (component is null) {
				Msg("Component is null, returning");
				return;
			}

			Traverse.Create(component).Method("InitializeSyncMembers").GetValue();

			for (int i = 0; i < component.SyncMemberCount; i++) {
				ISyncMember syncMember = component.GetSyncMember(i);
				bool flag = false;
				IField? field = syncMember as IField;
				ISyncDelegate? syncDelegate = field as ISyncDelegate;
				ISyncRef? syncRef = field as ISyncRef;
				AssetRef<ITexture2D>? texRef = field as AssetRef<ITexture2D>;
				flag |= syncMember is null;
				flag |= field is null;
				flag |= texRef is not null;
				flag |= syncDelegate is not null;
				flag |= syncRef is not null;
				flag |= component.GetSyncMemberFieldInfo(i).GetCustomAttribute<HideInInspectorAttribute>() is not null;
				//the field.valuetypes get checked below to ensure that they don't get called when field is null.
				if (flag is false && !field.ValueType.IsMatrixType() && !field.ValueType.IsSphericalHarmonicsType()) {
					if (InspectorRecursionLimiter.CanBeRendered(field.GetType()) is false) {
						__result = null;
						return;
					}
				}
			}
		}

		private static bool ContainsAnyGenericParameters(Type type) {
			if (type.ContainsGenericParameters) {
				return true;
			}
			bool containsGenerics = false;
			foreach (Type innerType in type.GetGenericArguments()) {
				containsGenerics |= !innerType.IsNullable() && ContainsAnyGenericParameters(innerType);
			}
			return containsGenerics;
		}
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

	[HarmonyPatch(typeof(SyncMemberEditorBuilder), "BuildMemberEditors")]
	class InspectorRecursionLimiter {
		private static bool Prefix(IField field, Type type, string path, UIBuilder ui, FieldInfo fieldInfo, LayoutElement layoutElement, bool generateName = true) {
			if (!Config.GetValue(ComponentPatchesEnabled)) {
				return true;
			}
			if (CanBeRendered(type, path + ".") is false) {
				ui.Text("HostCrashGuard stopped this from rendering. This feature can be disabled.");
				ui.Style.MinHeight = 8f;//remove this and the two lines below if ui stuff is messed up.
				ui.Panel();
				ui.NestOut();
				return false;
			}
			return true;
		}

		private static bool typeChecking(Type type) {
			return type.IsPrimitive ||
				type == typeof(string) ||
				type == typeof(bool) ||
				type == typeof(Uri) ||
				type == typeof(Type) ||
				type == typeof(decimal) ||
				type == typeof(color) ||
				type == typeof(colorX) ||
				type == typeof(floatQ) ||
				type == typeof(doubleQ) ||
				type == typeof(bool2) ||
				type == typeof(bool3) ||
				type == typeof(bool4) ||
				type.IsEnum;
		}

		public static bool CanBeRendered(Type type, string path = ".") {
			if (typeChecking(type)) {
				return true;
			}
			if (type.IsNullable()) {
				FieldInfo valueField = type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
				return InspectorRecursionLimiter.CanBeRendered(valueField.FieldType, path);
			}
			//I'm not sure if this multithreading helps, but it probably does.
			int result = 1;
			Parallel.ForEach(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), (FieldInfo f) => {
				if ((path.Length - path.Replace("." + f.FieldType.FullName + ".", String.Empty).Length) > 0) {
					Interlocked.CompareExchange(ref result, 0, 1);
				}
			});
			if (result == 0) {
				return false;
			}
			foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
				if (!InspectorRecursionLimiter.CanBeRendered(f.FieldType, (path + f.FieldType.FullName + "."))) {
					return false;
				}
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
				if (disconnectInfo.Reason == DisconnectReason.Timeout) {
					Fail("The network connection has timed out.", world, __instance);
					return false;
				}
			}

			return true;
		}

		static void Fail(string reason, World world, LNL_Connection connection) {
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
					Userspace.LeaveSession(world);
					slot.Destroy();
					connection.Close();
				};
				uIBuilder.Button("Close Menu", new colorX?(RadiantUI_Constants.Sub.GREEN)).LocalPressed +=
				(IButton button, ButtonEventData eventData) => slot.Destroy();

				slot.PositionInFrontOfUser(float3.Backward, null, 0.6f);
				slot.LocalScale *= 0.001f;
			});
		}
	}
}
