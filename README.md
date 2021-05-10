# openhl
Digital preservation of Half-Life

Dependencies
------------
CMake  - Build system

SDL2   - Main engine uses it as the windowing system

Python - Waf build system used by some upstream modules. I aim to
         replace this with CMake.

Mono   - The only Wise unpacker I could find was witten in CSharp.
         I aim to rewrite this.
