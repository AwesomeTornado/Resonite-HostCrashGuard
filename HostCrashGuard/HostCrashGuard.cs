using System.Threading.Tasks;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

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
		harmony.Patch(AccessTools.Method(typeof(EngineGatherJob), "RunSessionDownload"), prefix: AccessTools.Method(typeof(PatchMethods), "Before_RunSessionDownload"));
		harmony.Patch(AccessTools.Method(typeof(EngineGatherJob), "RunSessionDownload"), postfix: AccessTools.Method(typeof(PatchMethods), "After_RunSessionDownload"));
		harmony.PatchAll();
	}

	//private async Task RunSessionDownload()
	class PatchMethods {
		static void Before_RunSessionDownload(EngineGatherJob __instance) {
			Msg("Prefix from HostCrashGuard");
		}

		static void After_RunSessionDownload(EngineGatherJob __instance) {
			Msg("Postfix from HostCrashGuard");
		}
	}
}
