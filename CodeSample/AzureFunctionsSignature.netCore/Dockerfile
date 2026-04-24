# Azure Functions .NET 8 Isolated Worker with headless Chromium.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AzureFunctionsSignature.netCore.csproj", "./"]
RUN dotnet restore "AzureFunctionsSignature.netCore.csproj"
COPY . .
RUN dotnet publish "AzureFunctionsSignature.netCore.csproj" \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false


FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime

# Chromium runtime dependencies for Debian bookworm.
# If a future Chrome version crashes with "error while loading shared libraries: libXXX.so",
# add the lib here and rebuild.
RUN apt-get update && apt-get install -y --no-install-recommends \
      ca-certificates \
      fonts-liberation \
      libasound2 \
      libatk-bridge2.0-0 \
      libatk1.0-0 \
      libcairo2 \
      libcups2 \
      libdbus-1-3 \
      libdrm2 \
      libexpat1 \
      libfontconfig1 \
      libgbm1 \
      libglib2.0-0 \
      libnspr4 \
      libnss3 \
      libpango-1.0-0 \
      libpangocairo-1.0-0 \
      libx11-6 \
      libx11-xcb1 \
      libxcb1 \
      libxcomposite1 \
      libxcursor1 \
      libxdamage1 \
      libxext6 \
      libxfixes3 \
      libxi6 \
      libxkbcommon0 \
      libxrandr2 \
      libxrender1 \
      libxshmfence1 \
      libxss1 \
      libxtst6 \
      wget \
      unzip \
      && rm -rf /var/lib/apt/lists/*

# Chrome for Testing — version must match the one bundled with PuppeteerSharp.
# Check available versions: https://googlechromelabs.github.io/chrome-for-testing/
ARG CHROME_VERSION=146.0.7680.153
RUN wget -q "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_VERSION}/linux64/chrome-linux64.zip" \
      -O /tmp/chrome.zip \
    && mkdir -p /home/site/wwwroot/chromium \
    && unzip -q /tmp/chrome.zip -d /home/site/wwwroot/chromium \
    && chmod +x /home/site/wwwroot/chromium/chrome-linux64/chrome \
    && rm /tmp/chrome.zip

# Chromium is already present in the image — block PuppeteerSharp from re-downloading it at runtime.
# One worker per instance = one Chromium process shared = lower RAM footprint.
ENV SE_DISABLE_AUTO_DOWNLOAD=1 \
    FUNCTIONS_WORKER_PROCESS_COUNT=1

COPY --from=build /app/publish /home/site/wwwroot
