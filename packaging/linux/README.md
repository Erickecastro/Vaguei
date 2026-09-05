# Integração com o desktop Linux

Ao empacotar o Vaguei, instale `vaguei.desktop` em
`/usr/share/applications/` e os PNGs de `icons/hicolor/` nos diretórios
equivalentes de `/usr/share/icons/hicolor/`.

Durante o desenvolvimento, o ícone da janela já é definido pelo Avalonia.
O agrupamento correto no dock do GNOME depende da instalação do arquivo
`.desktop` e de `StartupWMClass=vaguei`.
