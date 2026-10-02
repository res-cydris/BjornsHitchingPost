using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BjornsHitchingPost;

// Native clients already send WearNTear RPCs. Materialize only the addressed
// server-owned post when the server's normal scene does not contain it.
[HarmonyPatch(typeof(ZRoutedRpc), "HandleRoutedRPC")]
internal static class PostRequests
{
    private static readonly int Remove = "RPC_Remove".GetStableHashCode();
    private static readonly int Damage = "RPC_Damage".GetStableHashCode();
    private static readonly MethodInfo CreateObject = AccessTools.Method(typeof(ZNetScene), "CreateObject");
    private static readonly FieldInfo Instances = AccessTools.Field(typeof(ZNetScene), "m_instances");

    private static void Prefix(ZRoutedRpc.RoutedRPCData data, out GameObject __state)
    {
        __state = null;
        if (!OptionalClients.Server || !ZNetScene.instance || !Plugin.PostTemplate || !ObjectDB.instance ||
            (data.m_methodHash != Remove && data.m_methodHash != Damage)) return;
        var zdo = ZDOMan.instance.GetZDO(data.m_targetZDO);
        if (!Plugin.IsPost(zdo) || !zdo.IsOwner()) return;
        var view = ZNetScene.instance.FindInstance(zdo);
        if (!view)
        {
            // Avoid loading arbitrary distant objects in response to network input.
            // Hammer range, build-station and ward checks remain in vanilla Player.RemovePiece.
            var peer = ZNet.instance.GetPeer(data.m_senderPeerID);
            if (peer == null || !peer.IsReady() ||
                !ZNetScene.InActiveArea(zdo.GetPosition(), peer.GetRefPos())) return;
            __state = CreateObject.Invoke(ZNetScene.instance, new object[] { zdo }) as GameObject;
            if (!__state) return;
            view = __state.GetComponent<ZNetView>();
        }
        Plugin.Decorate(view);
        view.GetComponent<HitchingPost>()?.Configure();
    }

    // Unload temporary objects without destroying their saved ZDO. This prevents
    // support/weather simulation in an area where terrain and neighbors aren't loaded.
    private static void Finalizer(GameObject __state)
    {
        if (!__state) return;
        var view = __state.GetComponent<ZNetView>();
        if (view && view.IsValid() && ZNetScene.instance)
        {
            var instances = (Dictionary<ZDO, ZNetView>)Instances.GetValue(ZNetScene.instance);
            var zdo = view.GetZDO();
            if (instances.TryGetValue(zdo, out var registered) && registered == view) instances.Remove(zdo);
            view.ResetZDO();
        }
        Object.Destroy(__state);
    }
}
