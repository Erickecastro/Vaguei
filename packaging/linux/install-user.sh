#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
project_root="$(cd -- "${script_dir}/../.." && pwd)"

case "${1:-$(uname -m)}" in
  linux-x64|x86_64) runtime_id="linux-x64" ;;
  linux-arm64|aarch64) runtime_id="linux-arm64" ;;
  *)
    echo "Arquitetura não suportada. Use linux-x64 ou linux-arm64." >&2
    exit 1
    ;;
esac

data_dir="${XDG_DATA_HOME:-${HOME}/.local/share}"
app_dir="${data_dir}/vaguei"
desktop_dir="${data_dir}/applications"
publish_dir="${project_root}/artifacts/publish/${runtime_id}"

dotnet publish "${project_root}/src/Vaguei.Desktop/Vaguei.Desktop.csproj" \
  --configuration Release \
  --runtime "${runtime_id}" \
  --self-contained true \
  --output "${publish_dir}"

install -d "${app_dir}" "${desktop_dir}"
cp -a "${publish_dir}/." "${app_dir}/"
chmod +x "${app_dir}/vaguei"

for size in 32 48 64 128 256 512; do
  icon_dir="${data_dir}/icons/hicolor/${size}x${size}/apps"
  install -d "${icon_dir}"
  install -m 0644 \
    "${script_dir}/icons/hicolor/${size}x${size}/apps/vaguei.png" \
    "${icon_dir}/vaguei.png"
done

install -m 0644 "${script_dir}/vaguei.desktop" "${desktop_dir}/vaguei.desktop"
sed -i "s|^Exec=.*|Exec=${app_dir}/vaguei|" "${desktop_dir}/vaguei.desktop"

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database "${desktop_dir}"
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache --force --ignore-theme-index "${data_dir}/icons/hicolor" >/dev/null
fi

echo "Vaguei instalado. Abra-o pelo menu de aplicativos para o GNOME associar o ícone corretamente."
