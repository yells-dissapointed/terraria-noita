"""Build the upstream LuaJIT release identified in the supplied Noita DLL."""
import argparse
from pathlib import Path
import platform
import shutil
import subprocess

COMMIT = '69e5342eb893815b18a1ec84ba74b0e0d1cc9beb'

def build(existing_source=None):
    if platform.machine().lower() not in ('x86_64', 'amd64'):
        raise RuntimeError('This pinned LuaJIT prototype currently targets x64 hosts only')
    root=Path(__file__).resolve().parents[1]
    source=Path(existing_source).resolve() if existing_source else root/'Native/build/LuaJIT'
    if not source.exists():
        source.parent.mkdir(parents=True,exist_ok=True)
        subprocess.run(['git','clone','https://github.com/LuaJIT/LuaJIT.git',str(source)],check=True)
        subprocess.run(['git','-C',str(source),'checkout',COMMIT],check=True)
    actual=subprocess.check_output(['git','-C',str(source),'rev-parse','HEAD'],text=True).strip()
    if actual!=COMMIT:raise RuntimeError('LuaJIT source is not the pinned 2.0.4 release')
    if platform.system()=='Windows':
        if not shutil.which('cl'):raise RuntimeError('Use the x64 Native Tools Command Prompt for Visual Studio')
        subprocess.run(['cmd','/c','msvcbuild.bat'],cwd=source/'src',check=True)
        built=source/'src/lua51.dll';name='terrarianoita_lua51.dll'
    else:
        subprocess.run(['make','-j2','BUILDMODE=dynamic'],cwd=source,check=True)
        built=source/'src/libluajit.so';name='libterrarianoita_lua51.so'
        if platform.system()=='Darwin':name='libterrarianoita_lua51.dylib'
    output=root/'Native'/name
    shutil.copy2(built,output)
    shutil.copy2(source/'COPYRIGHT',root/'Native/LUAJIT-LICENSE.txt')
    print(output)
    return output

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--source');a=parser.parse_args();build(a.source)
