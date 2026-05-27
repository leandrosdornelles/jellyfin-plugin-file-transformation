using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FileTransformation.Helpers
{
    public static class PatchHelper
    {
        private static Harmony s_harmony = new Harmony("dev.iamparadox.jellyfin");

        internal static void SetupPatches(ILogger? logger = null)
        {
            try
            {
                MethodInfo? patchMethodInfo = typeof(StartupHelper).GetMethod(nameof(StartupHelper.Patch_Startup_Configure), BindingFlags.NonPublic | BindingFlags.Static);
                if (patchMethodInfo == null)
                {
                    logger?.LogWarning("[FileTransformation] Startup.Configure patch method was not found. File transforms are disabled.");
                    return;
                }

                HarmonyMethod configureStartupPatchMethod = new HarmonyMethod(patchMethodInfo);

                Type? startupType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(GetLoadableTypes)
                    .FirstOrDefault(x => x.FullName == "Jellyfin.Server.Startup")
                    ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetLoadableTypes).FirstOrDefault(x => x.Name == "Startup");

                MethodInfo? configureMethodInfo = startupType?.GetMethod("Configure", BindingFlags.Instance | BindingFlags.Public);
                if (configureMethodInfo == null)
                {
                    logger?.LogWarning("[FileTransformation] Jellyfin Startup.Configure was not found. File transforms are disabled.");
                    return;
                }

                s_harmony.Patch(configureMethodInfo, prefix: configureStartupPatchMethod);
                logger?.LogInformation("[FileTransformation] Startup.Configure patch applied.");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                logger?.LogError(ex, "[FileTransformation] Startup.Configure patch failed. File transforms are disabled.");
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }
    }
}
