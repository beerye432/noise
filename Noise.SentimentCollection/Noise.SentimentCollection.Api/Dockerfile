FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Noise.SentimentCollection/Noise.SentimentCollection.csproj", "Noise.SentimentCollection/"]
RUN dotnet restore "Noise.SentimentCollection/Noise.SentimentCollection.csproj"
COPY . .
WORKDIR "/src/Noise.SentimentCollection"
RUN dotnet build "Noise.SentimentCollection.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Noise.SentimentCollection.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Noise.SentimentCollection.dll"]