#!/usr/bin/env bash
# vocaloid6-ui-patch — install/remove the VOCALOID 6 dialog restyle shim in a wine prefix.
#
# The shim is a .NET startup hook (DOTNET_STARTUP_HOOKS): it loads into every
# .NET process in the prefix but exits immediately unless the process is
# VOCALOID6, where it restyles Yamaha.VOCALOID.DialogBase windows at runtime.
# No file of the VOCALOID installation is modified.
set -euo pipefail

SHARE_DIR="/usr/share/vocaloid6-ui-patch"
DLL_NAME="Vocaloid6UiPatch.dll"
WIN_DIR='C:\ProgramData\vocaloid6-ui-patch'
WIN_DLL="${WIN_DIR}\\${DLL_NAME}"
REG_KEY='HKCU\Environment'
REG_VAL='DOTNET_STARTUP_HOOKS'

prefix="${WINEPREFIX:-$HOME/.wine}"
wine_bin="${WINE:-wine}"

usage() {
    cat <<EOF
Usage: vocaloid6-ui-patch <apply|remove|status> [--prefix DIR]

  apply    Copy the shim into the wine prefix and register the startup hook.
  remove   Unregister the hook and delete the shim from the prefix.
  status   Show whether the hook is registered in the prefix.

The prefix defaults to \$WINEPREFIX or ~/.wine. The wine binary defaults to
\$WINE or "wine" from PATH.
EOF
    exit "${1:-0}"
}

cmd="${1:-}"
[[ -n "$cmd" ]] || usage 1
shift || true
while [[ $# -gt 0 ]]; do
    case "$1" in
        --prefix) prefix="$2"; shift 2 ;;
        -h|--help) usage ;;
        *) echo "unknown argument: $1" >&2; usage 1 ;;
    esac
done

[[ -d "$prefix/drive_c" ]] || { echo "error: '$prefix' is not a wine prefix" >&2; exit 1; }
dest="$prefix/drive_c/ProgramData/vocaloid6-ui-patch"

reg_query() {
    WINEPREFIX="$prefix" "$wine_bin" reg query "$REG_KEY" /v "$REG_VAL" 2>/dev/null \
        | sed -n "s/.*${REG_VAL}[[:space:]]*REG_SZ[[:space:]]*//p" | tr -d '\r'
}

case "$cmd" in
    apply)
        current="$(reg_query || true)"
        if [[ -n "$current" && "$current" != "$WIN_DLL" ]]; then
            echo "warning: $REG_VAL is already set to '$current' and will be replaced." >&2
        fi
        mkdir -p "$dest"
        install -m644 "$SHARE_DIR/$DLL_NAME" "$dest/$DLL_NAME"
        WINEPREFIX="$prefix" "$wine_bin" reg add "$REG_KEY" /v "$REG_VAL" /d "$WIN_DLL" /f >/dev/null
        echo "applied: hook registered in $prefix (restart VOCALOID 6 to see it)"
        ;;
    remove)
        WINEPREFIX="$prefix" "$wine_bin" reg delete "$REG_KEY" /v "$REG_VAL" /f >/dev/null 2>&1 || true
        rm -rf "$dest"
        echo "removed: hook unregistered from $prefix"
        ;;
    status)
        current="$(reg_query || true)"
        if [[ "$current" == "$WIN_DLL" && -f "$dest/$DLL_NAME" ]]; then
            echo "active in $prefix"
        elif [[ -n "$current" ]]; then
            echo "inactive ($REG_VAL points elsewhere: $current)"
        else
            echo "inactive in $prefix"
        fi
        ;;
    *) usage 1 ;;
esac
