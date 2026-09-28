using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BjornsHitchingPost;

public sealed class AnimalTether : MonoBehaviour
{
    internal const string PostKey = "bhp_post";
    internal const string AnchorKey = "bhp_anchor";
    private Character creature;
    private ZNetView view;
    private Rigidbody body;
    private LineRenderer rope;
    private float missingTime;
    private float leaseTimer;
    private LineConnect ropeSettings;
    private float maxLineSlack;
    public ZDOID PostId => view && view.IsValid() ? view.GetZDO().GetZDOID(PostKey) : ZDOID.None;
    public bool IsAttached => PostId != ZDOID.None;

    private void Awake()
    {
        creature = GetComponent<Character>();
        view = GetComponent<ZNetView>();
        body = GetComponent<Rigidbody>();
        if (view && view.IsValid()) view.Register<ZDOID, ZDOID, bool>("BHP_Request", Request);
        if (view && view.IsValid()) view.Register<ZDOID>("BHP_ReleaseAnimal", ReleaseAnimal);
    }

    private void Update()
    {
        var local = Player.m_localPlayer;
        if (IsAttached && local && RiddenBy(local) == creature) local.StopDoodadControl();
        if (!view || !view.IsValid() || !view.IsOwner()) return;
        if (Time.unscaledTime < leaseTimer) return;
        leaseTimer = Time.unscaledTime + 1f;
        if (!IsAttached && Plugin.Harpoon(creature)) OptionalClients.Lease(view.GetZDO());
    }

    private void OnDestroy()
    {
        if (view && view.IsValid()) view.Unregister("BHP_Request");
        if (view && view.IsValid()) view.Unregister("BHP_ReleaseAnimal");
        if (rope) Destroy(rope.gameObject);
    }

    private void Request(long sender, ZDOID playerId, ZDOID postId, bool release)
    {
        if (!view.IsValid() || !view.IsOwner() || !Plugin.Eligible(creature) || creature.IsDead()) return;
        var playerObject = ZNetScene.instance.FindInstance(playerId);
        var postObject = ZNetScene.instance.FindInstance(postId);
        var player = playerObject ? playerObject.GetComponent<Player>() : null;
        if (!player || !postObject || !postObject.GetComponent<HitchingPost>()) return;
        // Validate identity and distance on the animal's owner, never trust the requesting peer.
        if (player.GetComponent<ZNetView>().GetZDO().GetOwner() != sender || player.IsDead() ||
            Vector3.Distance(player.transform.position, postObject.transform.position) > Plugin.Reach ||
            !WardAccess.Allowed(player, postObject.transform.position)) return;
        if (release)
        {
            if (PostId == postId) Clear();
            return;
        }
        if (IsAttached || Vector3.Distance(transform.position, postObject.transform.position) > Plugin.Radius ||
            creature.IsAttached() || !WardAccess.Allowed(player, transform.position)) return;
        if (!Settings.AllowUntamed && !creature.IsTamed()) return;
        var effect = Plugin.Harpoon(creature);
        var tameable = creature.GetComponent<Tameable>();
        bool rider = creature.IsTamed() && tameable && tameable.m_saddle && view.GetZDO().GetBool(ZDOVars.s_haveSaddleHash) &&
            view.GetZDO().GetLong(ZDOVars.s_user, 0L) == player.GetZDOID().UserID;
        if (!rider && (!effect || Plugin.HarpoonOwner(effect) != player || effect.IsDone())) return;
        view.GetZDO().Set(PostKey, postId);
        view.GetZDO().Set(AnchorKey, postObject.transform.position);
        view.GetZDO().Set(OptionalClients.LeaseKey, 0L);
        OptionalClients.Track(view.GetZDO());
        if (effect) creature.GetSEMan().RemoveStatusEffect(effect);
        missingTime = 0;
        player.Message(MessageHud.MessageType.Center, "Animal attached to Bjørn's Hitching Post.");
    }

    internal static Character RiddenBy(Player player) =>
        player.GetDoodadController() is Sadle saddle ? saddle.GetComponentInParent<Character>() : null;

    internal void RequestRelease(Player player) => view.InvokeRPC("BHP_ReleaseAnimal", player.GetZDOID());

    private void ReleaseAnimal(long sender, ZDOID playerId)
    {
        if (!view || !view.IsValid() || !view.IsOwner() || !IsAttached) return;
        var instance = ZNetScene.instance.FindInstance(playerId);
        var player = instance ? instance.GetComponent<Player>() : null;
        var post = ZDOMan.instance.GetZDO(PostId);
        if (!player || player.IsDead() || player.GetComponent<ZNetView>().GetZDO().GetOwner() != sender ||
            Vector3.Distance(player.transform.position, creature.GetCenterPoint()) > Plugin.Reach ||
            !WardAccess.Allowed(player, transform.position) ||
            !WardAccess.Allowed(player, post != null ? post.GetPosition() : view.GetZDO().GetVec3(AnchorKey, transform.position))) return;
        Clear();
    }

