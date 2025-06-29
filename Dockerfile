FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG NUGET_TOKEN
ARG NUGET_SOURCE
WORKDIR /source

# Add GitHub Packages NuGet source
RUN dotnet nuget add source $NUGET_SOURCE --name github --username PrintO-Studio --password $NUGET_TOKEN --store-password-in-clear-text

# Copy csproj and restore as distinct layers
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy and publish app and libraries
COPY . .
RUN dotnet publish -a $TARGETARCH -c Release -o /app

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 5000
WORKDIR /app
COPY --from=build /app .
ENV INFISICAL_CLIENT_ID=""
ENV INFISICAL_CLIENT_SECRET=""
ENV INFISICAL_PROJECT_ID=""
ENV INFISICAL_URL=""
USER $APP_UID
ENTRYPOINT ["dotnet", "PrintOSystem.dll"]