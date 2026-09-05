# Security Policy

## Reporting a vulnerability

The project is still preparing its official security contact. Until that channel is published, do not include credentials, private resume data, exploit details, or other sensitive information in public GitHub issues.

Before a public release, this section must be updated with a monitored private reporting address and an expected response window.

## Secrets and credentials

- Never commit API keys, tokens, passwords, cookies, private keys, signing certificates, or production connection strings.
- Store development credentials in environment variables or a platform secret store.
- Keep `.env` files local. Only `.env.example` with empty values belongs in the repository.
- Revoke and rotate a credential immediately if it appears in Git history; deleting the current line is not sufficient.
- Do not log resume contents, tokens, request authorization headers, or candidate contact information.
- Run `./scripts/check-secrets.sh` before committing or publishing changes.

The current Jooble integration reads `JOOBLE_API_KEY` from the process environment. The application does not persist that key.

## Supported versions

Vaguei is experimental and has no stable supported release yet. Security fixes are applied to the current development branch.

