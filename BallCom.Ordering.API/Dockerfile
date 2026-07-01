FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BallCom.Ordering.API/BallCom.Ordering.API.csproj", "BallCom.Ordering.API/"]
RUN dotnet restore "BallCom.Ordering.API/BallCom.Ordering.API.csproj"
COPY ["BallCom.Ordering.API/", "BallCom.Ordering.API/"]
RUN dotnet publish "BallCom.Ordering.API/BallCom.Ordering.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BallCom.Ordering.API.dll"]
