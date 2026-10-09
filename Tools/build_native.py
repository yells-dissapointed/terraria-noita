"""Build 64-bit upstream Lua 5.1.5; no Noita code or executables are required."""
import argparse
import hashlib
import os
from pathlib import Path
import platform
import shutil
import subprocess
import tarfile
import urllib.request

SHA256 = '2640fc56a795f29d28ef15e13c34a47e223960b0240e8cb0a82d9b0738695333'

def build(archive=None):
    root=Path(__file__).resolve().parents[1]
    temp=root/'Native/build';temp.mkdir(parents=True,exist_ok=True)
    package=Path(archive).resolve() if archive else temp/'lua-5.1.5.tar.gz'
    if not package.exists():
        urllib.request.urlretrieve('https://www.lua.org/ftp/lua-5.1.5.tar.gz',package)
    if hashlib.sha256(package.read_bytes()).hexdigest()!=SHA256:
        raise RuntimeError('Lua source archive checksum mismatch')
    with tarfile.open(package) as tar:
        tar.extractall(temp,filter='data')
    source=temp/'lua-5.1.5/src'
    files=[str(p.resolve()) for p in sorted(source.glob('*.c')) if p.name not in ['lua.c','luac.c','print.c']]
    output=root/'Native';output.mkdir(exist_ok=True)
    if platform.system()=='Windows':
        if not shutil.which('cl'):
            raise RuntimeError('Run from the x64 Native Tools Command Prompt for Visual Studio')
        target=output/'terrarianoita_lua51.dll'
        subprocess.run(['cl','/nologo','/O2','/LD','/DLUA_BUILD_AS_DLL',*files,'/link','/OUT:'+str(target.resolve())],cwd=temp,check=True)
    elif platform.system()=='Darwin':
        target=output/'libterrarianoita_lua51.dylib'
        subprocess.run(['cc','-O2','-dynamiclib','-DLUA_USE_MACOSX',*files,'-lm','-o',str(target)],check=True)
    else:
        target=output/'libterrarianoita_lua51.so'
        subprocess.run(['cc','-O2','-fPIC','-shared','-DLUA_USE_LINUX',*files,'-lm','-ldl','-o',str(target)],check=True)
    print(target)
    return target

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--archive');args=parser.parse_args()
    build(args.archive)
