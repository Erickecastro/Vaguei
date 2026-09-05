# Protótipo Mobile

## Decisão

Um protótipo Android é viável no ecossistema atual porque Avalonia 12 suporta Android com .NET 10. Ele deve ser criado como um projeto de entrada separado, referenciando uma camada compartilhada, sem converter o executável desktop em um projeto Android.

O objetivo inicial é validar busca, filtros, resultados, tema, abertura de vagas e seleção de currículo em um emulador. Fidelidade visual, publicação na loja e iOS ficam fora do primeiro experimento.

## Estado do ambiente em 5 de setembro de 2026

- .NET SDK 10.0.400 instalado.
- Android SDK e `adb` encontrados.
- Workload `.NET for Android` ainda não instalado.
- O projeto atual usa uma `Window` desktop com barra de título e redimensionamento próprios; esses elementos não devem ser levados para Android.

## Arquitetura proposta

```text
Vaguei.Domain          regras e modelos compartilhados
Vaguei.Application     busca, filtros e matching compartilhados
Vaguei.Collectors      integrações HTTP compartilhadas
Vaguei.ResumeParser    parsers compatíveis; validar APIs por plataforma
Vaguei.Infrastructure  persistência com implementação por plataforma
Vaguei.UI              views, estilos e componentes compartilhados
Vaguei.Desktop         janela, barra de título e integração desktop
Vaguei.Android         Activity, permissões e ciclo de vida Android
```

A extração de `Vaguei.UI` deve acontecer gradualmente. A tela desktop atual não deve ser duplicada inteira, pois isso criaria duas interfaces difíceis de manter.

## Primeira entrega

1. Instalar o workload Android com o SDK oficial da Microsoft.
2. Criar `Vaguei.UI` e mover recursos de tema e componentes independentes de `Window`.
3. Criar `Vaguei.Android` com `net10.0-android` e `AvaloniaMainActivity`.
4. Implementar navegação mobile em uma única coluna.
5. Adaptar seleção de documentos e armazenamento local ao Android.
6. Validar conectividade, abertura de URLs e ciclo de vida.
7. Gerar somente APK de debug para emulador ou dispositivo de teste.

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

