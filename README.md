# Romvault-Docker

RomVault 3.7.5 with a native Linux [Avalonia](https://avaloniaui.net/) UI, streamed to the
browser via KasmVNC. Built for unraid. No mono, no Wine — the app is compiled from the
source in this repo by GitHub Actions on every push to `main` and published to
`ghcr.io/wingedonezero/romvault:latest`.

## Layout

- `RVWorld/` — RomVault source: upstream 3.7.5 (git subtree of
  [RomVault/RVWorld](https://github.com/RomVault/RVWorld)) merged with the
  cross-platform Avalonia UI fork
  ([wingedonezero/RVWorld](https://github.com/wingedonezero/RVWorld)).
  The WinForms UI under `RVWorld/ROMVault/` is the reference; the Avalonia UI under
  `RVWorld/ROMVaultAvalonia/` tracks it 1:1.
- `Dockerfile` — stage 1 publishes the Avalonia UI self-contained (linux-x64) with the
  .NET SDK; stage 2 drops it onto `ghcr.io/linuxserver/baseimage-kasmvnc:debianbookworm`.
- `rootfs/` — container overlay: app autostart, first-run init (creates folders, seeds
  settings), default `RomVault3cfg.xml`.
- `unraid/unraid-template.xml` — unraid Docker template.
- `.github/workflows/build.yml` — build + push to GHCR on push to `main` / `v*` tags.

## Paths

| In container | On unraid | Holds |
|---|---|---|
| `/config` | `appdata/romvault` | `config/RomVault3cfg.xml`, scan cache, `screenpos.xml`, **`DatRoot/`** (drop DATs here) |
| `/roms` | e.g. `/mnt/user/media/roms` | **`RomRoot/`** (sorted sets) and **`ToSort/`** (dump zone) — same share, so fixes are instant renames |

## Install on unraid

1. Docker tab → scroll to **Template Repositories** → add
   `https://github.com/wingedonezero/Romvault-Docker` → Save.
2. **Add Container** → pick `romvault` from the template dropdown.
3. Check the two paths, hit Apply, then open `http://SERVER:3000`.

Runs as `PUID`/`PGID` (default `99`/`100`), so appdata files stay editable over SMB —
drop DATs into `appdata/romvault/DatRoot` any time.
