# Integração com o desktop Linux

Para publicar e instalar o Vaguei somente para o usuário atual, sem `sudo`:

```bash
./packaging/linux/install-user.sh
```

Feche instâncias iniciadas anteriormente com `dotnet run` e abra **Vaguei**
pelo menu de aplicativos. O GNOME só consegue associar corretamente o ícone do
dock quando o programa é iniciado por sua entrada `.desktop` instalada.

Ao empacotar o Vaguei, instale `vaguei.desktop` em
`/usr/share/applications/` e os PNGs de `icons/hicolor/` nos diretórios
equivalentes de `/usr/share/icons/hicolor/`.

Durante o desenvolvimento, o ícone da janela já é definido pelo Avalonia.
O agrupamento correto no dock do GNOME depende da instalação do arquivo
`.desktop` e de `StartupWMClass=vaguei`.
