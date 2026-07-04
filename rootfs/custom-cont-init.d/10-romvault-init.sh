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
