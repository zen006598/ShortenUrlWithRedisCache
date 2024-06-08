#!/bin/bash
export SA_PASSWORD=$(cat /run/secrets/sa_password)
/opt/mssql/bin/sqlservr