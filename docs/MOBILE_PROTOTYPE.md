# Protótipo Mobile

## Decisão

O protótipo Android usa um frontend .NET MAUI 10 com controles nativos. A implementação Android anterior em Avalonia foi removida. O desktop permanece em Avalonia; domínio, aplicação, coletores, infraestrutura, ViewModel e parsers continuam compartilhados.

O objetivo inicial é validar busca, filtros, resultados, tema, favoritos, compatibilidade, abertura de vagas e seleção de currículo em um aparelho. Publicação na loja e iOS ficam fora do primeiro experimento.

## Estado do ambiente em 5 de setembro de 2026

- .NET SDK 10.0.400 instalado.
- Android SDK e `adb` encontrados.
- Workload `.NET for Android` instalada.
- APK de debug com a lógica funcional compartilhada do desktop compilado com sucesso.
- O projeto atual usa uma `Window` desktop com barra de título e redimensionamento próprios; esses elementos não devem ser levados para Android.

## Arquitetura proposta

```text
Vaguei.Domain          regras e modelos compartilhados
Vaguei.Application     busca, filtros e matching compartilhados
Vaguei.Collectors      integrações HTTP compartilhadas
Vaguei.ResumeParser    parsers compatíveis; validar APIs por plataforma
Vaguei.Infrastructure  persistência com implementação por plataforma
Vaguei.Desktop         janela, barra de título e integração desktop
Vaguei.Maui            UI nativa, seletor, ciclo de vida e navegação Android
```

A extração de `Vaguei.UI` deve acontecer gradualmente. A tela desktop atual não deve ser duplicada inteira, pois isso criaria duas interfaces difíceis de manter.

## Funcionalidades adaptadas

- Pesquisa direta e pesquisa baseada no currículo.
- Importação local de PDF, DOCX, ODT e TXT pelo seletor Android.
- Remoção do arquivo temporário logo após a análise.
- Escopos exclusivos Brasil e Exterior.
- Filtros de período, localização, modelo, contrato e senioridade.
- Limpeza de filtros, favoritos persistentes e visualização de salvas.
- Fluxo exclusivo por etapas: preparação, carregamento e resultados.
- Preparação centralizada e filtros avançados em painel recolhível.
- Timeout geral de busca e política de rede mobile sem repetição demorada.
- Cancelamento imediato ao perder a conexão e mensagens técnicas resumidas na tela pequena.
- Orientação bloqueada em retrato e cinco abas institucionais equivalentes ao desktop.
- Resultados renderizados diretamente por `RecyclerView`, `LinearLayoutManager` e `ViewHolder` do Android. A física de rolagem, reciclagem e inércia permanece integralmente nativa; os cards compactos conservam o tema e os comandos compartilhados do MAUI.
- Coleta, leitura de cache, normalização e compatibilidade executadas fora da thread visual; a busca mantém todas as fontes configuradas e apenas devolve a interface após consolidar os resultados disponíveis.
- Seletores de filtros próprios, em estilo rádio, coerentes com os temas do Vaguei e sem empilhamento de diálogos Android.
- Ícone adaptativo e splash próprios; o splash usa marca preta transparente sobre branco puro.
- Consulta paralela das nove fontes independentes, com limites curtos por fonte e para a busca completa.
- Acesso a currículo pelo Storage Access Framework: somente o documento escolhido é concedido ao aplicativo, sem permissão ampla para fotos, vídeos ou armazenamento.
- Compatibilidade, justificativas e competências nos resultados.
- Diagnóstico de fontes, carregamento e aviso de falta de conexão.
- Tema claro/escuro persistente e área institucional Sobre.
- Abertura da candidatura no navegador original.

## Próximas entregas

1. Validar o APK de debug em emulador e dispositivo físico.
2. Extrair gradualmente recursos e componentes independentes de `Window`.
3. Validar seletor de documentos, armazenamento, rotação e retomada em aparelho real.
4. Refinar acessibilidade, tamanhos de toque e comportamento com teclado virtual.
5. Extrair os ViewModels para uma biblioteca de apresentação compartilhada definitiva.
6. Manter builds de teste sem assinatura de produção.

## Compilar o protótipo

O Fedora está configurado globalmente com JDK 25, mas o .NET Android 10 requer JDK 21. A build aponta explicitamente para o JDK compatível e não altera o Java do sistema:

```bash
dotnet build src/Vaguei.Maui/Vaguei.Maui.csproj \
  -p:AndroidSdkDirectory=/home/ericke/Android/Sdk \
  -p:JavaSdkDirectory=/usr/lib/jvm/java-21-temurin-jdk
```

O APK de debug é gerado sob `src/Vaguei.Maui/bin/Debug/net10.0-android/`. A pasta `bin` e arquivos `*.apk` são ignorados pelo Git.

Consulte o [roteiro de instalação e testes](ANDROID_TESTING.md).

## Segurança

- APK, AAB, `local.properties`, keystores e certificados não pertencem ao Git.
- O keystore de produção deve existir em armazenamento seguro e possuir backup privado.
- Senhas de assinatura devem vir de variável de ambiente ou arquivo externo protegido, nunca da linha de comando compartilhada ou do `.csproj`.
- Chaves de APIs agregadoras dentro de um aplicativo distribuído podem ser extraídas. Antes de produção, fontes que exigem segredo precisarão de proxy/backend ou credenciais restritas por plataforma.
- Currículos devem continuar locais e temporários, respeitando o seletor de documentos e permissões do Android.

## Critérios para avançar além do protótipo

- Fluxos principais funcionam em tela pequena sem conteúdo cortado.
- Nenhuma credencial privada está embutida no APK.
- Acessibilidade, teclado, rotação e retomada do aplicativo foram testados.
- Política de privacidade descreve corretamente o comportamento mobile.
- Builds são reproduzíveis e assinadas fora do repositório.
