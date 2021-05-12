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
be found in the "dist" folder (split up into 100MB chunks to fit
in GitHub's restrictions).

Compilation / Usage
-------------------
The entire system can be compiled by changing to the project directory
and running:

  $ sh build.sh

Once this has finished the game can then be run via:

  $ export/bin/hl

That "export" folder can be renamed or moved to any location you
like. Effort has been made to ensure that there are no hard-coded
paths. This should ease packaging.

By default, both the main Half-Life game and Opposing Force is
compiled. However only the Half-Life data files are provided to
conserve space. Opposing Force can be obtained from the
steaminstall_full.exe package.

Bugs
----
- The steaminstall setup extractor is currently CSharp. So it drags in the
  whole of Mono. This is a pain so I plan to rewrite it. The code is technical
  but actually very clean and readable so this shouldn't be too bad.

- Upstream use a Python build system for C++ called waf for a few
  of the modules. I plan to rewrite this to keep with CMake.

- Changing video options via the menu is flaky. Try via the config file
  instead.

Dependencies
------------
CMake   - Build system
SDL2    - Main engine uses it as the windowing system
Python  - Waf build system used by some upstream modules.
Mono    - Extracts the game data (win32 self-extracting executable).

Acknowledgements
----------------
Flying with Gauss [https://xash.su/] - An open-source re-implementation
of the Half-Life engine. An outstanding technical feat.

Matt Nadareski [https://github.com/mnadareski] - An open-source
unpacker for self extracting win32 executables from Wise Solutions.
A really cool project in its own right and also very handy to
extract the Half-Life game data.

Ryan Freeman [https://slipgate.org] - Providing the initial OpenBSD port.
Whilst this project is a fork with a growing number of differences,
this port was still valuable to work out how it was built.

Valve - Probably. But I am still bitter about the whole Steam thing ;)

