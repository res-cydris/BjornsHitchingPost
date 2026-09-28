using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BjornsHitchingPost;

internal static class Settings
{
    internal static ConfigEntry<float> Length;
    internal static ConfigEntry<bool> Untamed;
    internal static ConfigEntry<KeyboardShortcut> HitchKey, ReleaseAnimalKey, ReleaseAllKey;
    private static float serverLength = 5f;
    private static bool serverUntamed = true;
    internal static float Radius => OptionalClients.Server ? SafeLength(Length.Value) : serverLength;
    internal static bool AllowUntamed => OptionalClients.Server ? Untamed.Value : serverUntamed;

    internal static void Initialize(ConfigFile config)
    {
        Length = config.Bind("General", "TetherLength", 5f, new ConfigDescription(
            "Maximum tether distance in meters. Server/host setting, synchronized to modded clients.",
            new AcceptableValueRange<float>(1f, 30f)));
        Untamed = config.Bind("General", "AllowUntamedAnimals", true,
            "Allow new attachments of untamed animals. Server/host setting. Existing attachments remain when disabled.");
        HitchKey = config.Bind("Controls", "HitchKey", new KeyboardShortcut(KeyCode.E),
            "Local shortcut for hitching at a post. None follows the game's Use binding.");
        ReleaseAnimalKey = config.Bind("Controls", "ReleaseAnimalKey", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
            "Local shortcut for releasing the animal you look at. None follows alternate Use (normally Shift+E).");
        ReleaseAllKey = config.Bind("Controls", "ReleaseAllKey", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift),
            "Local shortcut for releasing all animals at the post you look at. None follows alternate Use (normally Shift+E).");
    }

    internal static void Receive(float length, bool untamed)
    {
        serverLength = SafeLength(length);
        serverUntamed = untamed;
    }

    private static float SafeLength(float length) => float.IsNaN(length) || float.IsInfinity(length) ? 5f : Mathf.Clamp(length, 1f, 30f);

    internal static bool Custom(ConfigEntry<KeyboardShortcut> key) => key.Value.MainKey != KeyCode.None;
    internal static string Label(ConfigEntry<KeyboardShortcut> key, bool alternate = false) => Custom(key)
        ? key.Value.ToString()
        : Localization.instance.Localize(alternate
            ? (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? "$KEY_AltKeys + $KEY_Use" : "$KEY_AltPlace + $KEY_Use")
            : "$KEY_Use");

    private static readonly System.Reflection.MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

    [HarmonyPatch(typeof(Player), "Update")]
    private static class CustomControls
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !OptionalClients.Ready ||
                !(bool)TakeInput.Invoke(__instance, null) || Hud.InRadial() ||
                __instance.InAttack() || __instance.InDodge() || __instance.IsDead()) return;
            var target = __instance.GetHoverObject();
            if (!target) return;
            var post = target.GetComponentInParent<HitchingPost>();
            if (post)
            {
                if (Custom(ReleaseAllKey) && ReleaseAllKey.Value.IsDown()) post.Act(__instance, true);
                else if (Custom(HitchKey) && HitchKey.Value.IsDown()) post.Act(__instance, false);
                return;
            }
            var tether = target.GetComponentInParent<AnimalTether>();
            if (tether && tether.IsAttached && Custom(ReleaseAnimalKey) && ReleaseAnimalKey.Value.IsDown())
                tether.RequestRelease(__instance);
        }
    }
}
