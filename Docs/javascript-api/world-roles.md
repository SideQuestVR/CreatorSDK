# World Roles

Who a player is in your world (its owner, a moderator, SideQuest staff or a visitor), in a form your own server can trust.

`user.isSpaceAdmin` and anything else your page reads about a player come from the player's own app, and a modified app can change them. They're fine for deciding what to show. To unlock something that matters on your own server, such as an owner's settings, a licence or a private catalogue, ask for a **role token**: a short-lived token signed by SideQuest that says what the player is in your world. Your server checks the signature, so a modified app can't fake it.

!> Don't treat "this player can save [world files](saving-data.md#world-files)" as proof that they own the world. SideQuest staff can save too.

## Roles

| `role` | Who |
|---|---|
| `owner` | The world's owner |
| `moderator` | An Owner or Moderator of a group the world is attached to, unless they're banned from the world |
| `staff` | SideQuest staff who have none of the roles above |
| `visitor` | Any other signed-in player |

Each player gets the first role in the table that fits. `staff` is a separate flag, true for SideQuest staff whatever their role, so staff are never reported as the owner: a staff account that doesn't own your world reads `role: "staff"`. When staff own the world, it reads `role: "owner"` with `staff: true`.

The role is worked out by SideQuest when the token is made, so a new token always reflects the latest change, such as a new group moderator.

## Getting a token

```js
const scene = BS.Scene.GetInstance();

// 1. Your server gives the page a fresh nonce (any random string; see below)
const nonce = await (await fetch("https://my-server.example/nonce")).text();

// 2. The app asks SideQuest for a token for this world
const { token, role, staff } = await scene.GetRoleToken(nonce);

// 3. The page sends the token to your server, which verifies it
await fetch("https://my-server.example/unlock", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token })
});
```

`GetRoleToken(nonce)` resolves with:

| Field | What it is |
|---|---|
| `token` | The signed token. Send this to your server |
| `role` | `"owner"`, `"moderator"`, `"staff"` or `"visitor"`, for the page to show. It proves nothing until your server verifies the token |
| `staff` | Whether the player is SideQuest staff, for display |
| `worldId` | Your world's id |
| `expiresAt` | When the token expires (ISO 8601), 5 minutes after it was made |

The token is always for the world the player is in; a page can't ask for another world's. The nonce is optional but recommended: up to 128 printable ASCII characters with no spaces. The token carries it back, so your server can be sure a token is new and used only once.

`GetRoleToken` rejects with an `Error` saying why when:

- the player isn't signed in to SideQuest (guests can't get one),
- the space isn't a SideQuest world,
- the player can't access the world (it's private to them, or they're banned),
- the space is still loading (ask again once it has loaded), or
- it runs in SDK Play Mode, where it reports "role tokens are not available in this build". Publish the world to try it.

## Verifying on your server

Your server needs your world's id. `BS.PersistFileBridge.info(scene)` gives it as `worldId`, or read it once from `GetRoleToken`'s reply and keep it in your server's settings.

Check every token for all of these:

1. It's signed by SideQuest.
2. `aud` is `sq-world:<your world id>`. A token for another world must be refused.
3. It hasn't expired (`exp`).
4. If you sent a nonce, `nonce` is one you issued and haven't seen before.

Then use `role` (and `staff`, if you treat staff specially) and `sub`, the player's SideQuest account id.

### The simple way: ask altvr.app

```js
// Node 18+ or any server with fetch
async function verifyRoleToken(token) {
    const res = await fetch("https://altvr.app/sq/v2/worlds/role-token/verify", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token })
    });
    if (!res.ok) return null;                        // 400: forged, expired or not a role token
    const claims = await res.json();
    if (claims.aud !== "sq-world:" + MY_WORLD_ID) return null;
    if (!useNonceOnce(claims.nonce)) return null;    // your own single-use check
    return claims;                                   // claims.role, claims.staff, claims.sub
}
```

`/verify` checks the signature, issuer and expiry. Checking `aud` and the nonce is your job, because only your server knows which world and which nonces to expect.

### Without a network call: check the signature yourself

The signing keys are published at `https://altvr.app/sq/v2/worlds/role-token/jwks`, so any JWT library can verify tokens offline. With [`jose`](https://github.com/panva/jose) in Node:

```js
import { createRemoteJWKSet, jwtVerify } from "jose";

const sideQuestKeys = createRemoteJWKSet(new URL("https://altvr.app/sq/v2/worlds/role-token/jwks"));

async function verifyRoleToken(token) {
    try {
        const { payload } = await jwtVerify(token, sideQuestKeys, {
            issuer: "https://altvr.app",
            audience: "sq-world:" + MY_WORLD_ID,
            typ: "sq-world-role+jwt",
            algorithms: ["ES256"]
        });
        if (!useNonceOnce(payload.nonce)) return null;
        return payload;
    } catch {
        return null;                                 // bad signature, wrong world, expired...
    }
}
```

`createRemoteJWKSet` caches the keys and fetches them again when a token names a key it hasn't seen, which is how a new signing key reaches you.

### What's in a token

A token is a JWT signed with ES256. Its header is `{"alg": "ES256", "typ": "sq-world-role+jwt", "kid": "..."}`, and its claims are:

| Claim | What it is |
|---|---|
| `iss` | `https://altvr.app` |
| `aud` | `sq-world:<world id>` |
| `sub` | The player's SideQuest account id, as a string. It isn't the same as `user.uid` |
| `worlds_id` | The world's id, as a number |
| `slug` | The world's address name (`my-world` in `my-world.worldspace.host`), or `null` when it has none |
| `role` | `owner`, `moderator`, `staff` or `visitor` |
| `staff` | `true` for SideQuest staff, whatever their role |
| `nonce` | The nonce the page passed, when it passed one |
| `iat`, `exp` | When it was made and when it expires (Unix seconds, 5 minutes apart) |
| `jti` | A unique id for this token |

## Rules

- **Tokens last 5 minutes.** Get a new one when you need to check again; don't store them.
- **Nonces are single-use.** Issue one per check, remember it until the token would have expired, and refuse a second use. Without a nonce, anyone who sees a token can replay it for its 5 minutes.
- **Treat a token like a password while it's live.** Send it over HTTPS and keep it out of logs.
- **The role is checked when the token is made.** If you keep someone unlocked for a long session, check them again from time to time.
- **Not in Visual Scripting.** A graph can't talk to your server on its own, so role tokens are for pages only.
