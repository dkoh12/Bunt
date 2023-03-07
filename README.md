# The Bunt Programming Language written in C#

It supports among many things.

- arithmetic
- functions
- anonymous functions / lambdas
- if statements
- while / for loops
- break
- continue
- lists
- recursion
- classes
- constructors
- inheritances
- instances
- fields
- methods
- properties
- closures
- print statements
- logical and / logical or
- local variables
- global variables
- variable declaration and assignment


## Why the name 'Bunt'?


The name "Bunt" comes from the baseball term "bunt". 

1. While an injured batter may not be able to hit a home run, he can still hit a bunt. I wrote the language while my wrist and arm were injured.

2. bunt is not a powerful swing but it's just enough to get the ball rolling and doing it's job of moving the teammate from first base to second base. Likewise this language is not powerful and performant but has just enough features to write a simple program.


## Bunt REPL

### Launch Bunt REPL from Visual Studio

open `bunt.sln` in Visual Studio. Press the Green arrow to build and launch the interpreter in a REPL session.

you will see a pop up window where you can interact with Bunt.

```
>
```

### Launch Bunt REPL from command line

Open Windows Powershell and run the following command from the root of the repo

```
\> dotnet build
```

you can also run `msbuild` or `dotnet msbuild`. This will generate executable files under `\bunt\bin\Debug\net6.0\`


you can run 
```
.\bunt\bin\Debug\net6.0\bunt.exe
```

and this will launch the REPL session.


## Execute Bunt files

Follow the same steps as `Launch Bunt REPL from command line` section. Pass a single file as an argument.

```
.\bunt\bin\Debug\net6.0\bunt.exe .\tests\fun.bunt
```