# Inventário de dados

Última atualização: 6 de setembro de 2026.

| Dado | Finalidade | Local | Retenção atual | Compartilhamento |
|---|---|---|---|---|
| Arquivo de currículo selecionado | Extração do perfil | Dispositivo do usuário | O Vaguei não cria uma cópia permanente | Nenhum |
| Nome, cargo, experiência e competências extraídos | Busca e compatibilidade | Memória do processo | Até remoção do currículo ou encerramento do aplicativo | Termos necessários podem ser enviados às fontes; o currículo não é enviado |
| E-mail, telefone e URLs encontrados no currículo | Nenhuma finalidade necessária | Memória durante sanitização | Descartados antes da análise de perfil | Nenhum |
| Tema e filtros | Preferências do usuário | Armazenamento local | Até alteração ou remoção dos dados do aplicativo | Nenhum |
| Identificadores de vagas favoritas | Organização local | Armazenamento local | Até desfavoritar ou remover os dados do aplicativo | Nenhum |
| Catálogos públicos de vagas | Desempenho e resiliência | Cache local desktop | Até 30 minutos, limitado a 128 entradas | Nenhum compartilhamento adicional |
| Cargo, competências e localização pesquisados | Consulta de vagas | Memória e requisições HTTPS | Durante a consulta; retenção posterior depende da fonte externa | Fontes habilitadas necessárias à busca |
| Chave oficial Jooble, quando configurada | Autenticação na API | Variável de ambiente | Controlada pelo ambiente; não persistida pelo Vaguei | Jooble durante a requisição |

O inventário deve ser revisado sempre que uma nova integração, persistência ou finalidade for adicionada.
