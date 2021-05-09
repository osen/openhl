cmake_minimum_required(VERSION 2.8)
project(HLEXTRACT)

add_library(hl
  HLLib/BSPFile.cpp
  HLLib/Checksum.cpp
  HLLib/DebugMemory.cpp
  HLLib/DirectoryFile.cpp
  HLLib/DirectoryFolder.cpp
  HLLib/DirectoryItem.cpp
  HLLib/Error.cpp
  HLLib/FileMapping.cpp
  HLLib/FileStream.cpp
  HLLib/GCFFile.cpp
  HLLib/GCFStream.cpp
  HLLib/HLLib.cpp
  HLLib/Mapping.cpp
  HLLib/MappingStream.cpp
  HLLib/MemoryMapping.cpp
  HLLib/MemoryStream.cpp
  HLLib/NCFFile.cpp
  HLLib/NullStream.cpp
  HLLib/PAKFile.cpp
  HLLib/Package.cpp
  HLLib/ProcStream.cpp
  HLLib/SGAFile.cpp
  HLLib/Stream.cpp
  HLLib/StreamMapping.cpp
  HLLib/Utility.cpp
  HLLib/VBSPFile.cpp
  HLLib/VPKFile.cpp
  HLLib/WADFile.cpp
  HLLib/Wrapper.cpp
  HLLib/XZPFile.cpp
  HLLib/ZIPFile.cpp
)

add_executable(hlextract
  HLExtract/Main.c
)
target_link_libraries(hlextract hl)
