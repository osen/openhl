Open Half-Life
==============

This project is an attempt at the digital preservation of Half-Life.
The focus is not on features but instead to create a maintainable
codebase which can be easily ported to future POSIX platforms.

The goal is not necessarily to support as many platforms as possible,
but instead open / sane platforms that are accessible to people. I
don't plan to faff about with AppStore packaging systems. I also
don't plan to support any platform which does not provide a system
compiler.

Game Data
---------
The game data provided by this project is appropriate to distribute
and does not constitute piracy. The game data was made public by
Valve and shared on popular services such as FilePlanet and magazine
demo disks.

The original intention was probably to popularize their Steam DRM
platform but nonetheless, the data was made public. It is also
extracted using standard GCF tools (as distributed on Valve's
Developer Wiki). There has been no cracking involved. These publicly
distributable files are as follows:

  steaminstall_halflife.exe - Provides Half-Life
  steaminstall_opfor.exe    - Provides Opposing Force
  steaminstall_full.exe     - Provides Half-Life and Opposing Force

Only the former is provided by this project to save space. It can
be found in the "dist" folder (once LFS fetch is performed).

Compilation / Usage
-------------------

The Half-Life game data must first be fetched. If you already had
git-lfs installed when you cloned the project, this would have
happened automatically. If you installed it after then you must run
the following command to manually fetch it.

  $ git lfs pull

The entire system can be compiled by changing to the project directory
and running:

  $ sh build.sh

Once this has finished the game can then be run via:

  $ prefix/bin/hl

That "prefix" folder can be renamed or moved to any location you
like. Effort has been made to ensure that there are no hard-coded
paths. This should ease packaging.

By default, both the main Half-Life game and Opposing Force is
compiled. However only the Half-Life data files are provided to
conserve space. Opposing Force can be obtained from the
steaminstall_full.exe package.

Bugs
----
- No background in the menu. The game data is a little out of sync with
  the code. Work will be done to fix this.

- The steaminstall setup extractor is currently CSharp. So it drags in the
  whole of Mono. This is a pain so I plan to rewrite it.

- Upstream use a Python build system for C++ called waf for a few
  of the modules. I plan to rewrite this to keep with CMake.

Dependencies
------------
CMake   - Build system
SDL2    - Main engine uses it as the windowing system
Python  - Waf build system used by some upstream modules.
Mono    - Extracts the game data (win32 self-extracting executable).
git-lfs - To store the large installers on GitHub.

Acknowledgements
----------------
Flying with Gauss [https://xash.su/] - An open-source re-implementation
of the Half-Life engine. An outstanding technical feat.

Ryan Freeman [slipgate.org] - Providing the initial OpenBSD port.
Whilst this project is a fork with a growing number of differences,
this port was still valuable to work out how it was built.

