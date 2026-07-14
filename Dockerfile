FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MarvelTimelineApi.csproj", "./"]
RUN dotnet restore "MarvelTimelineApi.csproj"
COPY . .
RUN dotnet build "MarvelTimelineApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarvelTimelineApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Data ./Data
ENTRYPOINT ["dotnet", "MarvelTimelineApi.dll"]
