# syntax=docker/dockerfile:1
#
# RomVault (3.7.5 core + Avalonia Linux UI) streamed to the browser via KasmVNC.
# No mono, no Wine: the Avalonia UI is compiled from this repo's source in the
# build stage as a self-contained linux-x64 app, so the runtime stage needs no
# .NET install and nothing is pulled from distro package feeds at update time.
#
# Path contract (matches the unraid template):
#   /config  ->  appdata: settings (config/RomVault3cfg.xml), scan cache,
#                screenpos.xml, and DatRoot/ (drop your DATs there)
#   /roms    ->  media share: RomRoot/ (sorted sets) + ToSort/ (dump zone),
#                same filesystem so fixes are instant renames

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY RVWorld/ .
RUN dotnet publish ROMVaultAvalonia/ROMVaultAvalonia.csproj \
      -c Release \
      -r linux-x64 \
      --self-contained true \
      -o /out

FROM ghcr.io/linuxserver/baseimage-kasmvnc:debianbookworm

# ICU is the only runtime lib the self-contained .NET app needs that the
# KasmVNC desktop base doesn't already ship.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libicu72 \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*

ENV TITLE=RomVault

COPY --from=build /out /app
COPY rootfs/ /

EXPOSE 3000

VOLUME /config
