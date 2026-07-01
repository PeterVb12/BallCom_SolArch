FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.API/BallCom.API.csproj", "BallCom.API/"]
RUN dotnet restore "BallCom.API/BallCom.API.csproj"
COPY ["BallCom.API/", "BallCom.API/"]
RUN dotnet publish "BallCom.API/BallCom.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.API.dll"]
