# 1.0.2

- Handle vanilla hammer-removal and damage requests for server-owned posts outside the server scene.
- Load only the requested post for the native RPC, configure resource refunds, then unload any surviving temporary object.
- Keep client installation optional and preserve native client removal checks.

# 1.0.1

- Include the MIT license: Copyright (c) 2026 res-cydris.
- Include the license in the release and source packages and document it in the README.
- Update package, plugin, assembly, and multiplayer handshake to 1.0.1; gameplay unchanged.

# 1.0.0

- First stable release, following user-confirmed in-game testing.
- Hitch tameable animals with an Abyssal Harpoon or directly from a ridden mount.
- Release individual animals or all animals at a post.
- Configurable tether length, untamed-animal permission, and local shortcuts. Defaults: E to hitch; Left Shift + E to release.
- Server required; vanilla player compatible. Server and modded clients must update together to 1.0.0.
- Retain the tested gameplay, existing configuration, saved attachments, and user-supplied icon.
# 0.2.6

- Add server-synchronized tether length (1–30 meters) and untamed-animal attachment permission.
- Add local hitch, individual-release, and release-all shortcuts, with current keys shown in hover prompts.
- Preserve default game controls and existing attachments; document configuration in the README.

# 0.2.5

- Release individual animals with alternate Use while looking at their body or saddle.
- Hitch a ridden tame mount without a harpoon; dismount after attachment is confirmed.
- Add release README installation instructions and update the package description.

# 0.2.4

- Position the crossmember from the vanilla pole's mesh bounds, fixing the visible gap.
- Match the Abyssal Harpoon rope's renderer settings, slack, and distance-based thickness.
- Move the rope attachment and hover point to the corrected crossmember height.
- Tethers remain visible only to modded clients. Update the server and modded clients together.
# 0.2.3

- Include the user-edited README.
- Update package, plugin, assembly, and client/server handshake versions to 0.2.3.
- Gameplay and icon unchanged; server and modded clients must update together.

# 0.2.2

- Rename Thunderstore package identifier to BjornsHitchingPost (a new listing). Replace the old package rather than installing both.

- Update package, plugin, assembly, and matching client/server handshake to 0.2.2.
- Retain the spaced display name and user-supplied icon.
- Gameplay behavior unchanged; server and modded clients must update together.

# 0.2.1

- Bump package, plugin, assembly, and client/server handshake versions to 0.2.1.
- Retain the user-supplied icon and existing optional-client behavior.
- Server and modded clients must update together; unmodded clients remain optional.

# 0.2.0

- User-supplied hitching-post artwork for Thunderstore and the in-game build menu.

- Server required; clients can join without this mod or its dependencies.
- Posts use the vanilla wood_pole prefab with custom saved metadata.
- Modded clients restore decorations, recipe/refunds, controls, and ropes locally.
- Matching-version handshakes identify eligible animal/post simulators.
- Animals pause under server ownership when no compatible simulator is nearby.
- Harpoon holder acquires temporary ownership for native harpoon state and harmless attachment.
- Saved 0.1.0 posts convert on server load. Back up first; do not downgrade converted worlds.
- Ownership tests, patch-binding checks, package/assembly version and ZIP binary validation added.

# 0.1.0

- Initial test build requiring all clients to install the mod.
- Fine wood/bronze nails post, harpoon transfer, five-meter ropes, multiple animals, release interaction.
- Runtime eligibility, saved attachments, growth transfer, and ward checks.


