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
adb install -r src/Vaguei.Android/bin/Debug/net10.0-android/com.erickecastro.vaguei-Signed.apk
```

A configuração de debug incorpora os assemblies .NET no APK e não depende de Fast Deployment, portanto o arquivo funciona com instalação manual por `adb install` sem depender do pipeline do IDE.

A inicialização dessa configuração foi validada em um aparelho ARM64 real, com Android 16: o processo permaneceu ativo e o buffer de crashes ficou vazio.

Abra **Vaguei** pela lista de aplicativos. Também é possível iniciar pelo terminal:

```bash
adb shell monkey -p com.erickecastro.vaguei -c android.intent.category.LAUNCHER 1
```

## Roteiro funcional

- Alternar tema, fechar e abrir novamente; o tema deve persistir.
- Pesquisar diretamente por cargo, tecnologia e empresa.
- Alternar entre Somente Brasil e Brasil + exterior.
- Testar todos os períodos de publicação.
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
- Abrir e fechar a área Sobre.
- Rotacionar o aparelho durante a tela inicial e durante uma busca.

## Diagnóstico

Para acompanhar apenas mensagens relacionadas ao processo do aplicativo:

```bash
adb logcat --pid="$(adb shell pidof com.erickecastro.vaguei)"
```

Se uma instalação anterior usar assinatura incompatível, registre primeiro os dados que deseja preservar. Desinstalar o aplicativo apaga preferências e favoritos locais.

## Limitações conhecidas deste APK

- A composição visual foi readaptada para uma coluna; não replica barra de título ou painel lateral desktop.
- Arrastar e soltar arquivo, redimensionar janela e confirmar fechamento não se aplicam ao Android.
- A Jooble não é incorporada ao APK porque uma chave privada poderia ser extraída. Ela exigirá credencial pública autorizada ou proxy seguro.
- Não há assinatura de produção, publicação na Play Store ou mecanismo de atualização.
