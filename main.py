import os
from lint_csharp import lint_csharp
from lint_python import lint_python

def main():
    file_path = input("Enter the file path: ")
    file_extension = os.path.splitext(file_path)[1]
    
    with open(file_path, 'r') as file:
        code = file.read()

    if file_extension == '.py':
        lint_python(code)
    elif file_extension == '.cs':
        lint_csharp(code)
    else:
        print(f"No linter available for files with extension {file_extension}")

if __name__ == "__main__":
    main()
