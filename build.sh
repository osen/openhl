set -e

ROOTDIR="$(pwd)"
DISTDIR="$ROOTDIR/dist"
SRCDIR="$ROOTDIR/src"
PREFIX="$ROOTDIR/prefix"

PYTHON=python3
MD5="/bin/md5 -q"

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

do_checksum()
{
  FILENAME="$1"
  MATCH="$2"

  echo "Checksum: $FILENAME"

  CS="$($MD5 "$1")"

  if [ "$CS" != "$MATCH" ]; then
    echo "Error: Checksum didn't match"
    echo "Perhaps project was not checked out using LFS (Large File Support)"
    exit 1
  fi
}

extract_valve()
{
  cd "$DISTDIR"
  echo "Extracting public Half-Life installer"
  mono "$SRCDIR/wiseunpacker/WiseUnpacker.exe" steaminstall_halflife.exe
  mv steaminstall_halflife/MAINDIR\\SteamApps\\half-life.gcf half-life.gcf
  rm -rf steaminstall_halflife

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

#clean

do_checksum "$DISTDIR/steaminstall_halflife.exe" "b2db14b71fa98695a30de98792bfd51a"

prepare_system
build_hlextract
build_wiseunpacker
extract_valve
build_engine
build_hlsdk
build_opforsdk

