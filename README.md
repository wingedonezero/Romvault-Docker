# Romvault-Docker

RomVault 3.7.5 with a **native web UI**. Built for unraid. No mono, no Wine, no VNC —
a Blazor web app runs directly on top of the RomVault engine and is compiled from the
source in this repo by GitHub Actions on every push to `main`, published to
`ghcr.io/wingedonezero/romvault:latest`.

## Layout

- `RVWorld/` — RomVault source: upstream 3.7.5 (git subtree of
  [RomVault/RVWorld](https://github.com/RomVault/RVWorld)) merged with the
  cross-platform Avalonia UI fork
  ([wingedonezero/RVWorld](https://github.com/wingedonezero/RVWorld)), plus:
  - `RVWorld/ROMVaultWeb/` — the web UI (Blazor Server over RomVaultCore). This is
    what the container runs.
  - `RVWorld/ROMVaultAvalonia/` — the native Linux desktop UI (kept buildable; not
    shipped in the image).
  - `RVWorld/ROMVault/` — the upstream WinForms UI, the behavioral reference.
- `Dockerfile` — stage 1 publishes ROMVaultWeb self-contained (linux-x64); stage 2 is
  a slim Debian runtime with a PUID/PGID entrypoint. ~150 MB image.
- `rootfs/` — container entrypoint (user mapping, first-run init, config seeding) and
  the default `RomVault3cfg.xml`.
- `unraid/unraid-template.xml` — unraid Docker template.
- `.github/workflows/build.yml` — build + push to GHCR on push to `main` / `v*` tags.

## Paths

| In container | On unraid | Holds |
|---|---|---|
| `/config` | `appdata/romvault` | `config/RomVault3cfg.xml`, scan cache, **`DatRoot/`** (drop DATs here) |
| `/roms` | e.g. `/mnt/user/Media/Roms` | **`RomRoot/`** (sorted sets) and **`ToSort/`** (dump zone) — same share, so fixes are instant renames |

## Install on unraid

1. Download the template:
   ```
   wget -O /boot/config/plugins/dockerMan/templates-user/my-romvault.xml \
     https://raw.githubusercontent.com/wingedonezero/Romvault-Docker/main/unraid/unraid-template.xml
   ```
2. **Docker → Add Container** → pick `romvault` from the template dropdown.
3. Check the two paths, hit Apply, then open `http://SERVER:3000`.

Runs as `PUID`/`PGID` (default `99`/`100`); the drop folders are kept group-writable so
you can manage DATs and ToSort over SMB.
