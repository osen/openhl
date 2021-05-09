set -e

ROOTDIR="$(pwd)"
WORKDIR="$ROOTDIR/work"
DISTDIR="$ROOTDIR/dist"
SRCDIR="$ROOTDIR/src"
PREFIX="$ROOTDIR/prefix"

build_engine()
{
  cd "$SRCDIR/engine"

  #python3 waf configure -T release --64bits --single-binary --prefix="$PREFIX"
  python3 waf configure -T release --64bits --prefix="$PREFIX"
  python3 waf build
  python3 waf install
}

build_hlsdk()
{
  cd "$SRCDIR/hlsdk"

  # Remove fragile naming system (*_amd64)
  echo "" > cmake/LibraryNaming.cmake

  cmake -B build \
    -DCMAKE_BUILD_TYPE=Release \
    -D64BIT=ON \
    -DSERVER_INSTALL_DIR=valve \
    -DCLIENT_INSTALL_DIR=valve \
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

prepare_system()
{
  rm -rf "$PREFIX"
  cp -r "$ROOTDIR/system" "$PREFIX"
  mkdir -p "$PREFIX/share/xash3d"
  chmod +x "$PREFIX/bin/hl"
}

extract_valve()
{
  cd "$PREFIX/share/xash3d"
  "$SRCDIR/hlextract/build/hlextract" -p "$DISTDIR/half-life.gcf" -e valve -d .
  cp -r "$SRCDIR/overlays/valve/"* "valve"
}

clean()
{
  rm -rf "$PREFIX"
  rm -rf "$WORKDIR"
}

#clean
#mkdir -p "$WORKDIR"
#prepare_system
build_hlextract
extract_valve
#build_engine
#build_hlsdk

