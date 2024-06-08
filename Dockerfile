FROM mcr.microsoft.com/mssql/server:2022-latest
WORKDIR /usr/src/app
COPY entrypoint.sh .