# Dapplo.Utils
Some basic utilities used through multiple dapplo projects

WORK IN PROGRESS

- Documentation can be found [here](http://www.dapplo.net/blocks/Dapplo.Utils) (soon)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/yyieit327n41qijc?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-utils)
- Coverage Status: [![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Utils/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Utils?branch=master)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Utils.svg)](https://badge.fury.io/nu/Dapplo.Utils.Config)

Some things that are available in this library:
- AsyncLock, this allows you to lock in async code without blocking the thread.
- Extensions for Type, e.g. getting the default of a type: typeof(int).Default() this returns 0 (like default(int) but usable in non generic code)
- Extensions for string, e.g. FormatWith allows more clear formatting.
- UiContext which can be used to have Tasks run on the UI.
