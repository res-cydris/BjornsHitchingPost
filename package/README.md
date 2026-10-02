# Bjørn's Hitching Post

Hitch animals to ease taming or prevent them from roaming off. Harpoon and mounted hitching, individual release, and configurable tethers. Server required; vanilla player compatible.

## Using the post

Build **Bjørn's Hitching Post** from the hammer's **Furniture** menu near a workbench. Each post costs **10 Fine wood and 10 Bronze nails**.

### Hitch a harpooned animal

1. Hit an eligible animal with your **Abyssal Harpoon**. With the mod installed, eligible animals take no damage or knockback from that hit, and friendly pets can be harpooned without enabling PvP.
2. Bring the animal within the configured tether distance (**five meters by default**) of the post, keeping your harpoon line attached.
3. Look at the post and press **Use (E by default)**. Your harpoon line transfers to the post.

### Hitch your mount

Ride a saddled, tame animal up to the post. While still riding, look at the post and press **Use**. Your mount is hitched without a harpoon, and you dismount when the attachment is confirmed. While mounted, this action selects your mount rather than nearby harpooned animals.

### Release animals

- **One animal:** approach it, look at its body or saddle, and press **Shift + Use**. You do not need to stand beside the post. While tethered, this action takes precedence over renaming or saddle removal.
- **All animals at a post:** look at the post and press **Shift + Use**.

The default shortcuts are E to hitch and Left Shift + E to release. Change them in the configuration below, or set them to `None` to follow the game interaction bindings. Ward permissions apply at the post and, for individual release, at the animal. Release an animal before riding or transporting it away.

Multiple animals can share a post. Each can move within the configured tether distance (five meters by default). Food, calmness, and the normal taming duration are still required. Animals can become alarmed, attack, and damage the post, which has 1,000 health. Post ropes consume no player stamina. Player-held harpoons retain their normal stamina and release rules.

On entering an area, allow a moment for the server to transfer simulation before interacting. The harmless-harpoon behavior applies to modded players; unmodded players' harpoon hits retain normal behavior.

## Eligible creatures

Eligibility uses the game's `Tameable` component or a young creature's `Growup` path to a tameable adult. Players and bosses are excluded.

- Boars and piggies
- Wolves and wolf cubs
- Lox and lox calves
- Asksvin and asksvin hatchlings
- Moose and moose calves
- Hens and chickens

Tameable summoned companions also qualify: `Skeleton_Friendly` and spiritcaller variants of boar, wolf, moose, and bjorn. Their native lifetime/dismissal logic is not rewritten, but simulation pauses under the stationary fallback. Ordinary wild bjorn, deer, necks, trolls, eggs, and other non-tameable creatures cannot be hitched. Star levels do not restrict eligibility. Modded creatures must expose the same components; custom species themselves may require additional client mods.

## What unmodded players experience

This mod is **vanilla player compatible**. Unmodded players see the animals and an ordinary vanilla **wooden pole**. They do not see the custom decorations, name, build recipe, controls, or post ropes. Saved/networked posts use the existing `wood_pole` prefab, so no unknown assets are needed on their machines. Decorations add no extra collision geometry.

**When no compatible modded simulator is nearby, tethered animals are held stationary under server ownership.** They remain confined, but AI, combat, and active taming simulation pause. Roaming within the tether and taming resume when a modded player returns, or when the modded host is actively simulating that area. Vanilla timers based on world time may catch up on resuming; this does not add off-screen taming. Vanilla clients cannot enforce this mod's restraint rules.

Vanilla players can remove posts with the hammer and damage them even when no modded player is nearby. The modded server handles the native requests and preserves the fine wood and bronze nail refunds. Normal vanilla hammer range, workbench, and ward checks still apply. Animals release after the existing missing-post grace period (up to 15 seconds). Unmodded players still cannot use hitch/release controls or see ropes, and parked animals remain paused.

## Persistence and permissions

Attachments are saved with the animal and transferred when it grows. Outside wards, nearby modded players can release tethered animals. Links to destroyed posts clear after a 15-second grace period. Animals pause when no compatible modded simulator is nearby, as described above.

Unofficial community mod; not an Iron Gate product. Game and dependency DLLs are not included.

## Configuration

After the first launch with the mod installed, edit **`BepInEx/config/bjorns.hitchingpost.cfg`**. For a mod-manager profile, open that profile's configuration folder. Stop the game or server before editing, then restart it to load the changes.

### Gameplay settings — server or world host

The server/host controls these values and sends them to modded clients. Client-side gameplay values do not override the server. In single-player, your local settings apply.

