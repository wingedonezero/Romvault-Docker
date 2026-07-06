# syntax=docker/dockerfile:1
#
# RomVault (3.7.5 core) with a native Blazor web UI - no mono, no Wine,
# no X server, no VNC. The web app is compiled from this repo's source as a
# self-contained linux-x64 binary, so the runtime stage needs no .NET install
# and nothing is pulled from distro package feeds at update time.
#
# Path contract (matches the unraid template):
#   /config  ->  appdata: settings (config/RomVault3cfg.xml), scan cache,
#                and DatRoot/ (drop your DATs there)
#   /roms    ->  media share: RomRoot/ (sorted sets) + ToSort/ (dump zone),
#                same filesystem so fixes are instant renames

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY RVWorld/ .
RUN dotnet publish ROMVaultWeb/ROMVaultWeb.csproj \
      -c Release \
      -r linux-x64 \
      --self-contained true \
      -o /out

FROM debian:bookworm-slim

# libicu: globalization support for the self-contained .NET app.
# curl: healthcheck.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libicu72 curl ca-certificates \
 && apt-get clean \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /out /app
COPY rootfs/ /
RUN chmod +x /entrypoint.sh

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=10s --start-period=90s \
  CMD curl -fsS http://localhost:3000/health >/dev/null || exit 1

VOLUME /config

ENTRYPOINT ["/entrypoint.sh"]
