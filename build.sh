set -e

ROOTDIR="$(pwd)"
DISTDIR="$ROOTDIR/dist"
SRCDIR="$ROOTDIR/src"
PREFIX="$ROOTDIR/prefix"

PYTHON=python3

build_engine()
{
  cd "$SRCDIR/engine"

  #$PYTHON waf configure -T release --64bits --single-binary --prefix="$PREFIX"
  $PYTHON waf configure -T release --64bits --prefix="$PREFIX"
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
    -DGAMEDIR="$PREFIX/lib/xash3d"

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
    -DGAMEDIR="$PREFIX/lib/xash3d"

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
  rm -rf "$PREFIX"
  cp -r "$ROOTDIR/system" "$PREFIX"
  chmod +x "$PREFIX/bin/hl"
  chmod +x "$PREFIX/bin/opfor"
}

extract_valve()
{
  mkdir -p "$PREFIX/share/xash3d"
  cd "$PREFIX/share/xash3d"
  "$SRCDIR/hlextract/build/hlextract" -p "$DISTDIR/half-life.gcf" -e valve -d .
  #"$SRCDIR/hlextract/build/hlextract" -p "$DISTDIR/opposing force.gcf" -e gearbox -d .
  cp -r "$SRCDIR/overlays/valve/"* "valve"
}

clean()
{
  rm -rf "$PREFIX"
}

clean
prepare_system
build_hlextract
build_wiseunpacker
extract_valve
build_engine
build_hlsdk
build_opforsdk

