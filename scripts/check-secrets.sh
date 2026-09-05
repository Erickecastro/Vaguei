#!/usr/bin/env bash
set -euo pipefail

repository_root="$(git rev-parse --show-toplevel)"
cd "$repository_root"

tracked_files="$(git ls-files -co --exclude-standard \
  | grep -Ev '(^|/)(bin|obj|TestResults)/' || true)"

if [[ -z "$tracked_files" ]]; then
  exit 0
fi

patterns='(AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9_]{20,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|JOOBLE_API_KEY[[:space:]]*=[[:space:]]*[^[:space:]#]+|Authorization:[[:space:]]*(Bearer|Basic)[[:space:]]+[A-Za-z0-9._~+/-]{12,})'

matches="$(printf '%s\n' "$tracked_files" \
  | xargs -r grep -nEIH "$patterns" 2>/dev/null \
  | grep -Eiv '(your-key|sua-chave|example|exemplo|placeholder|redacted|<[^>]+>)' \
  || true)"

if [[ -n "$matches" ]]; then
  echo "Possíveis segredos encontrados:" >&2
  echo "$matches" >&2
  echo "Remova os valores sensíveis antes do commit." >&2
  exit 1
fi

echo "Nenhum padrão conhecido de segredo foi encontrado."
