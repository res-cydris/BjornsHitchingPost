using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BjornsHitchingPost;

// Mixed-client worlds must never depend on an unmodded peer to simulate a tether.
// The server assigns tagged objects only to a compatible nearby simulator, or parks
// them under server ownership until one returns. No custom prefab goes over the wire.
internal static class OptionalClients
{
    internal const string DriverKey = "bhp_driver";
    internal const string LeaseKey = "bhp_lease_until";
    private const string Protocol = "1.0.2";
    private static readonly Dictionary<long, float> Clients = new Dictionary<long, float>();
    private static readonly HashSet<ZDOID> Tracked = new HashSet<ZDOID>();
    private static readonly FieldInfo Objects = AccessTools.Field(typeof(ZDOMan), "m_objectsByID");
    private static float nextTick, nextHello, acknowledged;
    private static bool initialScan;
    private static readonly Dictionary<ZDOID, Vector3> Parked = new Dictionary<ZDOID, Vector3>();
    private static readonly Dictionary<ZDOID, float> MissingPosts = new Dictionary<ZDOID, float>();
    internal static bool Server => ZNet.instance && ZNet.instance.IsServer();
    internal static bool Ready => ZNet.instance && (Server || Time.unscaledTime - acknowledged < 15f);

    private static bool Leased(ZDO zdo) => ZNet.instance && zdo.GetLong(LeaseKey) > ZNet.instance.GetTime().Ticks;
    private static bool Protected(ZDO zdo) => Plugin.IsPost(zdo) || zdo.GetZDOID(AnimalTether.PostKey) != ZDOID.None || Leased(zdo);

    internal static void Lease(ZDO zdo)
    {
        if (!zdo.IsOwner()) return;
        zdo.Set(DriverKey, ZDOMan.GetSessionID());
        zdo.Set(LeaseKey, ZNet.instance.GetTime().AddSeconds(8).Ticks);
        Track(zdo);
    }

    internal static void Track(ZDO zdo)
    {
        if (zdo == null || !zdo.IsValid()) return;
        if (zdo.GetPrefab() == Plugin.PrefabName.GetStableHashCode())
        {
            // Upgrade 0.1.0 saves before they can be sent to vanilla peers.
            if (Server)
            {
                zdo.SetPrefab(Plugin.VanillaPrefab.GetStableHashCode());
                zdo.Set(Plugin.PostMarker, true);
            }
        }
        if (Protected(zdo)) Tracked.Add(zdo.m_uid);
    }

    private static bool Capable(long uid, Vector3 point)
    {
        if (uid == ZDOMan.GetSessionID())
            return ZNetScene.InActiveArea(point, ZNet.instance.GetReferencePosition());
        if (!Clients.TryGetValue(uid, out float seen) || Time.unscaledTime - seen > 15f) return false;
        var peer = ZNet.instance.GetPeer(uid);
        return peer != null && peer.IsReady() && ZNetScene.InActiveArea(point, peer.GetRefPos());
    }

    private static long SelectOwner(ZDO zdo)
    {
        var position = zdo.GetPosition();
        var peers = new List<long>();
        foreach (long uid in Clients.Keys)
            if (Capable(uid, position)) peers.Add(uid);
        bool hostActive = Capable(ZDOMan.GetSessionID(), position);
        // A live harpoon effect is local to its simulator; do not move it gratuitously.
        long preferred = Leased(zdo) ? zdo.GetLong(DriverKey) : zdo.GetOwner();
        return OwnershipPolicy.Select(ZDOMan.GetSessionID(), preferred, hostActive, peers);
    }

