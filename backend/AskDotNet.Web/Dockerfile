FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/AskDotNet.Web/AskDotNet.Web.csproj", "backend/AskDotNet.Web/"]
COPY ["backend/AskDotNet.Core/AskDotNet.Core.csproj", "backend/AskDotNet.Core/"]
COPY ["backend/AskDotNet.Rag/AskDotNet.Rag.csproj", "backend/AskDotNet.Rag/"]
RUN dotnet restore "backend/AskDotNet.Web/AskDotNet.Web.csproj"
COPY . .
WORKDIR "/src/backend/AskDotNet.Web"
RUN dotnet build "./AskDotNet.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AskDotNet.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AskDotNet.Web.dll"]
