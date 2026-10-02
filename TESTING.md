# 1.0.2 mixed-client acceptance checklist

**Gameplay is untested.** Offline checks do not prove multiplayer compatibility, Unity visuals, or persistence. Use a backed-up test world.

## Setup

- Server/host: Valheim 1.0.26, BepInExPack Valheim 5.4.2351, Jötunn 2.30.2, hitching-post mod 1.0.2.
- Client A: same game/mod/dependencies.
- Client B: stock Valheim without this mod, Jötunn, or a mod loader.
- Client C for handoffs: same as A.

## Mixed-client tests

1. B joins while A is present: no missing-mod rejection, unknown prefabs, or disappearing posts.
2. A builds for 10 fine wood + 10 bronze nails. A sees decoration; B sees an ordinary wooden pole. Collision boundaries match.
3. Let B arrive first and initially own a wild boar. A harpoons it. Verify no damage, a normal harpoon line, and ownership transferring to A.
4. A transfers it at the post. A sees a post rope; B sees the animal without one. Player stamina drain ends.
5. B stays close; A leaves simulation range. The animal stays bounded and becomes stationary under server ownership. No AI/combat/active taming during fallback; no drift or extrapolation beyond the radius.
6. A returns: normal bounded motion and taming resume within a few seconds. Repeat with disconnect/reconnect.
7. C stays nearby while A leaves: C continues simulation. A's active player harpoon must not be handed off while A remains nearby.
8. Repeat far from world origin on a dedicated server; a listen-host test alone is insufficient.
9. With only B nearby, verify no ownership transfer to B. Held post management and combat against parked animals are limited until A/C returns; record observed behavior.
10. Save/restart with only B present. Links survive, no custom prefab reaches B, and A's return resumes bounded simulation.
11. Late post packets must not instantly release animals: missing-post cleanup waits 15 seconds.

## Core behavior

- Wild/tamed boar, wolf, lox, asksvin, moose, hen; include star variants.
- Piggy, wolf cub, lox calf, asksvin hatchling, moose calf, chicken: growth transfers one rope to the correct adult.
- Reject non-tameable creatures, players, bosses, unharpooned animals, another player's tether, and targets beyond five meters.
- Several animals per post; Shift+E releases only that post's animals.
- Harpooning a hitched animal does not steal it.
- Food/calmness remain required; no instant taming.
- Ward permission checks on remotely owned animals and dedicated servers.
- Destroy/dismantle with A nearby: refund fine wood/bronze nails, not ordinary wood. Animals release after grace period.
- B removes a post while A simulates it: refunds handled correctly by modded owner.
- Slopes, water, knockback, crowds, attempted riding: no escape or unstable correction. Release before riding away.
- Summons retain native dismissal/lifetime logic when simulated; no duplicated persistent summons.

## Upgrade

Use a COPY of a 0.1.0 test world with posts and attachments. Upgrade server/A to 1.0.2 before B joins. Existing posts must convert, retain links, and save/reload. Do not downgrade a converted save; restore backup to return to 0.1.0.

Record server/client versions, nearby players, species, exact steps, and relevant BepInEx log errors. Test without unrelated mods first. See VALIDATION.txt for offline results.

## 1.0.2 visual regression checks

- Inspect newly placed and existing posts after reload: the crossmember overlaps the pole with no gap.
- Compare the post tether with an active Abyssal Harpoon rope at near and far distances.
- Confirm the rope starts at the crossmember, follows the animal, and disappears on release/death.
- Check multiple animals, growth, reconnects, and rotated posts.
- Confirm vanilla players can still join; post ropes remain visible only to modded clients.

## 1.0.2 interactions

- Hitch two animals; Shift+Use one animal's body or saddle. Only that animal should be released; normal Use should retain vanilla interactions.
- Repeat with a wild animal and a growing juvenile. Test at the edge of the tether, away from the post.
- Verify ward denial at the animal and at the post; verify out-of-range requests do not release animals.
- Ride a tame saddled animal to the post and press Use: it should attach without a harpoon and dismount after confirmation. Verify unrelated animals are untouched.
- Confirm an unmounted animal without a harpoon cannot attach, and a different player's mount cannot be attached.
- Repeat on dedicated server, host, reconnect, and controller alternate-interaction bindings.

## 1.0.2 configuration

- First launch generates bjorns.hitchingpost.cfg with 5m, untamed allowed, and E / E + LeftShift shortcuts.
- Set server length to 2.5m and client local length to 20m; confirm all modded clients enforce/display 2.5m after joining.
- Disable untamed animals on server; new wild attachments fail while tame animals and ridden mounts work. Existing wild attachments remain releasable.
- Try custom G, R, and R + LeftShift shortcuts, explicit default E / E + LeftShift shortcuts and None fallback, hover labels, and controller defaults.
- Try shortcuts overlapping Use to ensure only one action occurs; ensure chat, inventory, menus, and death prevent custom actions.
- Reconnect to a different server with different settings, and test single-player settings.
- Shorten distance with existing active and parked animals; verify both stay within the new limit.

## 1.0.2 vanilla removal regression

- Dedicated modded server, vanilla player alone at a post, all modded players offline or far away: remove with hammer and verify exactly 10 FineWood + 10 BronzeNails refunded.
- Repeat with a nearby modded simulator, and after server restart.
- Without workbench or ward access, verify native hammer refusal remains.
- Damage the post from a vanilla client; verify health persists across hits and destruction refunds once.
- With several tethered animals, verify removal clears links within 15 seconds and vanilla ownership resumes.
- Repeat rapid removal requests: no duplicate refunds. Verify ordinary wood poles remain unaffected.
- Check server logs for exceptions and verify surviving temporary posts do not decay/collapse from unloaded surroundings.
