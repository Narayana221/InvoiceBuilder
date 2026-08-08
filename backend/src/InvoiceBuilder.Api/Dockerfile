FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore InvoiceBuilder.slnx
RUN dotnet publish src/InvoiceBuilder.Api/InvoiceBuilder.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# IronPDF renders via headless Chromium — these shared libraries aren't in the minimal aspnet image.
RUN apt-get update && apt-get install -y --no-install-recommends \
    libc6-dev \
    libgtk2.0-0 \
    libnss3 \
    libatk-bridge2.0-0 \
    libx11-xcb1 \
    libxcb-dri3-0 \
    libdrm-common \
    libgbm1 \
    libasound2t64 \
    libappindicator3-1 \
    libxrender1 \
    libfontconfig1 \
    libxshmfence1 \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InvoiceBuilder.Api.dll"]
