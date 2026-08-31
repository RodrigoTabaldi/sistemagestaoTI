FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Elcop.TI.slnx ./
COPY src/Elcop.TI.Domain/Elcop.TI.Domain.csproj src/Elcop.TI.Domain/
COPY src/Elcop.TI.Application/Elcop.TI.Application.csproj src/Elcop.TI.Application/
COPY src/Elcop.TI.Infrastructure/Elcop.TI.Infrastructure.csproj src/Elcop.TI.Infrastructure/
COPY src/Elcop.TI.Web/Elcop.TI.Web.csproj src/Elcop.TI.Web/
COPY src/Elcop.TI.Domain/packages.lock.json src/Elcop.TI.Domain/
COPY src/Elcop.TI.Application/packages.lock.json src/Elcop.TI.Application/
COPY src/Elcop.TI.Infrastructure/packages.lock.json src/Elcop.TI.Infrastructure/
COPY src/Elcop.TI.Web/packages.lock.json src/Elcop.TI.Web/

RUN dotnet restore src/Elcop.TI.Web/Elcop.TI.Web.csproj --locked-mode

COPY src ./src
RUN dotnet publish src/Elcop.TI.Web/Elcop.TI.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "Elcop.TI.Web.dll"]
