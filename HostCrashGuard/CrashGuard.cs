using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.UIX;

using HarmonyLib;

using LiteNetLib;

using Renderite.Shared;

using ResoniteModLoader;

namespace CrashGuard;

public class CrashGuard : ResoniteMod {
	internal const string VERSION_CONSTANT = "3.1.0"; //Changing the version here updates it in all locations needed
	public override string Name => "CrashGuard";
	public override string Author => "__Choco__";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/AwesomeTornado/Resonite-CrashGuard";

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> NetworkPatchesEnabled = new ModConfigurationKey<bool>("Network Patches", "Enable all network crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> ComponentPatchesEnabled = new ModConfigurationKey<bool>("Component Patches", "Enable all component related crash fixes of this mod.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<bool> DisableExceptions = new ModConfigurationKey<bool>("Disable Exceptions", "Remove all exceptions from FrooxEngine. This is a drastic measure, but can prevent crashes.", () => true);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<float2> DialogSize = new ModConfigurationKey<float2>("Popup Size", "Changes the size of the network error popup.", () => new float2(300f, 250f));

	private static ModConfiguration Config;

	public const string exceptionWarningMessage = "CrashGuard prevented an exception. This might cause issues for users not using CrashGuard if left unsolved.";

	public override void OnEngineInit() {
		Config = GetConfiguration();
		Harmony harmony = new Harmony("com.__Choco__.CrashGuard");
		harmony.PatchAll();
		Msg("CrashGuard loaded.");
	}

	[HarmonyPatch(typeof(Engine), "RequestShutdown")]
	class preventEngineShutdownOnUnityCrash {
		private static bool Prefix(Engine __instance) {
			if (!__instance.RenderSystem.RendererProcess.HasExited) {
				//This force crash was not caused by renderer restart.
				Msg("fatal error, sorry :(");
				//TODO! Make this not prevent all crashes
				//return true;
			}
			Msg("Renderer error, recover?");
			return false;
		}

	}

	[HarmonyPatch(typeof(Engine), "ForceCrash")]
	class restartUnityPatch {
		private static bool Prefix(Engine __instance) {
			if (!__instance.RenderSystem.RendererProcess.HasExited) {
				//This force crash was not caused by renderer restart.
				return true;
			}
			Traverse traverse = Traverse.Create(__instance);
			Error("Unity crash detected, attmpting to start new unity process.");
			traverse.Field("RenderSystem").SetValue(new RenderSystem());
			//bool useRenderer, HeadOutputDevice headOutputDevice, Guid uniqueSessionId, Bitmap2D rendererIcon = null, SplashScreenDescriptor splashScreenOverride = null)
			__instance.RenderSystem.Initialize(__instance, __instance.InputInterface.HeadOutputDevice, __instance.UniqueSessionID, true, null, null, __instance.InitProgress);
			//__instance.RenderSystem.InitializeRenderSystem(true, __instance.InputInterface.HeadOutputDevice, __instance.UniqueSessionID, null, null).RunSynchronously();
			
			//traverse.Method("InitializeRenderSystem", 
			//	true,
			//	__instance.InputInterface.HeadOutputDevice,
			//	__instance.UniqueSessionID,
			//	null,
			//	null).GetValue<Task>().RunSynchronously();
			Error("Render system initialized, finishing engine initialization...");
			__instance.RenderSystem.FinishInitialize().RunSynchronously();
			Error("Render system restarted successfully. ");
			return false;
		}
	}

	[HarmonyPatch(typeof(SyncElement), "BeginModification")]
	class RemoveSyncElementExceptions {
		private static bool Prefix(SyncElement __instance, ref bool __result) {
			if (!Config.GetValue(DisableExceptions)) {
				return true;
			}
			Traverse traverse = Traverse.Create(__instance);
			///////////////////////////////////////////////////////////////////
			/* Read
			 * ModificationBlocked
			 * modificationLevel
			 */
			bool ModificationBlocked = traverse.Field("ModificationBlocked").GetValue<bool>();
			ushort modificationLevel = traverse.Field("modificationLevel").GetValue<ushort>();
			uint _flags = traverse.Field("_flags").GetValue<uint>();
			///////////////////////////////////////////////////////////////////
			if (ModificationBlocked) {
				Error(exceptionWarningMessage);
				Error("Modification of the element is currently blocked, cannot modify");
			}

			if (modificationLevel == 0) {
				if (__instance.IsDisposed) {
					string message = "Cannot modify disposed elements! Hierachy: \n" + __instance.ParentHierarchyToString();
					Error(exceptionWarningMessage);
					Error(message);
					__result = false;
				}

				__instance.World.ConnectorManager.ThreadCheck();
				if (__instance.IsDriven && __instance.ActiveLink.WasLinkGranted && (_flags & 0x620) == 0 && !__instance.ActiveLink.IsModificationAllowed) {
					SyncElement syncElement = __instance.ActiveLink as SyncElement;
					string text = null;
					if (!__instance.DriveErrorLogged) {
						text = $"The {__instance.Name} ({__instance.ReferenceID} - {__instance.GetType()}) element on {__instance.Component?.GetType()} ({__instance.Component?.ReferenceID}) is currently being driven by {__instance.ActiveLink?.Name} ({__instance.ActiveLink.ReferenceID} - {__instance.ActiveLink?.GetType()}) on {syncElement?.Component?.GetType()} ({syncElement?.Component?.ReferenceID})" + " and can be modified only through the drive reference.";
					}

					if (text != null) {
						__instance.DriveErrorLogged = true;
						Error(exceptionWarningMessage);
						Error(text);
					}

					__result = false;
				}
			}

			traverse.Field("modificationLevel").SetValue((ushort)(modificationLevel + 1));
			__result = true;
			///////////////////////////////////////////////////////////////////

			return false;
		}
	}

	[HarmonyPatch(typeof(ReferenceController), "GetObjectOrThrow")]
	class RemoveReferenceControllerExceptions {

		private static bool Prefix(ReferenceController __instance, ref IWorldElement __result, in RefID reference) {
			if (!Config.GetValue(DisableExceptions)) {
				return true;
			}
			if (reference == RefID.Null) {
				Error(exceptionWarningMessage);
				Error("Cannot request null reference ID!");
			}
			__result = Traverse.Create(__instance).Field("objects").GetValue<Dictionary<RefID, IWorldElement>>()[reference];
			return false;
		}
	}

	[HarmonyPatch(typeof(UserRoot), nameof(UserRoot.GetControllerSlot))]
	class RemoveUserRootControllerExceptions {
		private static bool Prefix(UserRoot __instance, Chirality node) {
			if (!Config.GetValue(DisableExceptions)) {
				return true;
			}
			if (node == Chirality.Left || node == Chirality.Right) {
				return true;
			}
			Error(exceptionWarningMessage);
			Error("Invalid node: " + node.ToString());
			return false;
		}
	}
	[HarmonyPatch(typeof(UserRoot), nameof(UserRoot.GetHandSlot))]
	class RemoveUserRootHandExceptions {
		private static bool Prefix(UserRoot __instance, Chirality chirality) {
			if (!Config.GetValue(DisableExceptions)) {
				return true;
			}
			if (chirality == Chirality.Left || chirality == Chirality.Right) {
				return true;
			}
			Error(exceptionWarningMessage);
			Error("Invalid chirality: " + chirality.ToString());
			return false;
		}
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
				ui.Text("CrashGuard stopped this from rendering. This feature can be disabled.");
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
				UIBuilder uIBuilder = RadiantUI_Panel.SetupPanel(slot, "Crash Guard", Config.GetValue(DialogSize), pinButton: false);
				RadiantUI_Constants.SetupEditorStyle(uIBuilder);
				uIBuilder.VerticalLayout(4f);
				uIBuilder.Style.MinHeight = 24f;
				uIBuilder.Text(reason + " CrashGuard has stopped " + world.Name + " from closing. Please save any unfinished work and close this world.");
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
