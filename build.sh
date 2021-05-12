set -e

ROOTDIR="$(pwd)"
DISTDIR="$ROOTDIR/dist"
SRCDIR="$ROOTDIR/src"
DESTDIR="$ROOTDIR/export"

if [ -z "$PYTHON" ]; then
  PYTHON=python3
fi

build_engine()
{
  cd "$SRCDIR/engine"

  #$PYTHON waf configure -T release --64bits --single-binary --prefix="$DESTDIR"
  $PYTHON waf configure -T release --64bits --prefix="$DESTDIR"
  $PYTHON waf build
  $PYTHON waf install
}

build_hlsdk()
{
  cd "$SRCDIR/hlsdk"

  cmake -B build \
    -DCMAKE_BUILD_TYPE=Release \
    -D64BIT=ON \
    -DSERVER_INSTALL_DIR=valve \
    -DCLIENT_INSTALL_DIR=valve \
    -DGAMEDIR="$DESTDIR/lib/xash3d"

  cmake --build build
  cmake --build build --target install
}

build_opforsdk()
{
  cd "$SRCDIR/opfor-sdk"

  cmake -B build \
    -DCMAKE_BUILD_TYPE=Release \
    -D64BIT=ON \
    -DSERVER_INSTALL_DIR=gearbox \
    -DCLIENT_INSTALL_DIR=gearbox \
    -DGAMEDIR="$DESTDIR/lib/xash3d"

  cmake --build build
  cmake --build build --target install
}

build_hlextract()
{
  cd "$SRCDIR/hlextract"

  cmake -B build \
    -DCMAKE_BUILD_TYPE=Release

  cmake --build build
}

build_wiseunpacker()
{
  cd "$SRCDIR/wiseunpacker"
  make
}

prepare_system()
{
  mkdir "$DESTDIR"
  cp -r "$ROOTDIR/system/"* "$DESTDIR"
  chmod +x "$DESTDIR/bin/hl"
  chmod +x "$DESTDIR/bin/opfor"
}

extract_valve()
{
  cd "$DISTDIR"
  # Recombine after the
  # split -b 100M steaminstall_halflife.exe steaminstall_halflife.exe.
  echo "Combining steaminstall_halflife.exe"
  cat steaminstall_halflife.exe.aa > steaminstall_halflife.exe
  cat steaminstall_halflife.exe.ab >> steaminstall_halflife.exe
  cat steaminstall_halflife.exe.ac >> steaminstall_halflife.exe

  echo "Extracting public Half-Life installer"
  mono "$SRCDIR/wiseunpacker/WiseUnpacker.exe" steaminstall_halflife.exe
  mv steaminstall_halflife/MAINDIR\\SteamApps\\half-life.gcf half-life.gcf
  rm -rf steaminstall_halflife

  mkdir -p "$DESTDIR/share/xash3d"
  cd "$DESTDIR/share/xash3d"
  "$SRCDIR/hlextract/build/hlextract" -p "$DISTDIR/half-life.gcf" -e valve -d .
  #"$SRCDIR/hlextract/build/hlextract" -p "$DISTDIR/opposing force.gcf" -e gearbox -d .
  cp -r "$SRCDIR/overlays/valve/"* "valve"
}

prepare_system
build_hlextract
build_wiseunpacker
extract_valve
build_engine
build_hlsdk
build_opforsdk

