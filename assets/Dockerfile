#
# Stage 1, Build Backend
#
FROM mcr.microsoft.com/dotnet/sdk:7.0 as backend

WORKDIR /src

# Copy nuget project files.
COPY *.sln ./

# Copy the main source project files
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done

RUN dotnet restore

COPY . .

# Publish
RUN dotnet publish --no-restore Squidex.Assets.ResizeService/Squidex.Assets.ResizeService.csproj --output /build/ --configuration Release


#
# Stage 3, Build runtime
#
FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim

# Default AspNetCore directory
WORKDIR /app

# Copy from backend build stages
COPY --from=backend /build/ .

EXPOSE 80

ENTRYPOINT ["dotnet", "Squidex.Assets.ResizeService.dll"]
