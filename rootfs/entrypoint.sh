#!/bin/bash
set -e

PUID=${PUID:-99}
PGID=${PGID:-100}
UMASK=${UMASK:-022}

echo "───────────────────────────────────────"
echo " RomVault web  |  UID: ${PUID}  GID: ${PGID}"
echo "───────────────────────────────────────"

# First-run layout. /config is the unraid appdata folder; /roms is the media
# share. Never recursively chown /roms - it can hold terabytes of user data.
mkdir -p \
    /config/config \
    /config/DatRoot \
    /roms/RomRoot \
    /roms/ToSort

# Seed settings once: DatRoot stays in appdata, RomRoot/ToSort live on /roms.
if [ ! -f /config/config/RomVault3cfg.xml ]; then
    cp /defaults/RomVault3cfg.xml /config/config/RomVault3cfg.xml
fi

chown -R "${PUID}:${PGID}" /config
chown "${PUID}:${PGID}" /roms /roms/RomRoot /roms/ToSort

# The drop folders must be writable over SMB by any member of the share group
# (unraid users are in 'users'), not just the container user.
chmod 775 /config /config/config /config/DatRoot /roms /roms/RomRoot /roms/ToSort 2>/dev/null || true

umask "${UMASK}"
cd /config
exec setpriv --reuid="${PUID}" --regid="${PGID}" --clear-groups /app/ROMVaultWeb
