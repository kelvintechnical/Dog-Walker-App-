#!/usr/bin/env bash
set -euo pipefail
# Local disposable SQL Server Developer only. No paid host or production database.
fwf_secret_file=${1:?Pass an output path outside the repository for test connection settings}
fwf_container=${2:-fwf-m0-test-sql}
fwf_tls_dir="$fwf_secret_file.tls"
if [[ -e "$fwf_secret_file" || -e "$fwf_tls_dir" ]]; then
  echo "Refusing to overwrite existing test settings." >&2; exit 1
fi
umask 077
mkdir -p "$fwf_tls_dir"
openssl req -x509 -newkey rsa:2048 -nodes -days 1 \
  -keyout "$fwf_tls_dir/server.key" -out "$fwf_tls_dir/server.crt" \
  -subj '/CN=localhost' -addext 'subjectAltName=DNS:localhost,IP:127.0.0.1' \
  -addext 'extendedKeyUsage=serverAuth' >/dev/null 2>&1
cat /etc/ssl/certs/ca-certificates.crt "$fwf_tls_dir/server.crt" > "$fwf_tls_dir/ca-bundle.pem"
export MSSQL_SA_PASSWORD="Aa1!$(openssl rand -hex 24)"
# Bootstrap trust inside this disposable container, then run SQL Server as its normal user.
docker run -d --name "$fwf_container" --user 0 --entrypoint /bin/bash \
  -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD \
  -v "$fwf_tls_dir:/fwf-tls-source:ro" -p 127.0.0.1:14339:1433 \
  mcr.microsoft.com/mssql/server:2022-latest -c '
    set -e
    mkdir -p /var/opt/mssql/secrets
    cp /fwf-tls-source/server.key /fwf-tls-source/server.crt /var/opt/mssql/secrets/
    chown -R 10001:0 /var/opt/mssql/secrets
    chmod 600 /var/opt/mssql/secrets/server.key
    cp /fwf-tls-source/server.crt /usr/local/share/ca-certificates/fwf-test.crt
    update-ca-certificates >/dev/null
    /opt/mssql/bin/mssql-conf set network.tlscert /var/opt/mssql/secrets/server.crt >/dev/null
    /opt/mssql/bin/mssql-conf set network.tlskey /var/opt/mssql/secrets/server.key >/dev/null
    /opt/mssql/bin/mssql-conf set network.forceencryption 1 >/dev/null
    exec runuser -u mssql -- /opt/mssql/bin/sqlservr
  ' >/dev/null
for fwf_attempt in $(seq 1 60); do
  if docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" "$fwf_container" \
      /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -Q 'SELECT 1' >/dev/null 2>&1; then
    printf 'Server=127.0.0.1,14339;Database=master;User Id=sa;Password=%s;Encrypt=True;TrustServerCertificate=False;\n' \
      "$MSSQL_SA_PASSWORD" > "$fwf_secret_file"
    echo "Disposable SQL Server ready. Use SSL_CERT_FILE pointing to the generated ca-bundle.pem."
    exit 0
  fi
  sleep 1
done
echo "SQL Server did not become ready; inspect the test container, then remove it." >&2
exit 1
