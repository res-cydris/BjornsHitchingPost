using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace BjornsHitchingPost;

[BepInPlugin(Guid, "Bjørn's Hitching Post", "1.0.1")]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Patch)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "bjorns.hitchingpost";
    public const string PrefabName = "BjornsHitchingPost";
    internal const string VanillaPrefab = "wood_pole";
    internal const string PostMarker = "bhp_is_post";
    internal static GameObject PostTemplate;
    internal static float RopeHeight;
    internal static float Radius => Settings.Radius;
    internal const float Reach = 4f;
    internal static readonly int HarpoonHash = "Harpooned".GetStableHashCode();
    internal static readonly FieldInfo AttackerField = AccessTools.Field(typeof(SE_Harpooned), "m_attacker");
    private Harmony harmony;

    private void Awake()
    {
        Settings.Initialize(Config);
        harmony = new Harmony(Guid);
        harmony.PatchAll();
        PrefabManager.OnVanillaPrefabsAvailable += RegisterPiece;
        Logger.LogInfo("Bjørn's Hitching Post 1.0.1 — optional clients, server required; Valheim " + Version.GetVersionString());
    }

    private void Update() => OptionalClients.Tick();

    private void OnDestroy()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPiece;
        harmony?.UnpatchSelf();
    }

    private void RegisterPiece()
    {
        var piece = new CustomPiece(PrefabName, "wood_pole", new PieceConfig
        {
            Name = "Bjørn's Hitching Post",
            Description = "Hitch a harpooned animal or your mount. Tether distance and controls are configurable.",
            PieceTable = "Hammer",
            Category = "Furniture",
            CraftingStation = "piece_workbench",
            Icon = LoadIcon(),
            Requirements = new[] { new RequirementConfig("FineWood", 10, 0, true), new RequirementConfig("BronzeNails", 10, 0, true) }
        });
        var go = piece.PiecePrefab;
        PostTemplate = go;
        go.AddComponent<HitchingPost>();
        var wear = go.GetComponent<WearNTear>();
        wear.m_health = 1000f;
        var wood = go.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        // Measure in prefab-local space: wood_pole's origin is not its foot.
        float top = float.NegativeInfinity;
        foreach (var mesh in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.sharedMesh) continue;
            var bounds = mesh.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                top = Mathf.Max(top, go.transform.InverseTransformPoint(mesh.transform.TransformPoint(point)).y);
            }
        }
        RopeHeight = float.IsNegativeInfinity(top) ? .45f : top - .05f;
        AddBeam(go.transform, wood, new Vector3(0, RopeHeight, 0), new Vector3(1.3f, .18f, .22f));
        AddBeam(go.transform, wood, new Vector3(0, .12f, 0), new Vector3(.4f, .2f, .4f));
        // Reuse the game's wood shader; bronze-colored collars distinguish the custom post.
        var bronze = new Material(wood) { color = new Color(.55f, .29f, .10f) };
        AddBeam(go.transform, bronze, new Vector3(0, RopeHeight - .12f, 0), new Vector3(.25f, .12f, .25f));
        AddBeam(go.transform, bronze, new Vector3(0, .45f, 0), new Vector3(.25f, .12f, .25f));
        PieceManager.Instance.AddPiece(piece);
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterPiece;
        Logger.LogInfo("Registered hitching post: 10 FineWood + 10 BronzeNails.");
    }

    private static void AddBeam(Transform parent, Material material, Vector3 position, Vector3 scale)
    {
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "HitchingPostDetail";
        beam.layer = parent.gameObject.layer;
        beam.transform.SetParent(parent, false);
        beam.transform.localPosition = position;
        beam.transform.localScale = scale;
        beam.GetComponent<MeshRenderer>().sharedMaterial = material;
        // Additional decoration is client-only. Collision stays identical on vanilla clients.
        var collider = beam.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
    }

    internal static bool IsPost(ZDO zdo) => zdo != null &&
        (zdo.GetPrefab() == PrefabName.GetStableHashCode() ||
         (zdo.GetPrefab() == VanillaPrefab.GetStableHashCode() && zdo.GetBool(PostMarker)));

    internal static void Decorate(ZNetView view)
    {
        if (!view || !view.IsValid() || !IsPost(view.GetZDO())) return;
        if (!view.GetComponent<HitchingPost>()) view.gameObject.AddComponent<HitchingPost>();
    }

    [HarmonyPatch(typeof(ZNetView), "GetPrefabName")]
    private static class VanillaWirePrefab
    {
        private static void Postfix(ref string __result)
        {
            if (__result == PrefabName) __result = VanillaPrefab;
        }
    }

    [HarmonyPatch(typeof(ZNetView), "Awake")]
    private static class MarkPost
    {
        private static void Postfix(ZNetView __instance)
        {
            if (!__instance.IsValid()) return;
            if (Utils.GetPrefabName(__instance.gameObject) == PrefabName && __instance.IsOwner())
            {
                __instance.GetZDO().SetPrefab(VanillaPrefab.GetStableHashCode());
                __instance.GetZDO().Set(PostMarker, true);
            }
            OptionalClients.Track(__instance.GetZDO());
            Decorate(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    private static class RequireServer
    {
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (Utils.GetPrefabName(piece.gameObject) != PrefabName || OptionalClients.Ready) return true;
            __instance.Message(MessageHud.MessageType.Center, "Waiting for Bjørn's Hitching Post on the server.");
            __result = false;
            return false;
        }
    }

    // A modded harpoon holder must own simulation before the native damage RPC is dispatched.
    // Otherwise a vanilla owner would apply damage and keep the harpoon state only on its client.
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    private static class ClaimHarpoonTarget
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (hit.m_statusEffectHash != HarpoonHash || !Eligible(__instance) ||
                hit.GetAttacker() != Player.m_localPlayer || !Player.m_localPlayer) return true;
            if (!OptionalClients.Ready) return false;
            var view = __instance.GetComponent<ZNetView>();
            if (!view || !view.IsValid() || __instance.IsDead() || !WardAccess.Allowed(Player.m_localPlayer, __instance.transform.position)) return false;
            if (view.GetZDO().GetZDOID(AnimalTether.PostKey) != ZDOID.None) return false;
            view.ClaimOwnership();
            OptionalClients.Lease(view.GetZDO());
            return true;
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    private static class PostRefund
    {
        private static void Prefix(Piece __instance) => __instance.GetComponent<HitchingPost>()?.Configure();
    }

    private static Sprite LoadIcon()
    {
        using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("BjornsHitchingPost.icon.png"))
        {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            var texture = new Texture2D(256, 256);
            ImageConversion.LoadImage(texture, bytes);
            return Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(.5f, .5f));
        }
    }

    internal static bool Eligible(Character creature) => creature && !creature.IsPlayer() && !creature.IsBoss() && EligiblePrefab(creature.gameObject, 0);

    private static bool EligiblePrefab(GameObject prefab, int depth)
    {
        if (!prefab || depth > 3) return false;
        if (prefab.GetComponent<Tameable>()) return true;
        var growth = prefab.GetComponent<Growup>();
        if (!growth) return false;
        if (EligiblePrefab(growth.m_grownPrefab, depth + 1)) return true;
        return growth.m_altGrownPrefabs != null && growth.m_altGrownPrefabs.Count > 0 &&
               growth.m_altGrownPrefabs.All(entry => EligiblePrefab(entry.m_prefab, depth + 1));
    }

    internal static SE_Harpooned Harpoon(Character creature) => creature.GetSEMan()?.GetStatusEffect(HarpoonHash) as SE_Harpooned;
    internal static Character HarpoonOwner(SE_Harpooned effect) => effect ? AttackerField.GetValue(effect) as Character : null;

    [HarmonyPatch(typeof(Character), "Awake")]
    private static class AddTether
    {
        private static void Postfix(Character __instance)
        {
            if (Eligible(__instance) && !__instance.GetComponent<AnimalTether>())
                __instance.gameObject.AddComponent<AnimalTether>();
        }
    }

    // Only actual harpoon projectiles can target friendly eligible pets without PvP.
    [HarmonyPatch(typeof(Projectile), "IsValidTarget")]
    private static class FriendlyHarpoon
    {
        private static void Postfix(IDestructible destr, Character ___m_owner, int ___m_statusEffectHash, ref bool __result)
        {
            if (___m_owner is Player && ___m_statusEffectHash == HarpoonHash && destr is Character target &&
                Eligible(target) && !target.IsDead() && !target.IsDodgeInvincible()) __result = true;
        }
    }

    // Process the real harpoon status effect, but do not inflict damage or knockback on pets.
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    private static class HarpoonSafety
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (hit.m_statusEffectHash != HarpoonHash || !Eligible(__instance) || !(hit.GetAttacker() is Player player)) return true;
            var view = __instance.GetComponent<ZNetView>();
            if (!view || !view.IsValid() || !view.IsOwner() || __instance.IsDead() || __instance.IsTeleporting() ||
                __instance.InCutscene() || (hit.m_dodgeable && __instance.IsDodgeInvincible())) return false;
            if (!WardAccess.Allowed(player, __instance.transform.position)) return false;
            var tether = __instance.GetComponent<AnimalTether>();
            if (tether && tether.IsAttached)
            {
                player.Message(MessageHud.MessageType.Center, "Release this animal at its hitching post first.");
                return false;
            }
            var effect = __instance.GetSEMan().AddStatusEffect(HarpoonHash, true) as SE_Harpooned;
            if (effect) effect.SetAttacker(player);
            return false;
        }
    }
}

