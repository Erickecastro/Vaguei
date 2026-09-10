#!/usr/bin/env bash
set -euo pipefail

repository_root="$(git rev-parse --show-toplevel)"
cd "$repository_root"

: "${ANDROID_SDK_ROOT:?Defina ANDROID_SDK_ROOT para o diretório do Android SDK.}"
: "${JAVA_HOME:?Defina JAVA_HOME para um JDK 21.}"

build_tools_directory="$(find "$ANDROID_SDK_ROOT/build-tools" -mindepth 1 -maxdepth 1 -type d | sort -V | tail -n 1)"
zipalign="$build_tools_directory/zipalign"
apksigner="$build_tools_directory/apksigner"
debug_keystore="$HOME/.android/debug.keystore"
unsigned_apk="src/Vaguei.Maui/bin/Debug/net10.0-android/android-arm64/com.erickecastro.vaguei.maui.apk"
output_apk="src/Vaguei.Maui/bin/Debug/net10.0-android/android-arm64/Vaguei-debug-arm64.apk"
temporary_apk="$(mktemp --suffix=.apk)"

trap 'rm -f "$temporary_apk"' EXIT

[[ -x "$zipalign" ]] || { echo "zipalign não encontrado no Android SDK." >&2; exit 1; }
[[ -x "$apksigner" ]] || { echo "apksigner não encontrado no Android SDK." >&2; exit 1; }

dotnet build src/Vaguei.Maui/Vaguei.Maui.csproj \
  -f net10.0-android -c Debug --no-restore \
  -p:RuntimeIdentifier=android-arm64 -m:1 -nr:false

[[ -f "$unsigned_apk" ]] || { echo "APK arm64 não foi gerado." >&2; exit 1; }

"$zipalign" -f -p 4 "$unsigned_apk" "$temporary_apk"
"$apksigner" sign \
  --ks "$debug_keystore" \
  --ks-pass pass:android \
  --key-pass pass:android \
  --ks-key-alias androiddebugkey \
  --out "$output_apk" \
  "$temporary_apk"
"$apksigner" verify --verbose "$output_apk"

echo "APK de depuração validado: $output_apk"
