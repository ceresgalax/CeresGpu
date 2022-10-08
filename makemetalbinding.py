import os
import shutil
import subprocess
import sys


def exec(*args):
    print(f'Running {" ".join(args)}')
    subprocess.run(args, check=True)


def main():
    this_dir = os.path.normpath(os.path.join(__file__, '..')) 
    os.chdir(this_dir)
    exec(sys.executable, 'genmetal.py')

    archs = [
        ('x86_64', 'osx-x64'),
        ('arm64', 'osx-arm64')
    ]

    metalbinding_project_dir = os.path.join(this_dir, 'metalbinding')
    metalbinding_project_path = os.path.join(metalbinding_project_dir, 'metalbinding.xcodeproj')

    for xcode_arch, dotnet_arch in archs:
        exec('xcodebuild', '-project', metalbinding_project_path, '-configuration', 'Debug', '-arch', xcode_arch)
        dest_dir = os.path.join(this_dir, 'runtimes', dotnet_arch, 'native')
        os.makedirs(dest_dir, exist_ok=True)
        shutil.copy(
            os.path.join(metalbinding_project_dir, 'build', 'Debug', 'libmetalbinding.dylib'),
            os.path.join(dest_dir, 'libmetalbinding.dylib')
        )


if __name__ == '__main__':
    main()
