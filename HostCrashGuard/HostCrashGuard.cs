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
using System.Collections.Generic;
using System.Linq;

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

		/*private static bool CanBeRendered(IField field, Type type, string path, FieldInfo fieldInfo) {
			if (!Config.GetValue(ComponentPatchesEnabled)) {//TODO: clean up config usage here.
				return true;
			}
			if (type.IsPrimitive || type == typeof(string) || type == typeof(Uri) || type == typeof(Type) || type == typeof(decimal)) {
				if (generateName) {
					InspectorRecursionLimiter.GenerateMemberName(path, ui);
				}
				RangeAttribute range = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<RangeAttribute>() : null);
				if (type == typeof(bool)) {
					ui.BooleanMemberEditor(field, path);
					return false;
				}
				if (range != null) {
					string textFormat = range.TextFormat;
					if (type != typeof(float) && type != typeof(double) && type != typeof(decimal) && type != typeof(half)) {
						textFormat = "0";
					}
					ui.SliderMemberEditor(range.Min, range.Max, field, path, textFormat);
					return false;
				}
				string text;
				if (fieldInfo == null) {
					text = null;
				} else {
					FormatAttribute customAttribute = fieldInfo.GetCustomAttribute<FormatAttribute>();
					text = ((customAttribute != null) ? customAttribute.FormatString : null);
				}
				string format = text;
				bool noContinousParsing = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<NoContinuousParsingAttribute>() : null) != null;
				if (type == typeof(Uri)) {
					noContinousParsing = true;
				}
				ui.Style.FlexibleWidth = 10f;
				ui.PrimitiveMemberEditor(field, path, !noContinousParsing, format);
				ui.Style.FlexibleWidth = -1f;
				return false;
			} else {
				if (type == typeof(color)) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.ColorMemberEditor(field, path, true, false, true);
					return false;
				}
				if (type == typeof(colorX)) {
					if (layoutElement != null) {
						layoutElement.MinHeight.Value = 48f;
					}
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.ColorXMemberEditor(field, path, true, false, true);
					return false;
				}
				if (type == typeof(floatQ) || type == typeof(doubleQ)) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.QuaternionMemberEditor(field, path, false);
					return false;
				}
				if (type == typeof(bool2) || type == typeof(bool3) || type == typeof(bool4)) {
					int dimensions = type.GetVectorDimensions();
					for (int i = 0; i < dimensions; i++) {
						string element = "xyzw"[i].ToString();
						InspectorRecursionLimiter.GenerateMemberName(element, ui);
						ui.BooleanMemberEditor(field, element);
					}
					return false;
				}
				if (type.IsEnum) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.EnumMemberEditor(field, path);
					return false;
				}
				if (type.IsNullable()) {
					ui.PushStyle();
					ui.Style.MinWidth = 24f;
					ui.Style.MinHeight = 24f;
					NullableMemberEditor nullableMemberEditor = ui.NullableMemberEditor(field, path);
					ui.PopStyle();
					nullableMemberEditor.NullableBaseType.Value = Nullable.GetUnderlyingType(type);
					FieldInfo valueField = type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
					if (valueField.FieldType != type) {
						InspectorRecursionLimiter.Prefix(field, valueField.FieldType, path + ".value", ui, valueField, layoutElement, true);
					}
					return false;
				}
				foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (f.FieldType == type || path.Contains(f.Name)) {
						Msg("Stopped recursion chain");
						return false;
					}
				}
				foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					string subpath;
					if (string.IsNullOrEmpty(path)) {
						subpath = f.Name;
					} else {
						subpath = path + "." + f.Name;
					}
					InspectorRecursionLimiter.Prefix(field, f.FieldType, subpath, ui, f, layoutElement, true);
				}
				return false;
			}
		}*/

		private static bool Prefix(IField field, Type type, string path, UIBuilder ui, FieldInfo fieldInfo, LayoutElement layoutElement, bool generateName = true) {
			if (!Config.GetValue(ComponentPatchesEnabled)) {
				return true;
			}
			if (type.IsPrimitive || type == typeof(string) || type == typeof(Uri) || type == typeof(Type) || type == typeof(decimal)) {
				if (generateName) {
					InspectorRecursionLimiter.GenerateMemberName(path, ui);
				}
				RangeAttribute range = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<RangeAttribute>() : null);
				if (type == typeof(bool)) {
					ui.BooleanMemberEditor(field, path);
					return false;
				}
				if (range != null) {
					string textFormat = range.TextFormat;
					if (type != typeof(float) && type != typeof(double) && type != typeof(decimal) && type != typeof(half)) {
						textFormat = "0";
					}
					ui.SliderMemberEditor(range.Min, range.Max, field, path, textFormat);
					return false;
				}
				string text;
				if (fieldInfo == null) {
					text = null;
				} else {
					FormatAttribute customAttribute = fieldInfo.GetCustomAttribute<FormatAttribute>();
					text = ((customAttribute != null) ? customAttribute.FormatString : null);
				}
				string format = text;
				bool noContinousParsing = ((fieldInfo != null) ? fieldInfo.GetCustomAttribute<NoContinuousParsingAttribute>() : null) != null;
				if (type == typeof(Uri)) {
					noContinousParsing = true;
				}
				ui.Style.FlexibleWidth = 10f;
				ui.PrimitiveMemberEditor(field, path, !noContinousParsing, format);
				ui.Style.FlexibleWidth = -1f;
				return false;
			} else {
				if (type == typeof(color)) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.ColorMemberEditor(field, path, true, false, true);
					return false;
				}
				if (type == typeof(colorX)) {
					if (layoutElement != null) {
						layoutElement.MinHeight.Value = 48f;
					}
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.ColorXMemberEditor(field, path, true, false, true);
					return false;
				}
				if (type == typeof(floatQ) || type == typeof(doubleQ)) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.QuaternionMemberEditor(field, path, false);
					return false;
				}
				if (type == typeof(bool2) || type == typeof(bool3) || type == typeof(bool4)) {
					int dimensions = type.GetVectorDimensions();
					for (int i = 0; i < dimensions; i++) {
						string element = "xyzw"[i].ToString();
						InspectorRecursionLimiter.GenerateMemberName(element, ui);
						ui.BooleanMemberEditor(field, element);
					}
					return false;
				}
				if (type.IsEnum) {
					if (generateName) {
						InspectorRecursionLimiter.GenerateMemberName(path, ui);
					}
					ui.EnumMemberEditor(field, path);
					return false;
				}
				if (type.IsNullable()) {
					ui.PushStyle();
					ui.Style.MinWidth = 24f;
					ui.Style.MinHeight = 24f;
					NullableMemberEditor nullableMemberEditor = ui.NullableMemberEditor(field, path);
					ui.PopStyle();
					nullableMemberEditor.NullableBaseType.Value = Nullable.GetUnderlyingType(type);
					FieldInfo valueField = type.GetField("value", BindingFlags.Instance | BindingFlags.NonPublic);
					if (valueField.FieldType != type) {
						InspectorRecursionLimiter.Prefix(field, valueField.FieldType, path + ".value", ui, valueField, layoutElement, true);
					}
					return false;
				}
				foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					if (f.FieldType == type || path.Contains(f.Name)) {
						Msg("Stopped recursion chain");
						return false;
					}
				}
				foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
					string subpath;
					if (string.IsNullOrEmpty(path)) {
						subpath = f.Name;
					} else {
						subpath = path + "." + f.Name;
					}
					InspectorRecursionLimiter.Prefix(field, f.FieldType, subpath, ui, f, layoutElement, true);
				}
				return false;
			}
		}

		private static void GenerateMemberName(string path, UIBuilder ui) {
			if (!string.IsNullOrEmpty(path)) {
				int dotIndex = path.LastIndexOf(".");
				string name;
				if (dotIndex < 0) {
					name = path;
				} else {
					name = path.Substring(dotIndex + 1);
				}
				LocaleString localeString = name;
				ui.Text(in localeString, true, new Alignment?(Alignment.MiddleLeft), false, null);
			}
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
