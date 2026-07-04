#!/bin/bash
# First-run layout. /config is the unraid appdata folder; /roms is the media
# share. Never recursively chown /roms — it can hold terabytes of user data.

mkdir -p \
    /config/config \
    /config/DatRoot \
    /roms/RomRoot \
    /roms/ToSort

# Seed settings once: DatRoot stays in appdata, RomRoot/ToSort live on /roms.
if [ ! -f /config/config/RomVault3cfg.xml ]; then
    cp /defaults/RomVault3cfg.xml /config/config/RomVault3cfg.xml
fi

lsiown -R abc:abc /config
lsiown abc:abc /roms /roms/RomRoot /roms/ToSort

# The drop folders must be writable over SMB by any member of the share group
# (unraid users are in 'users'), not just the container user.
chmod 775 /config /config/config /config/DatRoot /roms /roms/RomRoot /roms/ToSort 2>/dev/null || true
