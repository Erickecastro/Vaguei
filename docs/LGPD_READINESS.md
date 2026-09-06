# Preparação para LGPD

Última atualização: 6 de setembro de 2026.

Este documento registra evidências técnicas e pendências operacionais do Vaguei. Ele não representa certificação nem parecer jurídico.

## Escopo atual

O currículo escolhido é lido e analisado no próprio dispositivo. O aplicativo não possui conta, backend, telemetria ou upload do arquivo. Nome, histórico profissional e competências existem somente na memória da sessão; o arquivo original permanece sob controle do usuário.

Consultas formadas por cargo, competências e localização são enviadas às fontes de vagas configuradas. Esses termos podem constituir dados pessoais quando puderem ser relacionados a uma pessoa e, por isso, devem permanecer limitados ao necessário para a busca.

## Controles implementados

- Processamento local do currículo.
- Remoção de e-mail, telefone e URL antes da análise do perfil.
- Seleção explícita de PDF, DOCX, ODT ou TXT.
- Ausência de upload, conta, publicidade comportamental e telemetria.
- Persistência local limitada a tema, filtros, identificadores de favoritos e cache temporário de vagas públicas.
- Credenciais externas obtidas por variável de ambiente e verificação automatizada de segredos.
- Links de candidatura preservam o canal oficial da fonte.
- Falhas de provedores não exibem currículo nem conteúdo pessoal em diagnósticos.

## Pendências antes de distribuição pública ou comercial

- Identificar formalmente controlador, eventuais operadores e responsável pelo produto.
- Definir e documentar a base legal de cada finalidade; consentimento não deve ser adotado automaticamente quando outra base for a correta.
- Publicar um canal privado para solicitações de titulares e relatos de segurança.
- Definir procedimento para confirmação, acesso, correção, eliminação e informação sobre compartilhamento quando aplicável.
- Manter registro das operações de tratamento e uma avaliação documentada de riscos.
- Criar política simplificada de segurança, controle de versões dos avisos e processo de revisão periódica.
- Formalizar resposta a incidentes, critérios de risco e registros exigidos pela regulamentação aplicável.
- Revisar juridicamente Termos de Uso, Política de Privacidade, textos de aceite e relações com cada provedor.
- Confirmar termos, atribuição, retenção, limites e autorização de todas as fontes antes de lançamentos públicos.
- Avaliar transferências internacionais quando consultas ou serviços externos envolverem tratamento fora do Brasil.
- Conduzir testes de acessibilidade, segurança, dependências, empacotamento e atualização.

## Gatilhos para nova avaliação

Conta, sincronização, backend, telemetria, candidatura integrada, notificações remotas, analytics, monetização, processamento em nuvem ou armazenamento do currículo alteram materialmente o risco e exigem revisão antes de serem ativados.

## Referências normativas

- Lei nº 13.709/2018 — LGPD.
- Guias orientativos e regulamentos publicados pela Autoridade Nacional de Proteção de Dados.
- Resolução CD/ANPD nº 2/2022 para agentes de tratamento de pequeno porte, quando aplicável.
- Resolução CD/ANPD nº 15/2024 sobre comunicação de incidentes de segurança.
