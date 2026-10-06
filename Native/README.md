# LuaJIT 2.0.4 runtime

Prebuilt **Windows x64** and **Linux x64** libraries are included. The Linux library
passed the original-script checks. The Windows DLL was cross-compiled with MinGW
GCC 13.2.0, checked as PE AMD64, and imports only the Windows system libraries
KERNEL32.dll and msvcrt.dll. It has not been executed on Windows.

Both use upstream commit `69e5342eb893815b18a1ec84ba74b0e0d1cc9beb`.
The Linux build used `make -j2 BUILDMODE=dynamic`; the Windows build used
`make -j2 HOST_CC=gcc CROSS=x86_64-w64-mingw32- TARGET_SYS=Windows BUILDMODE=dynamic`.
Binary hashes are recorded in `runtime-builds.json`.

Rebuild with `python Tools/build_luajit.py`. On Windows use the **x64 Native Tools
Command Prompt for Visual Studio**, with Python available. The script downloads
upstream LuaJIT's pinned 2.0.4 release commit and compiles a library for the host platform.

The supplied Noita `lua51.dll` is x86 and cannot be loaded into a 64-bit tModLoader
process. Its strings identify LuaJIT 2.0.4. This runtime is built from upstream
LuaJIT's MIT-licensed source; it contains no game code. Matching the interpreter
release does not establish native engine equivalence or identity with Nolla's DLL:
architecture, compilation options and possible vendor patches remain unverified.
See `LUAJIT-LICENSE.txt`.

`build_native.py` builds ordinary Lua 5.1.5 as an optional comparison runtime.
It replaces the same platform library filename; rebuild LuaJIT afterwards to use
the intended runtime. Ordinary Lua is not claimed to be Noita's exact interpreter.

Generated binaries can be packaged by tModLoader as mod resources. The mod copies
the platform library to its cache before loading it. Original Noita scripts are read
from a user-configured extracted data folder and are never packaged into this mod.