    [HarmonyPatch(typeof(Player), "Interact")]
    private static class IndividualRelease
    {
        private static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            var post = go ? go.GetComponentInParent<HitchingPost>() : null;
            if (post && ((Settings.Custom(Settings.HitchKey) && Settings.HitchKey.Value.IsDown()) ||
                (Settings.Custom(Settings.ReleaseAllKey) && Settings.ReleaseAllKey.Value.IsDown()))) return false;
            var tether = go ? go.GetComponentInParent<AnimalTether>() : null;
            if (tether && tether.IsAttached && Settings.Custom(Settings.ReleaseAnimalKey) && Settings.ReleaseAnimalKey.Value.IsDown())
                return false;
            if (Settings.Custom(Settings.ReleaseAnimalKey) || !alt || !tether || !tether.IsAttached) return true;
            if (!hold && OptionalClients.Ready && !__instance.InAttack() && !__instance.InDodge())
                tether.RequestRelease(__instance);
            return false;
        }
    }

    private static string ReleaseHint => "\n[<color=yellow><b>" + Settings.Label(Settings.ReleaseAnimalKey, true) +
        "</b></color>] Release from hitching post";

    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    private static class AnimalHover
    {
        private static void Postfix(Character __instance, ref string __result)
        {
            if (__instance.GetComponent<AnimalTether>()?.IsAttached == true) __result += ReleaseHint;
        }
    }

    [HarmonyPatch(typeof(Sadle), nameof(Sadle.GetHoverText))]
    private static class SaddleHover
    {
        private static void Postfix(Sadle __instance, ref string __result)
        {
            if (__instance.GetComponentInParent<AnimalTether>()?.IsAttached == true) __result += ReleaseHint;
        }
    }

    internal void Clear()
    {
        if (view && view.IsValid() && view.IsOwner()) view.GetZDO().Set(PostKey, ZDOID.None);
        if (rope) rope.enabled = false;
    }

    internal void Constrain(float dt)
    {
        if (!IsAttached || !view.IsOwner() || !body) return;
        if (creature.IsDead() || !Plugin.Eligible(creature)) { Clear(); return; }
        var post = ZDOMan.instance.GetZDO(PostId);
        if (post != null && !Plugin.IsPost(post)) { Clear(); return; }
        if (post == null)
        {
            // Network objects can arrive out of order. Retain saved anchor during the grace period.
            missingTime += dt;
            if (missingTime > 15f) { Clear(); return; }
        }
        else missingTime = 0;
        var anchor = post != null ? post.GetPosition() : view.GetZDO().GetVec3(AnchorKey, transform.position);
        var offset = body.position - anchor;
        var velocity = body.linearVelocity;
        if (!TetherMath.Constrain(Plugin.Radius, ref offset.x, ref offset.y, ref offset.z,
                ref velocity.x, ref velocity.y, ref velocity.z)) return;
        body.position = anchor + offset;
        body.linearVelocity = velocity;
    }

    private void LateUpdate()
    {
        if (!IsAttached || creature.IsDead()) { if (rope) rope.enabled = false; return; }
        var post = ZDOMan.instance.GetZDO(PostId);
        var anchor = (post != null ? post.GetPosition() : view.GetZDO().GetVec3(AnchorKey, transform.position))
            + (post != null ? post.GetRotation() : Quaternion.identity) * (Vector3.up * Plugin.RopeHeight);
        if (!rope) CreateRope();
        if (!rope) return;
        rope.enabled = true;
        var end = creature.GetCenterPoint();
        rope.transform.position = anchor;
        float distance = Vector3.Distance(anchor, end);
        // Match SE_Harpooned and LineConnect's native slack/thickness calculation.
        float slack = (1f - Utils.LerpStep(Plugin.Radius / 2f, Plugin.Radius, distance)) * maxLineSlack;
        for (int i = 0; i < rope.positionCount; i++)
        {
            float t = i / (float)(rope.positionCount - 1);
            float sag = ropeSettings.m_dynamicSlack ? distance * .5f * slack * (1f - Mathf.Pow(Mathf.Abs(.5f - t) * 2f, 2f)) : 0f;
            rope.SetPosition(i, Vector3.Lerp(anchor, end, t) + Vector3.down * sag);
        }
        if (ropeSettings.m_dynamicThickness)
            rope.widthMultiplier = Mathf.Lerp(ropeSettings.m_maxThickness, ropeSettings.m_minThickness,
                Mathf.Pow(Utils.LerpStep(ropeSettings.m_minDistance, ropeSettings.m_maxDistance, distance), ropeSettings.m_thicknessPower));
    }

    private void CreateRope()
    {
        if (ZNet.instance && ZNet.instance.IsDedicated()) return;
        if (!ObjectDB.instance) return;
        var effect = ObjectDB.instance.GetStatusEffect(Plugin.HarpoonHash) as SE_Harpooned;
        var template = effect?.m_startEffects?.m_effectPrefabs?
            .SelectMany(e => e.m_prefab ? e.m_prefab.GetComponentsInChildren<LineConnect>(true) : new LineConnect[0])
            .FirstOrDefault(line => line.GetComponent<LineRenderer>());
        if (!template) return;
        var native = template.GetComponent<LineRenderer>();
        ropeSettings = template;
        maxLineSlack = effect.m_maxLineSlack;
        // Copy the native renderer without cloning its network/status-effect components.
        // The saved post link drives endpoints locally; vanilla clients do not create this renderer.
        var child = new GameObject("BHP_HarpoonRope");
        child.transform.SetParent(transform, false);
        rope = child.AddComponent<LineRenderer>();
        rope.sharedMaterials = native.sharedMaterials;
        rope.widthCurve = native.widthCurve;
        rope.widthMultiplier = native.widthMultiplier;
        rope.colorGradient = native.colorGradient;
        rope.textureMode = native.textureMode;
        rope.textureScale = native.textureScale;
        rope.alignment = native.alignment;
        rope.numCornerVertices = native.numCornerVertices;
        rope.numCapVertices = native.numCapVertices;
        rope.generateLightingData = native.generateLightingData;
        rope.shadowCastingMode = native.shadowCastingMode;
        rope.receiveShadows = native.receiveShadows;
        rope.lightProbeUsage = native.lightProbeUsage;
        rope.reflectionProbeUsage = native.reflectionProbeUsage;
        rope.sortingLayerID = native.sortingLayerID;
        rope.sortingOrder = native.sortingOrder;
        rope.useWorldSpace = true;
        rope.positionCount = Mathf.Max(2, native.positionCount);
    }
    [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
    private static class MovementPatch
    {
        private static void Postfix(Character __instance, float dt) => __instance.GetComponent<AnimalTether>()?.Constrain(dt);
    }

    // Growup replaces the child GameObject. Capture the population before that single synchronous
    // call and transfer the saved link to its newly created adult, not to a nearby existing animal.
    [HarmonyPatch(typeof(Growup), "GrowUpdate")]
    private static class GrowthPatch
    {
        private sealed class State
        {
            internal HashSet<int> Existing;
            internal ZDOID Post;
            internal Vector3 Position, Anchor;
        }
        private static void Prefix(Growup __instance, out State __state)
        {
            __state = null;
            var tether = __instance.GetComponent<AnimalTether>();
            if (!tether || !tether.IsAttached || !tether.view.IsOwner()) return;
            __state = new State { Existing = new HashSet<int>(Character.GetAllCharacters().Select(c => c.GetInstanceID())),
                Post = tether.PostId, Position = __instance.transform.position,
                Anchor = tether.view.GetZDO().GetVec3(AnchorKey, __instance.transform.position) };
        }
        private static void Postfix(State __state)
        {
            if (__state == null) return;
            foreach (var grown in Character.GetAllCharacters())
            {
                if (__state.Existing.Contains(grown.GetInstanceID()) || !Plugin.Eligible(grown) ||
                    Vector3.Distance(grown.transform.position, __state.Position) > .1f) continue;
                var nview = grown.GetComponent<ZNetView>();
                if (!nview || !nview.IsValid() || !nview.IsOwner()) continue;
                nview.GetZDO().Set(PostKey, __state.Post);
                nview.GetZDO().Set(AnchorKey, __state.Anchor);
                OptionalClients.Track(nview.GetZDO());
            }
        }
    }
}

internal static class WardAccess
{
    private static readonly System.Reflection.FieldInfo Areas = AccessTools.Field(typeof(PrivateArea), "m_allAreas");
    private static readonly System.Reflection.MethodInfo Enabled = AccessTools.Method(typeof(PrivateArea), "IsEnabled");
    private static readonly System.Reflection.MethodInfo Inside = AccessTools.Method(typeof(PrivateArea), "IsInside");
    private static readonly System.Reflection.MethodInfo Permitted = AccessTools.Method(typeof(PrivateArea), "IsPermitted");
    internal static bool Allowed(Player player, Vector3 point)
    {
        bool blocked = false;
        foreach (var ward in (List<PrivateArea>)Areas.GetValue(null))
        {
            if (!(bool)Enabled.Invoke(ward, null) || !(bool)Inside.Invoke(ward, new object[] { point, 0f })) continue;
            if (ward.GetComponent<Piece>().GetCreator() == player.GetPlayerID() ||
                (bool)Permitted.Invoke(ward, new object[] { player.GetPlayerID() })) return true;
            blocked = true;
        }
        return !blocked;
    }
}
