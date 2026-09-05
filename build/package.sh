#!/bin/bash
# Assembles MewDummyDisplay.app and, for a release, the archive that ships.
#
#   ./build/package.sh                  universal release: arm64 + x86_64, zipped
#   ./build/package.sh --arch arm64     one architecture only
#   ./build/package.sh --arch x64       likewise
#   ./build/package.sh --debug          the Debug build of this machine, for development
#   ./build/package.sh --no-publish     reuse whatever is already published
#
# A release binary is NativeAOT: one Mach-O file with no runtime to install beside it.
# Two of them, one per architecture, are joined by lipo into a universal binary, which is
# how a Mac application ships for both Apple silicon and Intel in a single download.
#
# A Debug bundle is the whole framework-dependent layout instead, because its apphost
# expects the managed assemblies beside it. It is quicker to produce and behaves the same,
# so development uses it and nothing but the release path needs both architectures.
#
# Signing is ad-hoc, which is what an unnotarized build can have. See BUILD.md for what
# that means for anyone downloading it.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO_ROOT/src/MewDummyDisplay.App/MewDummyDisplay.App.csproj"
DIST="$REPO_ROOT/.artifacts/dist"
APP="$DIST/MewDummyDisplay.app"

MODE="release"
ARCHES=("osx-arm64" "osx-x64")
DO_PUBLISH=true

while [ $# -gt 0 ]; do
    case "$1" in
        --debug) MODE="debug" ;;
        --release) MODE="release" ;;
        --no-publish) DO_PUBLISH=false ;;
        --arch)
            case "${2:-}" in
                arm64) ARCHES=("osx-arm64") ;;
                x64) ARCHES=("osx-x64") ;;
                *) echo "--arch takes arm64 or x64"; exit 2 ;;
            esac
            shift
            ;;
        *) echo "unknown option: $1"; exit 2 ;;
    esac
    shift
done

HOST_ARCH="osx-arm64"
[ "$(uname -m)" = "x86_64" ] && HOST_ARCH="osx-x64"

BINARIES=()
if [ "$MODE" = "release" ]; then
    for rid in "${ARCHES[@]}"; do
        OUTPUT="$REPO_ROOT/src/MewDummyDisplay.App/bin/Release/net10.0/$rid/publish/MewDummyDisplay"
        if [ "$DO_PUBLISH" = true ]; then
            echo "publishing $rid"
            dotnet publish "$PROJECT" -c Release -r "$rid" --nologo -v quiet
        fi
        if [ ! -x "$OUTPUT" ]; then
            echo "missing $rid build. Drop --no-publish, or run: dotnet publish $PROJECT -c Release -r $rid"
            exit 2
        fi
        BINARIES+=("$OUTPUT")
    done
else
    # A Debug bundle is only ever for this machine, so it ignores --arch.
    SOURCE_DIR="$REPO_ROOT/src/MewDummyDisplay.App/bin/Debug/net10.0/$HOST_ARCH"
    if [ "$DO_PUBLISH" = true ]; then
        dotnet build "$PROJECT" --nologo -v quiet
    fi
    if [ ! -x "$SOURCE_DIR/MewDummyDisplay" ]; then
        echo "no Debug build. Run: dotnet build $PROJECT"
        exit 2
    fi
fi

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

if [ "$MODE" = "release" ]; then
    if [ "${#BINARIES[@]}" -gt 1 ]; then
        lipo -create "${BINARIES[@]}" -output "$APP/Contents/MacOS/MewDummyDisplay"
    else
        cp "${BINARIES[0]}" "$APP/Contents/MacOS/MewDummyDisplay"
    fi
else
    cp -R "$SOURCE_DIR"/* "$APP/Contents/MacOS/"
    # Debug symbols are not code, and codesign refuses a bundle holding a subcomponent it
    # cannot sign, so they do not go in.
    find "$APP/Contents/MacOS" -name '*.pdb' -delete
fi

cp "$REPO_ROOT/NOTICE" "$APP/Contents/Resources/NOTICE"

VERSION=$(grep -o '<Version>[^<]*' "$REPO_ROOT/src/Directory.Build.props" | head -1 | cut -d'>' -f2)

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key><string>MewDummyDisplay</string>
    <key>CFBundleIdentifier</key><string>com.aprillz.mewdummydisplay</string>
    <key>CFBundleName</key><string>MewDummyDisplay</string>
    <key>CFBundleDisplayName</key><string>MewDummyDisplay</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>${VERSION}</string>
    <key>CFBundleVersion</key><string>${VERSION}</string>
    <key>LSApplicationCategoryType</key><string>public.app-category.utilities</string>
    <key>LSMinimumSystemVersion</key><string>13.0</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>NSHumanReadableCopyright</key><string>Copyright (c) 2026 Aprillz. MIT licensed.</string>
    <!-- Menu bar only: no Dock icon and no app switcher entry. The application also
         reasserts the accessory activation policy at startup, because MewUI sets the
         regular policy while it starts, so a bare binary behaves the same way. -->
    <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST

# The sandbox was verified to allow the private virtual display API, so a release keeps it
# on. The entitlements file is kept outside the bundle: anything inside Contents becomes a
# signed subcomponent and codesign then refuses the bundle.
#
# A Debug bundle is signed without it. Its binary is framework-dependent and finds the
# runtime under /usr/local/share/dotnet, which the sandbox denies, so a sandboxed Debug
# bundle does not start at all. Sandbox behaviour therefore has to be checked on a release
# build, which is self-contained and has nothing to look up.
#
# --deep signs the nested Mach-O files first. A release bundle is one binary and does not
# need it, but a Debug one carries the managed assemblies in Contents/MacOS, where codesign
# takes every file for a subcomponent and refuses the bundle while any is unsigned.
#
# Failing here would leave a release bundle without its entitlement, so it is not swallowed.
if [ "$MODE" = "release" ]; then
    ENTITLEMENTS="$DIST/entitlements.plist"
    cat > "$ENTITLEMENTS" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key><true/>
</dict>
</plist>
PLIST
    codesign --force --sign - --entitlements "$ENTITLEMENTS" "$APP"
else
    codesign --force --deep --sign - "$APP"
fi

if [ "$MODE" = "release" ]; then
    ARCHIVE="$DIST/MewDummyDisplay-$VERSION-macos.zip"
    rm -f "$ARCHIVE"
    # ditto keeps the bundle's structure and its signature; plain zip does not.
    ditto -c -k --sequesterRsrc --keepParent "$APP" "$ARCHIVE"
    echo "architectures: $(lipo -archs "$APP/Contents/MacOS/MewDummyDisplay")"
    echo "archive:       $ARCHIVE"
fi

echo "bundle:        $APP"
echo "run it with:   open $APP"
