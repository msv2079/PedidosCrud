#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ -z "${Authentication__Username:-}" ]]; then
    read -r -p 'Escolha o usuário de acesso: ' Authentication__Username
fi

if [[ -z "${Authentication__Password:-}" ]]; then
    read -r -s -p 'Escolha a senha (12 a 256 caracteres): ' Authentication__Password
    printf '\n'
fi

if [[ -z "$Authentication__Username" || ${#Authentication__Username} -gt 100 || ${#Authentication__Password} -lt 12 || ${#Authentication__Password} -gt 256 ]]; then
    printf 'Usuário ou senha fora dos limites permitidos.\n' >&2
    exit 1
fi

Authentication__SigningKey="${Authentication__SigningKey:-$(openssl rand -base64 48 | tr -d '\n')}"
export Authentication__Username Authentication__Password Authentication__SigningKey

if [[ "${1:-}" == "--docker" ]]; then
    docker compose up --build
else
    dotnet run --project src/PedidosCrud.Web --launch-profile http
fi
