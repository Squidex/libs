#
# Stage 1, Build Backend
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend

WORKDIR /src

# Copy nuget project files.
COPY *.sln ./

# Copy the main source project files
COPY ai/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ai/${file%.*}/ && mv $file ai/${file%.*}/; done

COPY assets/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p assets/${file%.*}/ && mv $file assets/${file%.*}/; done

COPY caching/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p caching/${file%.*}/ && mv $file caching/${file%.*}/; done

COPY events/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p events/${file%.*}/ && mv $file events/${file%.*}/; done

COPY flows/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p flows/${file%.*}/ && mv $file flows/${file%.*}/; done

COPY hosting/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p hosting/${file%.*}/ && mv $file hosting/${file%.*}/; done

COPY log/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p log/${file%.*}/ && mv $file log/${file%.*}/; done

COPY messaging/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p messaging/${file%.*}/ && mv $file messaging/${file%.*}/; done

COPY text/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p text/${file%.*}/ && mv $file text/${file%.*}/; done

COPY utils/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p utils/${file%.*}/ && mv $file utils/${file%.*}/; done

RUN dotnet restore

COPY . .

# Publish
RUN dotnet publish --no-restore assets/Squidex.Assets.ResizeService/Squidex.Assets.ResizeService.csproj --output /build/ --configuration Release


#
# Stage 3, Build runtime
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

# Default AspNetCore directory
WORKDIR /app

# Copy from backend build stages
COPY --from=backend /build/ .

EXPOSE 80

ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "Squidex.Assets.ResizeService.dll"]
