# Maintainer: Daniel <danielbyomujuni@nekosyndicate.com>
#
# Builds the VOCALOID 6 dialog restyle shim (a .NET startup hook) and installs
# it with a helper script. After installing the package, run
#   vocaloid6-ui-patch apply
# to register it in your wine prefix ($WINEPREFIX or ~/.wine).
#
# The shim compiles against Microsoft's WPF reference assemblies (fetched as a
# checksummed nupkg below); at runtime it uses the .NET runtime that VOCALOID 6
# already has inside the wine prefix, so nothing .NET is needed on the host
# beyond the SDK at build time.
#
# The project sources are taken from this repository itself ($startdir), so the
# package can be built straight from a checkout with makepkg/paru. When
# publishing to the AUR, either push this whole repo as the AUR package or swap
# the source array to a git/tarball URL of the repository.

pkgname=vocaloid6-ui-patch
pkgver=1.0.0
pkgrel=1
pkgdesc="Catppuccin restyle for VOCALOID 6 dialogs under Wine (.NET startup-hook shim, no app files modified)"
arch=('any')
url="https://github.com/trainerlord/vocaloid6-ui-patch"
license=('MIT')
depends=('bash')
makedepends=('dotnet-sdk-8.0')
optdepends=('wine: to register the hook in a wine prefix (any wine build works)')
install=vocaloid6-ui-patch.install

_wdref=microsoft.windowsdesktop.app.ref
_wdrefver=8.0.12
source=("${_wdref}-${_wdrefver}.nupkg::https://api.nuget.org/v3-flatcontainer/${_wdref}/${_wdrefver}/${_wdref}.${_wdrefver}.nupkg")
noextract=("${_wdref}-${_wdrefver}.nupkg")
sha256sums=('848db68eab40a3ba7dab0dfac7e15de271d58efcbd1c7f032c8a6c48fd6273ba')

prepare() {
    mkdir -p wdref
    bsdtar -xf "${_wdref}-${_wdrefver}.nupkg" -C wdref
    cp -r "$startdir/shim" "$srcdir/"
}

build() {
    cd "$srcdir/shim/Vocaloid6UiPatch"
    export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
    dotnet build -c Release -o "$srcdir/out" \
        -p:WpfRefDir="$srcdir/wdref/ref/net8.0"
}

package() {
    install -Dm644 "$srcdir/out/Vocaloid6UiPatch.dll" \
        "$pkgdir/usr/share/vocaloid6-ui-patch/Vocaloid6UiPatch.dll"
    install -Dm755 "$startdir/vocaloid6-ui-patch.sh" \
        "$pkgdir/usr/bin/vocaloid6-ui-patch"
    install -Dm644 "$startdir/LICENSE" \
        "$pkgdir/usr/share/licenses/$pkgname/LICENSE"
    install -Dm644 "$startdir/README.md" \
        "$pkgdir/usr/share/doc/$pkgname/README.md"
}
