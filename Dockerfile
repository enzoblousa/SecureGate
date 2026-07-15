FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SecureGate.sln .
COPY src/SecureGate.Domain/SecureGate.Domain.csproj src/SecureGate.Domain/
COPY src/SecureGate.Application/SecureGate.Application.csproj src/SecureGate.Application/
COPY src/SecureGate.Api/SecureGate.Api.csproj src/SecureGate.Api/
COPY tests/SecureGate.Tests/SecureGate.Tests.csproj tests/SecureGate.Tests/
RUN dotnet restore src/SecureGate.Api/SecureGate.Api.csproj

COPY src/ src/
RUN dotnet publish src/SecureGate.Api/SecureGate.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SecureGate.Api.dll"]