    internal static void Tick()
    {
        if (!ZNet.instance || ZDOMan.instance == null) return;
        if (!Server && Player.m_localPlayer && Time.unscaledTime >= nextHello)
        {
            nextHello = Time.unscaledTime + 3f;
            var peer = ZNet.instance.GetServerPeer();
            if (peer != null && peer.IsReady()) peer.m_rpc.Invoke("BHP_Hello", Protocol);
        }
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + .5f;
        if (!initialScan && ZNetScene.instance && PlayerReadyForScan())
        {
            initialScan = true;
            Scan();
        }
        foreach (var id in Tracked.ToArray())
        {
            var zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || !Protected(zdo)) { Tracked.Remove(id); Parked.Remove(id); MissingPosts.Remove(id); continue; }
            if (Server) Enforce(zdo);
            if (ZNetScene.instance)
            {
                var instance = ZNetScene.instance.FindInstance(id);
                if (instance) Plugin.Decorate(instance.GetComponent<ZNetView>());
            }
        }
    }

    private static bool PlayerReadyForScan() => Server || Player.m_localPlayer;

    private static void Scan()
    {
        if (ZDOMan.instance == null) return;
        foreach (var zdo in ((Dictionary<ZDOID, ZDO>)Objects.GetValue(ZDOMan.instance)).Values) Track(zdo);
    }

    private static void Enforce(ZDO zdo)
    {
        long owner = SelectOwner(zdo);
        if (zdo.GetOwner() != owner) zdo.SetOwner(owner);
        var postId = zdo.GetZDOID(AnimalTether.PostKey);
        if (postId == ZDOID.None) return;
        // New object packets can be split across frames, so tolerate a late post packet.
        var post = ZDOMan.instance.GetZDO(postId);
        if (!Plugin.IsPost(post))
        {
            if (!MissingPosts.TryGetValue(zdo.m_uid, out float since)) MissingPosts[zdo.m_uid] = Time.unscaledTime;
            else if (Time.unscaledTime - since > 15f)
            {
                zdo.Set(AnimalTether.PostKey, ZDOID.None);
                zdo.Set(LeaseKey, 0L);
                Parked.Remove(zdo.m_uid);
                MissingPosts.Remove(zdo.m_uid);
            }
            return;
        }
        MissingPosts.Remove(zdo.m_uid);
        if (owner != ZDOMan.GetSessionID() || Capable(owner, zdo.GetPosition()))
        {
            Parked.Remove(zdo.m_uid);
            return;
        }
        if (!Parked.TryGetValue(zdo.m_uid, out var position))
        {
            position = zdo.GetPosition();
        }
        // Recheck parked animals too when the configured distance is shortened.
        {
            var offset = position - post.GetPosition();
            float vx = 0, vy = 0, vz = 0;
            TetherMath.Constrain(Plugin.Radius, ref offset.x, ref offset.y, ref offset.z, ref vx, ref vy, ref vz);
            position = post.GetPosition() + offset;
            Parked[zdo.m_uid] = position;
        }
        zdo.SetPosition(position);
        // A stale ship/platform parent would otherwise move the supposedly parked animal
        // on stock clients even with zero world-space velocity.
        zdo.UpdateConnection(ZDOExtraData.ConnectionType.SyncTransform, ZDOID.None);
        zdo.Set(ZDOVars.s_attachJointHash, "");
        zdo.Set(ZDOVars.s_velRelHash, Vector3.zero);
        zdo.Set(ZDOVars.s_velHash, Vector3.zero);
        zdo.Set(ZDOVars.s_bodyVelHash, Vector3.zero);
        zdo.Set(ZDOVars.s_bodyAVelHash, Vector3.zero);
    }

    [HarmonyPatch(typeof(ZNet), "Awake")]
    private static class Reset
    {
        private static void Prefix()
        {
            Clients.Clear(); Tracked.Clear(); Parked.Clear(); MissingPosts.Clear(); initialScan = false;
            nextTick = nextHello = 0; acknowledged = float.NegativeInfinity;
        }
    }

    [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
    private static class Handshake
    {
        private static void Postfix(ZNetPeer peer)
        {
            peer.m_rpc.Register<string>("BHP_Hello", (rpc, version) =>
            {
                if (!Server || !peer.IsReady() || version != Protocol) return;
                Clients[peer.m_uid] = Time.unscaledTime;
                rpc.Invoke("BHP_Ready", Protocol, Settings.Length.Value, Settings.Untamed.Value);
            });
            peer.m_rpc.Register<string, float, bool>("BHP_Ready", (rpc, version, length, untamed) =>
            {
                if (!Server && peer == ZNet.instance.GetServerPeer() && version == Protocol)
                {
                    Settings.Receive(length, untamed);
                    acknowledged = Time.unscaledTime;
                }
            });
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    private static class Disconnect
    {
        private static void Prefix(ZNetPeer peer) => Clients.Remove(peer.m_uid);
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.Deserialize))]
    private static class Received
    {
        private static void Postfix(ZDO __instance) => Track(__instance);
    }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.Load))]
    private static class LoadedLegacy { private static void Postfix() => Scan(); }

    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.LoadChunks))]
    private static class LoadedChunks { private static void Postfix() => Scan(); }

    [HarmonyPatch(typeof(ZDOMan), "RPC_ZDOData")]
    private static class ReceivedBatch
    {
        private static void Postfix()
        {
            if (!Server) return;
            foreach (var id in Tracked.ToArray())
            {
                var zdo = ZDOMan.instance.GetZDO(id);
                if (zdo != null && Protected(zdo)) Enforce(zdo);
            }
        }
    }

    [HarmonyPatch(typeof(ZDO), nameof(ZDO.SetOwner))]
    private static class ProtectOwner
    {
        private static void Prefix(ZDO __instance, ref long uid)
        {
            if (Server && Protected(__instance)) uid = SelectOwner(__instance);
        }
    }
}
