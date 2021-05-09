set -e

ROOTDIR="$(pwd)"
WORKDIR="$ROOTDIR/work"
DISTDIR="$ROOTDIR/dist"
PREFIX="$ROOTDIR/prefix"

build_engine()
{
  cd "$WORKDIR"
  rm -rf engine
  git clone --recurse-submodules https://github.com/FWGS/xash3d-fwgs.git engine
  cd engine
  #python3 waf configure -T release --64bits --single-binary --prefix="$PREFIX"
  python3 waf configure -T release --64bits --prefix="$PREFIX"
  python3 waf build
  python3 waf install
}

build_hlsdk()
{
  cd "$WORKDIR"
  rm -rf hlsdk
  git clone https://github.com/FWGS/hlsdk-xash3d.git hlsdk
  cd hlsdk

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
  cd "$WORKDIR"
  rm -rf hlextract

  cmake -B hlextract "$ROOTDIR/src/hlextract" \
    -DCMAKE_BUILD_TYPE=Release

  cmake --build hlextract
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
  cd "$WORKDIR"
  rm -rf extras
  git clone https://github.com/FWGS/xash-extras.git extras

  rm -rf "$PREFIX/share/xash3d/valve"
  cd "$PREFIX/share/xash3d"
  "$WORKDIR/hlextract/hlextract" -p "$DISTDIR/half-life.gcf" -e valve -d .
  cp -r "$WORKDIR/extras/"* "valve"
}

clean()
{
  rm -rf "$PREFIX"
  rm -rf "$WORKDIR"
}

clean
mkdir -p "$WORKDIR"
prepare_system
build_hlextract
extract_valve
build_engine
build_hlsdk