| Setting | Default | Effect |
| --- | --- | --- |
| `TetherLength` | `5` | Tether distance in meters, from `1` to `30`. Supports decimals. Applies to all posts and existing attachments, including parked animals. |
| `AllowUntamedAnimals` | `true` | Set to `false` to allow only already-tamed animals to be newly hitched. Existing attachments remain, and can still be released. |

Only tameable creature types are eligible regardless of this setting. Shortening the tether brings animals outside the new limit back inside it. Increasing tether length does not increase the distance from which you can interact with a post or release an animal.

### Keybinds — each player's client

| Setting | Default | Action |
| --- | --- | --- |
| `HitchKey` | `E` | Hitch your harpooned animal or ridden mount while looking at a post. |
| `ReleaseAnimalKey` | `E + LeftShift` | Release the tethered animal you are looking at. |
| `ReleaseAllKey` | `E + LeftShift` | Release all animals from the post you are looking at. |

Existing config values are preserved when upgrading; edit the three entries to adopt these defaults if a config already exists.

`None` means **follow the game's controls**, not disable the action: Use (normally E) for hitching, and alternate Use (normally Shift + E) for releasing. This also preserves the game's controller bindings. A custom shortcut replaces that action's default mod control, and its label appears in the hover prompt. Use Unity key names such as `G`, `R`, or `LeftControl`; a combination is written as `G + LeftControl`. Choose shortcuts that do not conflict with other game controls. If hitch and release-all use the same custom shortcut, release-all takes priority.

Example configuration:

```ini
[General]
TetherLength = 8
AllowUntamedAnimals = false

[Controls]
HitchKey = G
ReleaseAnimalKey = R
ReleaseAllKey = R + LeftShift
```

This example allows only new attachments of already-tamed animals, gives every post an eight-meter tether, and uses G to hitch, R to release one animal, and Shift + R to release all at a post. Keybind changes are local and do not affect other players.

## License

Licensed under the [MIT License](LICENSE). Copyright (c) 2026 res-cydris. Game assets and third-party dependencies remain under their respective licenses.

## Installation

Install the mod on the **dedicated server or world host**. Players who build posts, hitch or release animals, or want to see ropes must also install it. Other players can use vanilla Valheim.

The server and modded players must use **Bjørn's Hitching Post 1.0.2**, with **BepInExPack Valheim 5.4.2351** and **Jötunn 2.30.2**. Back up your world before upgrading. Replace older copies; do not install the old underscored package alongside this one.

### Thunderstore / r2modman — recommended

- Install **BjornsHitchingPost** through Thunderstore Mod Manager or r2modman. Allow it to install the listed dependencies, then launch Valheim through that profile.
- Alternatively, download the package ZIP from Thunderstore and follow the manual steps below.
- For a hosted dedicated server, select this package in the host's mod manager, confirm both dependencies are installed, and restart the server.

### GitHub Releases

- Open this mod's GitHub repository and go to **Releases**.
- Download the packaged **Bjørn's Hitching Post-1.0.2.zip** or **BjornsHitchingPost-1.0.2.zip** release asset, then follow the manual installation steps below.
- Choose the compiled mod package, not the `-source.zip` archive or GitHub's automatically generated source-code downloads.
- Install BepInExPack Valheim and Jötunn separately; the release ZIP does not bundle those dependencies.

### Nexus Mods

- Open this mod's Nexus Mods page and download the **1.0.2 main file** using the manual download option.
- Extract the downloaded package and follow the manual installation steps below.
- Install BepInExPack Valheim and Jötunn separately. Downloading this mod from Nexus does not install its dependencies automatically.
- Use only one installed copy of the plugin, even if you download it from multiple sites.

### Manual installation

1. Close Valheim or stop the dedicated server.
2. Install **BepInExPack Valheim** into that game's or server's root folder, following the dependency's instructions. Use the contents of its `BepInExPack_Valheim` directory, not the outer wrapper folder.
3. Install **Jötunn** into the same BepInEx installation following its package instructions.
4. Extract the Bjørn's Hitching Post ZIP. Copy its `plugins/BjornsHitchingPost` folder into `BepInEx/plugins`.
5. Check that the plugin is located at:

   ```text
   <Valheim or server folder>/BepInEx/plugins/BjornsHitchingPost/BjornsHitchingPost.dll
   ```

6. When upgrading, replace the old DLL and remove duplicate copies from other plugin folders. The README, manifest, and icon do not need to be copied into the game folder.
7. Start the game or server using its BepInEx-enabled launch setup. Check `BepInEx/LogOutput.log` for **Bjørn's Hitching Post 1.0.2** and any loading errors.

Repeat these steps for the server/world host and each player who needs the mod's controls or rope visuals. Keep their mod versions matched.