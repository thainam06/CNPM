FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY ShortVideoTranslator.Api/ShortVideoTranslator.Api.csproj ShortVideoTranslator.Api/
COPY ShortVideoTranslator.Business/ShortVideoTranslator.Business.csproj ShortVideoTranslator.Business/
COPY ShortVideoTranslator.Data/ShortVideoTranslator.Data.csproj ShortVideoTranslator.Data/
RUN dotnet restore ShortVideoTranslator.Api/ShortVideoTranslator.Api.csproj
COPY ShortVideoTranslator.Api/ ShortVideoTranslator.Api/
COPY ShortVideoTranslator.Business/ ShortVideoTranslator.Business/
COPY ShortVideoTranslator.Data/ ShortVideoTranslator.Data/
COPY ShortVideoTranslator.Frontend/src/ui-bundles.json ShortVideoTranslator.Frontend/src/ui-bundles.json
RUN dotnet publish ShortVideoTranslator.Api/ShortVideoTranslator.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
USER root
RUN apt-get update && apt-get install -y --no-install-recommends python3 python3-venv ca-certificates curl ffmpeg libgomp1 \
    && rm -rf /var/lib/apt/lists/*
COPY ShortVideoTranslator.Api/requirements-tts.txt /tmp/requirements-tts.txt
RUN python3 -m venv /opt/tts && /opt/tts/bin/python -m pip install --no-cache-dir -r /tmp/requirements-tts.txt \
    && rm /tmp/requirements-tts.txt
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p /app/App_Data /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app /home/app/.aspnet
ENV ASPNETCORE_URLS=http://+:8080 \
    HOME=/home/app \
    TextToSpeech__PythonExecutable=/opt/tts/bin/python \
    Storage__Directory=/app/App_Data
USER app
EXPOSE 8080
HEALTHCHECK --interval=20s --timeout=5s --start-period=30s --retries=3 CMD curl --fail --silent http://127.0.0.1:8080/api/health/ready || exit 1
ENTRYPOINT ["dotnet", "ShortVideoTranslator.Api.dll"]
