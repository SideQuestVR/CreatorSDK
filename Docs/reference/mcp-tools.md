# MCP Tools & Events

Every tool and event of the app's MCP server. To start the server and connect an agent, see [AI Agents (MCP)](ai-agents-mcp.md).

Agents read these descriptions from the server themselves, so you rarely need them to write a prompt. They're here so you know what an agent can and can't do, and what it gets back.

## Tools

| Tool | What it does |
|---|---|
| [`get_state`](#get_state) | The agent's own body: where it is and looks, its hands, what they hold and press, and what the world allows. |
| [`wait_for_events`](#wait_for_events) | Waits until something happens, then returns it. |
| [`take_snapshot`](#take_snapshot) | A picture of what the player sees right now: the agent's eyes. |
| [`move`](#move) | Walks, turns, looks up or down and jumps, in any combination. |
| [`teleport`](#teleport) | Teleports with the teleport arc, as a person does. |
| [`hand`](#hand) | Moves a hand: in front of the body, onto something, pointing at something. |
| [`press`](#press) | Presses, holds or lets go of a controller button, trigger, grip or stick. |
| [`click`](#click) | Points a hand's laser at something and pulls the trigger. |
| [`grab`](#grab) | Puts a hand on something and grips it. |
| [`release`](#release) | Lets go of what a hand holds, or throws it. |
| [`type_text`](#type_text) | Types into the text field the keyboard is open for. |
| [`gesture`](#gesture) | High five, fist bump, handshake, holding hands, or a wave. |
| [`list_users`](#list_users) | Who is in the space and who is talking. |
| [`say`](#say) | Speaks text aloud into the space. |
| [`stop_speaking`](#stop_speaking) | Stops speaking straight away. |
| [`list_voices`](#list_voices) | The voices available, and which one is selected. |
| [`set_voice`](#set_voice) | Changes the voice for everything said from then on. |
| [`set_listening`](#set_listening) | Starts or stops hearing and transcribing the people in the space. |
| [`get_model_status`](#get_model_status) | Which speech recognition models are downloaded, and whether one is loaded. |
| [`download_model`](#download_model) | Downloads a speech recognition model in the background. |
| [`speak_tone`](#speak_tone) | Sends a test tone, to check that people can hear the agent at all. |

A tool that can't run says why in its answer, for example when the app isn't in a space yet, rather than failing silently. No call runs for longer than 5 minutes.

### The agent plays as a person in a headset

The agent has exactly what a person in a headset has, and uses it the same way: a head to look with, two hands, and the controllers' buttons, triggers, grips and sticks. It opens the menu with the left Y button, clicks with its laser, teleports with the teleport arc and picks things up by gripping them. Whatever your world allows or forbids a person (teleporting, grabbing, grappling, which surfaces you can teleport onto), it allows or forbids the agent. Nothing moves the player, an object or a menu any other way.

It sees only through its eyes: `take_snapshot`, plus its own body in `get_state`.

### Pointing at what it sees

Tools that act on something (`click`, `grab`, `hand`, `teleport`, `move`, `gesture`) take a **view point**: `{ "view": [u, v] }`, a place in the agent's latest snapshot. `u` runs from 0 at the left edge to 1 at the right, `v` from 0 at the top to 1 at the bottom, so `[0.5, 0.5]` is the middle. The agent finds what is there along the line of sight the snapshot was taken from, even if it has turned since. Most of these also take a world position instead.

### Directions and positions

Positions are world coordinates in metres, with **y** up, the same as in Unity and in your world's scripts. **Yaw** is a compass heading in degrees: 0 faces **+Z**, 90 faces **+X**. **Pitch** is how far up the player looks, from -80 (down) to 80 (up), 0 being level.

## Seeing and moving

### get_state

No arguments. Returns a snapshot of the app, in sections. Agents call it first to find out where they are.

| Section | What's in it |
|---|---|
| `server` | Server name, version, address and uptime. |
| `client` | Product, platform and whether it's running in the Unity editor. |
| `events` | `high_water_mark`: the number of the latest event. |
| `player` | `available` (and a `reason` when not), `position`, `head_position`, `yaw`, `pitch`, `facing`, `grounded`, `moving`, `can_look_up_down`, `can_jump`. |
| `body` | The agent's own body: `head`; each hand's `position`, `pointing`, what it is `holding`, the controls it is `holding_down`, and its `laser` (the label of what the laser is on, as a person sees it highlighted); `menu_open`, `mic_muted`, `seated`; `teleport` (whether it is allowed, and while aiming whether the ring shows or the red cross, and where it lands); `abilities` (move, turn, jump, teleport, grab, distance grab, grapple, fly: what the world allows right now); `held_by` when something is holding the controls off, such as a text field. |
| `networking` | Whether the app is connected to the space's room, the room, this player's id and how many people are in it. |
| `speech` | Whether the agent can speak, the selected voice and the voice engines. |
| `hearing`, `transcription`, `models` | Whether the agent is listening, audio counters, and the speech recognition model. |

### take_snapshot

A JPEG of the view in front of the player, taken from the app's camera.

| Argument | Default | Range |
|---|---|---|
| `width` | 1024 | 64 to 1920 pixels |
| `height` | 576 | 64 to 1080 pixels |
| `quality` | 80 | JPEG quality, 10 to 100 |
| `grid` | false | Draws faint lines every 0.1 of the width and height, to read view points off |

One snapshot is taken at a time: a second call while one is being taken is answered with "try again in a moment". To see somewhere else, turn or walk first, then take another.

### move

Walking, turning, looking up or down and jumping, in one call, because they happen together: you walk while watching someone, and jump mid-stride. The player is moved the way a person moves it, through the same stick input, so it collides, climbs stairs and steps up as a person would.

| Argument | What it does |
|---|---|
| `to` | Walk here: the floor at a view point, `{ "view": [0.5, 0.8] }`, or a position, `{ "x": 0, "y": 0, "z": 5 }`. |
| `yaw` | Face this heading, 0 to 360. |
| `turn_degrees` | Turn this much from the current heading, -360 to 360; positive turns right. |
| `look_at` | Face and look at this (a view point or a position), up or down included. |
| `pitch` | Look this far up, -80 to 80. |
| `pitch_degrees` | Look this much further up than now; negative looks further down. |
| `jump` | Jump. Combine it with `to` to jump while walking. |
| `timeout_s` | Give up after this long. Default 30, at most 120. |
| `arrive_radius` | How close counts as arriving, in metres. Default 0.5, from 0.1 to 5. |

Give at least one of them. `look_at` wins over `yaw`, which wins over `turn_degrees`. `pitch` wins over `pitch_degrees`, and either one replaces the up-or-down aim that `look_at` would set.

Walking on its own steers towards the destination. Give a facing as well and the player keeps that facing and sidesteps there instead, which is how an agent keeps watching someone while it moves.

The call waits until the player arrives and faces the right way, or stops making progress, or runs out of time. Looking and jumping happen straight away. It answers with:

| Field | Meaning |
|---|---|
| `outcome` | `Arrived`, `Timeout`, `Stuck` (something is in the way), `Busy` (another move is still running), `Cancelled` or `Unavailable`. |
| `position`, `yaw`, `pitch` | Where the player ended up. |
| `remaining_m` | How far from the destination it stopped, ignoring height. |
| `requested_yaw`, `off_by_degrees` | The heading asked for and how far off it ended; the sign tells an overshoot from a shortfall. |
| `requested_pitch`, `pitch_changed` | The pitch asked for and whether it could be set. |
| `jumped` | Whether the player left the ground. |
| `detail` | A sentence saying what happened. |

Not arriving is a normal answer, not an error: the agent reads `detail` and tries another way. Only `Unavailable` is reported as an error.

It walks with the left stick and turns with the right one, as your Controls settings say (snap or smooth turning, one-stick movement), and jumps with right A. Snap turning turns in whole steps, so a turn ends within half a step of the heading asked for.

### teleport

Teleports the way a person does: raises the right hand, holds B, steers the arc until its ring is on the spot, and lets go. It only lands where the arc can: on surfaces your world lets people teleport onto, within the arc's reach, and only where teleporting is allowed at all.

| Argument | What it does |
|---|---|
| `to` | Required. Where to go: a view point or a position. |
| `tolerance_m` | How close the ring has to get to the spot. Default 1. |

Answers with `teleported`, where the ring was and the `position` the player landed at. When the ring can't be put on the spot, nothing happens and `why` says so: no ground there, the red cross (somewhere you can't teleport onto), out of reach, or teleporting turned off. Use `move` to walk there instead.

### list_users

No arguments. Everyone in the space, the agent's own player included:

| Field | Meaning |
|---|---|
| `name` | Display name. |
| `user_id` | `sq:<account number>` for someone signed in, `guest:<id>` for a guest. |
| `peer_id` | Their id in the space's room, used in events. |
| `is_local` | `true` for the agent's own player. |
| `talking` | Whether they're talking right now. Always `false` for the agent's own player. |

There are no positions: to find someone, the agent looks for them with `take_snapshot`, turning if it needs to, and then walks over with `move` and a view point.

## Hands and buttons

### hand

Moves one hand, or both, at a person's speed and only as far as an arm reaches.

| Argument | What it does |
|---|---|
| `hand` | `left`, `right` (the default) or `both`. |
| `rest` | `true` puts the hand down by the hips. |
| `body` | Hold the hand here, in metres from the chest: `{ "forward": 0.4, "right": 0.2, "up": 0.1 }`. It moves with the player. |
| `reach` | Put the hand on this: a view point or a position. |
| `point_at` | Aim the hand, and its laser, at this. |
| `palm` | Which way the palm faces: `down`, `up`, `in`, `out`, `forward` (raised, fingers up) or `back`. |

Answers with where each hand got to, whether something held it back or it was out of reach, what it holds and what its laser is on.

### press

Uses one control on one controller. What it does is whatever the app and your world make it do, as for anyone.

| Argument | What it does |
|---|---|
| `control` | Required. `trigger`, `grip`, `a`, `b` (right), `x`, `y`, `menu` (left), `stick` or `stick_click`. |
| `hand` | For trigger, grip and the stick: `left` or `right` (the default). |
| `action` | `tap` (the default: press, then let go), `hold` or `release`. |
| `value` | How far to pull a trigger or grip, 0 to 1. |
| `stick` | Where to push the stick, `[x, y]` from -1 to 1. |
| `duration_ms` | How long a tap holds. Default 150. |

In the app, left Y (or the menu button) opens the menu, left X mutes the microphone, right A jumps and right B aims a teleport. The trigger clicks what the laser is on, uses a held item and fires the grapple where your world allows it; the grip grabs.

### click

Points a hand's laser at something and pulls the trigger: menu buttons, panels, the keyboard, things in your world that react to clicks, seats (to sit down) and people (to open their card). Only one hand points at a time; clicking with the other one first takes the pointer over with a trigger press, as it does for a person.

| Argument | What it does |
|---|---|
| `target` | Required. A view point. |
| `hand` | `left` or `right` (the default). |
| `hover_only` | `true` points without clicking. |
| `hold_ms` | How long the trigger is held. Default 120. |

Answers with what the laser was on, and whether a click registered.

### grab

Puts a hand on something and closes the grip, so it has to be within arm's reach. With `distance: true` it points at the thing and grips instead, where your world allows distance grab. Only things your world made grabbable can be picked up.

| Argument | What it does |
|---|---|
| `target` | Required. A view point or a position. |
| `hand` | `left` or `right` (the default). |
| `distance` | Grab from afar. |

### release

Opens the hand. With `throw_at` (a view point or a position) the arm swings that way and lets go mid-swing; `speed` is how hard, up to 8 metres a second.

### type_text

Types into the text field the keyboard is open for, as typing from your phone does. The agent clicks the field first, which opens the keyboard.

| Argument | What it does |
|---|---|
| `text` | Required. What to type. `\b` deletes a character. |
| `submit` | `true` presses Enter afterwards. |

### gesture

A hand gesture with someone: `high_five`, `fist_bump`, `handshake` (shaking for 3 seconds makes you friends), `hold_hand`, or a `wave`. It only counts when the other person takes part too, and their **Social Interactions** setting decides who can hold their hand.

| Argument | What it does |
|---|---|
| `kind` | Required. Which gesture. |
| `with` | The person, as a view point. Not needed for `wave`. |
| `hand` | `left` or `right` (the default). |
| `wait_s` | How long to wait for them to join in. Default 6. |

Answers with whether the hands met and whether the gesture was confirmed.

## Speaking

Speaking and hearing work only inside a space, once the app has connected to its room: `networking.state` in `get_state` is `Connected`. People hear the agent as your avatar's voice.

The agent's voice replaces your microphone while it plays, sentence by sentence, so your microphone can be heard in the short gaps between sentences. Muting yourself in the app doesn't silence the agent.

### say

Speaks text aloud and returns as soon as it's queued, with an `utterance_id`. A `say_started` event marks the start and `say_finished` the end. Text longer than 2000 characters is cut short.

If the app isn't connected to a room yet, `say` still answers that the text is queued, but nothing is heard: an `error` event with the same `utterance_id` follows instead of `say_started`.

| Argument | Default | What it does |
|---|---|---|
| `text` | | Required. What to say. Plain conversational sentences work best. |
| `voice` | | Say this one line in another voice, from `list_voices`, without changing the default. |
| `rate` | 1 | Speaking rate, 0.25 to 4. |
| `volume` | 0.7 | Loudness, 0 to 1. |
| `pitch_hz` | 110 | Pitch of the built-in voice, 50 to 300. |
| `robot` | 0 | How mechanical the built-in voice sounds, 0 to 1. Past about 0.3 it gets hard to understand. |

`rate` and `volume` apply to the built-in and Windows voices; ElevenLabs voices speak as they are.

### stop_speaking

No arguments. Stops straight away and drops anything still queued, for when someone interrupts. Answers with `was_speaking`.

### list_voices

No arguments. Every voice available, each with `name`, `engine`, `culture`, `gender` and `notes`, and the `selected` one. The best engine present is used by default:

| Engine | Sounds | Needs |
|---|---|---|
| ElevenLabs | Human | `-elevenlabs-key <key>` on the command line, and a connection |
| Windows | Clear | The voices installed in Windows |
| Built-in, named `robot` | Like a 1980s speech chip, hard to make out | Nothing; always there |

When no Windows voices can be used, the answer says why in `system_voices_unavailable`. With ElevenLabs, the default is the first voice on your ElevenLabs account.

If an ElevenLabs or Windows voice fails while speaking, the agent carries on in the built-in voice and an `error` event says so. It keeps the built-in voice until `set_voice` picks another.

### set_voice

| Argument | What it does |
|---|---|
| `voice` | Required. A name from `list_voices`, or a distinctive part of one: `zira` finds `Microsoft Zira Desktop`. `robot` picks the built-in voice. |

Changes the voice for everything said from then on. If the name matches nothing, the voice stays as it was.

### speak_tone

Sends a short tone as the agent's voice, to check that people in the space can hear it at all.

| Argument | Default | Range |
|---|---|---|
| `frequency_hz` | 440 | 50 to 4000 |
| `duration_ms` | 600 | 50 to 5000 |
| `volume` | 0.4 | 0 to 1 |

Keep tones to 2 seconds or less: the voice buffer holds about 2 seconds, so a longer tone loses its beginning. A tone sends `say_started` but no `say_finished`.

## Hearing

### set_listening

| Argument | What it does |
|---|---|
| `listening` | Required. `true` to start listening, `false` to stop. |

While listening, each person's voice is heard separately, so two people talking at once give two transcripts. The person using the app is heard too, from the microphone, as speaker `local` named `You`, even while muted in the app. Each utterance is transcribed when the speaker pauses, or after 15 seconds of talking without a pause, and arrives as a `speech_transcript` event. If transcription falls behind, at most 3 utterances wait; older ones are dropped with an `error` event.

Transcription needs a speech recognition model. Without one, listening still works and the talking events still arrive, but there are no transcripts; the answer says so.

### get_model_status

No arguments. Lists the speech recognition models and which are downloaded. `active` is the best one downloaded, which isn't necessarily loaded: `ready` and `engine` under `listening` say what's actually loaded.

### download_model

| Argument | What it does |
|---|---|
| `model` | Required. One of the models below. |

Returns straight away and downloads in the background; `model_progress` events report progress, and a failed download sends an `error` event with `source` `model_download`.

!> **Restart the app after the first download.** The model is loaded when the app starts, so one downloaded during a session isn't used until the next one. Until then `set_listening` works but no transcripts arrive.

### Speech recognition models

Transcription runs on your PC with [whisper.cpp](https://github.com/ggml-org/whisper.cpp). The models are downloaded from Hugging Face once, into `%USERPROFILE%\AppData\LocalLow\SideQuest\AltspaceVR\greenfield-mcp\models`. Each time the app starts, it loads the most accurate model downloaded. The app transcribes in English only, whichever model it uses.

| Model | Size | Notes |
|---|---|---|
| `tiny.en` | 74 MB | Fastest, least accurate. |
| `base.en` | 141 MB | More accurate and still quick on a PC: the usual choice. |
| `small.en` | 465 MB | Most accurate, slowest. |
| `tiny`, `base` | 74 MB, 141 MB | Multilingual versions, but the app still transcribes English only, so they add nothing here. |

## Events

Things the agent may want to react to. Each has a `seq` number that only goes up, the time it happened (`at`), a `type` and a `payload`:

```json
{
  "seq": 42,
  "at": "2026-10-20T14:03:11.5210000+00:00",
  "type": "speech_transcript",
  "payload": { "speaker_id": "p-7", "speaker_name": "Sam", "text": "Hi there!", "seconds": 1.4 }
}
```

| Type | When | Payload |
|---|---|---|
| `speech_transcript` | Someone finished saying something. | `speaker_id`, `speaker_name`, `text`, `seconds` |
| `user_started_talking` | Someone else in the space started talking (not the person using the app). Sent whether or not the agent is listening. | `peer_id`, `name`, `user_id` |
| `user_stopped_talking` | They've been quiet for 0.6 seconds. | `peer_id`, `name`, `user_id` |
| `user_joined` | Someone came into the space. | `peer_id`, `name`, `user_id` |
| `user_left` | Someone left. | `peer_id`, `name`, `user_id` |
| `connection_state` | The connection to the space's room changed. | `state` |
| `walk_finished` | A `move` call ended, including turn-, look- and jump-only calls and calls answered `Busy`. | `outcome`, `position`, `yaw` |
| `teleported` | A `teleport` landed. | `ring`, `position` |
| `say_started` | The agent started speaking, or sent a tone (which has no `say_finished`). | `utterance_id`, `text`; for a tone `kind`, `frequency_hz`, `duration_ms` |
| `say_finished` | It finished, or was stopped. | `utterance_id`, `cancelled`, `spoken_seconds` |
| `model_progress` | A model download moved on. | `model`, `percent`, `received_mb`, `total_mb`; `done` at the end |
| `dropped_events` | Events were missed (see below). | `count`, `reason` |
| `error` | Something went wrong in the background, such as speech that couldn't be sent. | `source` (`speech`, `stt` or `model_download`), `message` |

### wait_for_events

Waits until at least one event happens, then returns at once. If nothing happens before the timeout, it returns an empty list, which is a normal answer: the agent calls it again to keep listening. This works with every MCP client, because to the client it's just a tool that takes a while.

| Argument | Default | What it does |
|---|---|---|
| `timeout_s` | 20 | How long to wait, 0 to 120 seconds. |
| `since_seq` | where the last call ended | Return only events after this number. |

It answers with `events`, `count`, `timed_out`, and `next_seq` to pass as `since_seq` next time. Without `since_seq`, each call carries on where the agent's previous one ended, and an agent that has just connected starts from now rather than receiving old events.

The server keeps the latest 256 events. If an agent falls further behind than that, the first event it gets is `dropped_events` with how many it missed, so a gap never looks like a quiet room.

An agent that reacts to the space loops: act, `wait_for_events`, react, act again.

## Protocol details

For writing your own client, or checking the server by hand.

| | |
|---|---|
| Transport | MCP streamable HTTP, at `/mcp`. |
| Revisions | `2025-06-18`, `2025-03-26` and `2024-11-05`. A client asking for another is answered with `2025-06-18`. |
| Session | `initialize` returns an `Mcp-Session-Id` header; send it with every later request. A client that leaves it out is matched to the only open session, if there's exactly one. Sessions last until the app quits or `DELETE /mcp`; one unused for 2 hours is cleared the next time a client sends `initialize`. An unknown session gets HTTP 404: send `initialize` again. |
| Methods | `initialize`, `notifications/initialized`, `ping`, `tools/list`, `tools/call`, `logging/setLevel`. |
| Responses | A request is answered with a single JSON object, a batch (the `2024-11-05` revision) with a JSON array, and a notification with an empty `202`. |
| Event stream | `GET /mcp` with `Accept: text/event-stream` opens a stream that repeats every event as a `notifications/message` notification, with a heartbeat every 15 seconds. It needs no session, so you can watch it with curl. |
| Health check | `GET /healthz` answers `{"ok":true,"server":"greenfield-mcp","version":"0.1.0"}`. |
| Authentication | With `-mcp-token`, every request to `/mcp` needs `Authorization: Bearer <secret>`; without it the answer is 401. `/healthz` needs no token. |
| Origin | Requests with an `Origin` header are accepted only from `localhost`, `127.0.0.1` or `::1`; others get 403. |
| Limits | 8 connections at once, 8 MB per request body, 5 minutes per tool call. |

Tool results are MCP content: text holding a JSON object for most tools, and a JPEG image for `take_snapshot`. A tool that couldn't do what was asked sets `isError`.

To try it by hand with curl (in PowerShell, write `curl.exe`), send `initialize`, note the `Mcp-Session-Id` header that comes back, then call a tool with it:

```sh
curl -i http://127.0.0.1:42070/mcp \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"curl","version":"1"}}}'

curl http://127.0.0.1:42070/mcp \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: <id from the first answer>" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_state","arguments":{}}}'
```
