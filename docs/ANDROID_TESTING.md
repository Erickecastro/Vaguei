# Teste do APK Android

## Preparar o aparelho

1. Abra **Configurações > Sobre o telefone**.
2. Toque sete vezes em **Número da versão** para habilitar as opções do desenvolvedor.
3. Em **Opções do desenvolvedor**, habilite **Depuração USB**.
4. Conecte o aparelho por USB e aceite a autorização RSA exibida nele.

No computador, confirme que o estado é `device`:

```bash
adb devices -l
```

## Instalar

O APK de debug não é versionado no Git. Depois de compilá-lo, instale com:

```bash
export ANDROID_SDK_ROOT=/caminho/para/o/Android/Sdk
export JAVA_HOME=/caminho/para/o/jdk-21
./scripts/build-android-apk.sh
adb install -r src/Vaguei.Maui/bin/Debug/net10.0-android/android-arm64/Vaguei-debug-arm64.apk
```

Para substituir completamente uma instalação anterior de teste — inclusive dados,
preferências e favoritos locais — desinstale antes e instale novamente:

```bash
adb uninstall com.erickecastro.vaguei.maui
adb install src/Vaguei.Maui/bin/Debug/net10.0-android/android-arm64/Vaguei-debug-arm64.apk
```

Use `adb install -r` quando quiser atualizar o aplicativo sem apagar dados locais.

A configuração de debug incorpora os assemblies .NET no APK e não depende de Fast Deployment, portanto o arquivo funciona com instalação manual por `adb install` sem depender do pipeline do IDE. O script gera somente `arm64`, alinha o pacote e valida a assinatura Android antes de exibir o caminho do APK.

A inicialização dessa configuração foi validada em um aparelho ARM64 real, com Android 16: o processo permaneceu ativo e o buffer de crashes ficou vazio.

A versão MAUI registra no `logcat` apenas fonte, duração, quantidade e tipo técnico da falha de busca. Termos pesquisados e conteúdo do currículo não são registrados.

Abra **Vaguei** pela lista de aplicativos. Também é possível iniciar pelo terminal:

```bash
adb shell monkey -p com.erickecastro.vaguei.maui -c android.intent.category.LAUNCHER 1
```

## Roteiro funcional

- Alternar tema, fechar e abrir novamente; o tema deve persistir.
- Pesquisar diretamente por cargo, tecnologia e empresa.
- Rolar diversos cards sem que o toque de rolagem acione seleção ou destaque do fundo.
- Alternar entre Brasil e Exterior, verificando que os resultados não misturem os dois escopos.
- Testar todos os períodos de publicação.
- Confirmar que os seletores de filtro usam o painel visual do Vaguei em vez do diálogo padrão do Android.
- Aplicar localização, modelo, contrato e senioridade.
- Limpar os filtros sem iniciar uma nova busca inesperada.
- Abrir e recolher o painel compacto de filtros.
- Salvar uma vaga, ativar Somente salvas e reiniciar o aplicativo.
- Abrir uma vaga e confirmar o redirecionamento ao navegador.
- Desconectar a internet, pesquisar e conferir o aviso temporário.
- Anexar PDF, DOCX, ODT e TXT pelo seletor de documentos.
- Confirmar nome, cargo e competências detectadas.
- Confirmar que anexar o currículo não inicia a busca automaticamente.
- Confirmar o destaque do botão Pesquisar e a compatibilidade após a busca.
- Remover o currículo pelo botão ×.
- Confirmar que uma fonte indisponível não mantém a busca carregando indefinidamente.
- Desligar a rede durante uma busca e confirmar cancelamento imediato com aviso compacto.
- Girar o aparelho com rotação automática ativa e confirmar que o aplicativo permanece em retrato.
- Conferir as abas Sobre, Privacidade, Termos, Licenças e Fontes.
- Abrir o teclado nos campos e confirmar que a tela não é recalculada ou redimensionada por completo.
- Confirmar que o seletor nativo concede acesso apenas ao currículo escolhido e não solicita fotos, vídeos ou armazenamento completo.
- Abrir e fechar a área Sobre.
- Rotacionar o aparelho durante a tela inicial e durante uma busca.

## Diagnóstico

Para acompanhar apenas mensagens relacionadas ao processo do aplicativo:

```bash
adb logcat --pid="$(adb shell pidof com.erickecastro.vaguei.maui)"
```

Se uma instalação anterior usar assinatura incompatível, registre primeiro os dados que deseja preservar. Desinstalar o aplicativo apaga preferências e favoritos locais.

## Limitações conhecidas deste APK

- A composição visual foi readaptada para uma coluna; não replica barra de título ou painel lateral desktop.
- Arrastar e soltar arquivo, redimensionar janela e confirmar fechamento não se aplicam ao Android.
- A Jooble não é incorporada ao APK porque uma chave privada poderia ser extraída. Ela exigirá credencial pública autorizada ou proxy seguro.
- Não há assinatura de produção, publicação na Play Store ou mecanismo de atualização.
