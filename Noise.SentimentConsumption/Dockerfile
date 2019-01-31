FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Noise.SentimentConsumption/Noise.SentimentConsumption.csproj", "Noise.SentimentConsumption/"]
RUN dotnet restore "Noise.SentimentConsumption/Noise.SentimentConsumption.csproj"
COPY . .
WORKDIR "/src/Noise.SentimentConsumption"
RUN dotnet build "Noise.SentimentConsumption.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Noise.SentimentConsumption.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Noise.SentimentConsumption.dll"]