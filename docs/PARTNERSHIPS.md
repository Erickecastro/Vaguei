# Integrações e Parcerias

## Princípios

O Vaguei integra somente APIs documentadas, feeds de carreira explicitamente públicos, dados licenciados ou fontes autorizadas por escrito. Não contorna autenticação, proteção anti-bot, limites de acesso ou restrições contratuais.

Cada integração deve preservar a atribuição, o identificador e a URL original da vaga, respeitar expiração e limites de consulta e permitir a desativação independente da fonte.

## Apresentação para plataformas

O Vaguei é um aplicativo de descoberta de oportunidades que normaliza vagas autorizadas e direciona o candidato ao canal oficial de candidatura. Currículos são analisados localmente e não são enviados aos parceiros na arquitetura atual.

## Checklist para uma nova fonte

1. Identificar responsável e documentação oficial.
2. Registrar autorização, licença ou termos aplicáveis.
3. Definir atribuição, URL canônica, retenção, expiração e rate limit.
4. Manter credenciais fora do repositório.
5. Mapear apenas os campos necessários.
6. Implementar timeout, cache, isolamento de falhas e testes.
7. Documentar canal de remoção e resposta a incidentes.

## Parcerias prioritárias

- Gupy: solicitar cadastro como job board parceiro e feed oficial JSON/XML.
- Jooble: utilizar chave oficial da API de pesquisa.
- InfoJobs, Jobbol e Catho: solicitar autorização específica para distribuição do catálogo.
- LinkedIn: integrar somente após contrato e aprovação explícita; não coletar páginas automaticamente.

## Matriz atual de acesso

| Situação | Fontes |
| --- | --- |
| Endpoint público ou página de carreira permitida | Arbeitnow, Ashby, Greenhouse, InHire, Jobicy, Lever, Remotive, SmartRecruiters e Workable |
| API oficial implementada, dependente de credencial | Jooble |
| Parceria ou autorização a solicitar | Gupy, InfoJobs, Jobbol e Catho |
| Sem coleta automatizada autorizada no momento | LinkedIn |

Essa classificação deve ser revisada sempre que uma fonte alterar seus termos, documentação ou modelo de acesso.
