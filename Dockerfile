# The API, built in one image and run from a much smaller one, so that neither
# the SDK nor the source code ends up where the app runs.

FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble AS build
WORKDIR /src

# The project files alone first, so that the packages are downloaded again only
# when these change, rather than on every change to the code.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY AnimalClassifier/AnimalClassifier.csproj AnimalClassifier/
COPY AnimalClassifier.Core/AnimalClassifier.Core.csproj AnimalClassifier.Core/
COPY AnimalClassifier.Infrastructure/AnimalClassifier.Infrastructure.csproj AnimalClassifier.Infrastructure/
RUN dotnet restore AnimalClassifier/AnimalClassifier.csproj -r linux-x64

COPY . .

# A clone without Git LFS holds a small pointer in place of the model, which
# would otherwise build fine and fail only once the app has started.
RUN test "$(stat -c %s AnimalClassifier/MLModel.mlnet)" -gt 1000000 \
    || { echo "MLModel.mlnet is not the real model: run git lfs pull." >&2; exit 1; }

# For Linux alone, which leaves out every other platform's native libraries.
RUN dotnet publish AnimalClassifier/AnimalClassifier.csproj \
    --configuration Release --runtime linux-x64 --self-contained false \
    --no-restore --output /publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble

# What OpenCV's native library needs that this image does not already have.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgtk-3-0t64 libatomic1 libdrm2 \
    && rm -rf /var/lib/apt/lists/*

# Where the volumes are mounted, which take their owner from these folders. The
# image's own non-root user can read and write them, and nobody else can.
RUN install -d -o "$APP_UID" -m 700 /data/uploads /data/keys

WORKDIR /app
COPY --from=build /publish .

USER $APP_UID

# The port the image already has the app listen on, as a non-root user cannot
# take one below 1024. Plain HTTP, as the reverse proxy in front handles HTTPS.
EXPOSE 8080

ENTRYPOINT ["dotnet", "AnimalClassifier.dll"]
