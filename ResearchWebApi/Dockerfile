FROM mcr.microsoft.com/dotnet/aspnet:3.1-alpine3.16 AS base
WORKDIR /src
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1-alpine3.16 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ResearchWebApi.csproj
RUN dotnet build "./ResearchWebApi.csproj" -c Debug -o /out

FROM build AS publish
RUN dotnet publish ResearchWebApi.csproj -c Debug -o /out


# Building final image used in running container
FROM base AS final
# Installing procps on the container to enable debugging of .NET Core
RUN apk update \
    && apk add unzip procps
WORKDIR /src
COPY --from=publish /out .
ENV ASPNETCORE_URLS=http://*:5000
# RUN kubectl port-forward --namespace default postgresql-1-postgresql-0 5432

ENTRYPOINT ["dotnet", "ResearchWebApi.dll"]