public sealed class HitchingPost : MonoBehaviour, Hoverable, Interactable
{
    private bool configured;
    private void Update() => Configure();

    internal void Configure()
    {
        if (configured || !Plugin.PostTemplate || !ObjectDB.instance) return;
        var view = GetComponent<ZNetView>();
        if (!view || !view.IsValid() || !Plugin.IsPost(view.GetZDO())) return;
        var piece = GetComponent<Piece>();
        var template = Plugin.PostTemplate.GetComponent<Piece>();
        piece.m_name = template.m_name;
        piece.m_description = template.m_description;
        piece.m_icon = template.m_icon;
        piece.m_resources = template.m_resources;
        GetComponent<WearNTear>().m_health = 1000f;
        if (Utils.GetPrefabName(gameObject) != Plugin.PrefabName)
            foreach (Transform child in Plugin.PostTemplate.transform)
                if (child.name == "HitchingPostDetail") Instantiate(child.gameObject, transform, false);
        configured = true;
    }
    public float GetHoverOffset() => Plugin.RopeHeight;
    public string GetHoverName() => "Bjørn's Hitching Post";
    public string GetHoverText() => "Bjørn's Hitching Post\n[<color=yellow><b>" + Settings.Label(Settings.HitchKey) +
        "</b></color>] Hitch harpooned animal or mount\n[<color=yellow><b>" + Settings.Label(Settings.ReleaseAllKey, true) +
        "</b></color>] Release all animals\nTether distance: " + Plugin.Radius.ToString("0.#") + " m";
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || !(user is Player player)) return false;
        if (Settings.Custom(alt ? Settings.ReleaseAllKey : Settings.HitchKey)) return false;
        return Act(player, alt);
    }

    internal bool Act(Player player, bool alt)
    {
        if (!OptionalClients.Ready) { player.Message(MessageHud.MessageType.Center, "Waiting for the server mod."); return false; }
        var view = GetComponent<ZNetView>();
        if (!view || !view.IsValid() || Vector3.Distance(player.transform.position, transform.position) > Plugin.Reach ||
            !PrivateArea.CheckAccess(transform.position)) return false;
        int requests = 0;
        var mount = AnimalTether.RiddenBy(player);
        foreach (var creature in Character.GetAllCharacters())
        {
            if (!alt && mount && creature != mount) continue;
            var tether = creature.GetComponent<AnimalTether>();
            if (!tether) continue;
            if (alt ? tether.PostId == view.GetZDO().m_uid : Vector3.Distance(creature.transform.position, transform.position) <= Plugin.Radius)
            {
                creature.GetComponent<ZNetView>().InvokeRPC("BHP_Request", player.GetZDOID(), view.GetZDO().m_uid, alt);
                requests++;
            }
        }
        player.Message(MessageHud.MessageType.Center, requests == 0 ? $"Bring a harpooned animal or mount within {Plugin.Radius:0.#} meters of the post." :
            alt ? "Releasing this post's animals." : "Checking nearby animals for your harpoon line.");
        return true;
    }
}
