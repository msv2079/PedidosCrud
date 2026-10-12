FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json ./
COPY src/PedidosCrud.Model/PedidosCrud.Model.csproj src/PedidosCrud.Model/
COPY src/PedidosCrud.Repository/PedidosCrud.Repository.csproj src/PedidosCrud.Repository/
COPY src/PedidosCrud.Service/PedidosCrud.Service.csproj src/PedidosCrud.Service/
COPY src/PedidosCrud.Web/PedidosCrud.Web.csproj src/PedidosCrud.Web/
RUN dotnet restore src/PedidosCrud.Web/PedidosCrud.Web.csproj
COPY src/ src/
RUN dotnet publish src/PedidosCrud.Web/PedidosCrud.Web.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "PedidosCrud.Web.dll"]
