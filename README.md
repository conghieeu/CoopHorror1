# 👻 Coop Horror Lab

> 🚧 **Work in Progress** — Actively in development.

A **cooperative multiplayer horror game** prototype built in Unity using **Unity Netcode for GameObjects (NGO)**. Players join together over Steam or LAN to survive — with Discord integration, NavMesh AI enemies, and a full character controller.

---

## 🎮 Overview

`unity-netcode-coop-lab` is a hands-on lab for building a **co-op horror experience** (codenamed `CoopHorror1`). It explores Unity's NGO networking stack combined with Steam transport, Discord Rich Presence, AI navigation, advanced character control, and multiplayer debugging tools.

---

## ✨ Features

- 👥 **Co-op multiplayer** via Unity Netcode for GameObjects (NGO)
- 🚂 **Facepunch Steam Transport** — peer-to-peer connection over Steam
- 💬 **Discord SDK** integration (Rich Presence / lobby)
- 🤖 **AI Navigation** — NavMesh-based enemies
- 🎮 **Advanced character controller** (Character Controller Pro)
- 📷 **Cinemachine** camera system
- 🦴 **Animation Rigging** for networked character animations
- 📳 **Haptic feedback** (Lofelt NiceVibrations)
- 💾 **Easy Save 3 (ES3)** — persistent save system
- 🖥️ **Quantum Console (QFSW.QC)** — in-game debug console
- 📊 **Multiplayer Tools** — NetStats Monitor, Network Profiler, Network Visualizer, Network Simulator
- ♻️ **Hot Reload** — live code changes without play mode restart
- 🔍 **ParrelSync** — multi-editor client testing

---

## 🛠️ Tech Stack

| Category | Package |
|----------|---------|
| Networking | Unity Netcode for GameObjects (NGO) |
| Transport | Facepunch Transport (Steam), Unity Transport (UTP2) |
| Social | Discord SDK |
| Character | Character Controller Pro (Lightbug) |
| Camera | Cinemachine |
| Animation | Unity Animation Rigging, Unity Timeline |
| AI | Unity AI Navigation (NavMesh) |
| Save | Easy Save 3 (ES3) |
| Feedback | MoreMountains Tools (Feel), Lofelt NiceVibrations |
| Debug | Quantum Console (QFSW.QC), Unity Multiplayer Tools |
| Input | Unity Input System |
| Inspector | Odin Inspector (Sirenix) |
| Performance | Unity Burst, Collections, Mathematics |
| Dev Tools | ParrelSync, Hot Reload, MCP For Unity |
| Shaders | ShaderLab (custom shaders) |

---

## 🚀 Getting Started

### Prerequisites

- **Unity 2022.3 LTS** or newer
- **Steam** installed (required for Facepunch transport)
- Unity packages auto-install via Package Manager on first open

### Clone & Open

```bash
git clone https://github.com/conghieeu/unity-netcode-coop-lab.git
```

1. Open **Unity Hub** → **Add project from disk** → select the folder
2. Let Unity import all packages
3. Open the main scene from `Assets/Scenes/`
4. Press ▶️ **Play** to run as Host, use **ParrelSync** to open a second editor as Client

### Testing Multiplayer Locally

This project includes **ParrelSync** for testing multiple clients in the editor simultaneously:

```
Window → ParrelSync → Clones Manager → Add new clone → Open in New Editor
```

---

## 📁 Project Structure

```
Assets/
├── Scenes/              # Game scenes
├── Scripts/
│   ├── Player/          # Player controller, NetworkBehaviour, input
│   ├── Enemy/           # AI NavMesh agents
│   ├── Network/         # NGO setup, lobby, connection management
│   ├── GameManager/     # Game state, round logic
│   └── UI/              # HUD, lobby screen, debug console
├── Prefabs/
│   ├── Player/          # Networked player prefabs
│   └── Enemy/           # AI enemy prefabs
├── Shaders/             # Custom ShaderLab shaders
└── Audio/               # SFX & atmosphere
```

---

## 🌐 Network Architecture

```
Host (Steam / LAN)
├── NetworkManager          ← NGO entry point
├── Facepunch Transport     ← Steam P2P connection
├── PlayerSpawner           ← Spawns NetworkObjects per client
├── EnemyAI (NavMesh)       ← Server-authoritative AI
└── Multiplayer Tools       ← Runtime stats & network visualization
```

---

## 🗺️ Roadmap

- [x] NGO + Facepunch Steam transport setup
- [x] Discord SDK integration
- [x] Character Controller Pro with networking
- [x] Cinemachine networked camera
- [x] NavMesh AI enemies
- [x] Multiplayer Tools monitoring
- [ ] Lobby & matchmaking flow
- [ ] Horror game loop (objectives, fail condition)
- [ ] Inventory & item interaction
- [ ] Audio atmosphere & spatial sound
- [ ] Full playtest build

---

## 📚 Key Resources

- 📘 [Unity Netcode for GameObjects Docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- 🚂 [Facepunch Transport](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.facepunch)
- 💬 [Discord GameSDK](https://discord.com/developers/docs/game-sdk/sdk-starter-guide)
- 🎮 [Character Controller Pro](https://lightbug.com.ar/character-controller-pro/)
- 🔍 [ParrelSync](https://github.com/VeriorPies/ParrelSync)

---

## 📄 License

Personal learning & portfolio project.

---

> Made with ❤️ by [Đoàn Công Hiếu](https://github.com/conghieeu)
