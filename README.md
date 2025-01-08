![Workflow](https://github.com/yak3d/StarfieldVoiceTool/actions/workflows/dotnet-desktop.yml/badge.svg?branch=main)  [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# Starfield Voice Tool
This tool is used to find voice lines by voice type in Starfield, filter them, play them and export them. 

## Requirements
* Starfield must be installed in a way that [Mutagen](https://github.com/Mutagen-Modding/mutagen) can find it. Installation by Steam should work fine.
* [ffmpeg](https://www.ffmpeg.org/), optional but VFRT will download if its not found on `%PATH%`

## Installation
1. Download a [Release Zip](https://github.com/yak3d/StarfieldVoiceTool/releases) or a [Nightly Zip](https://github.com/yak3d/StarfieldVoiceTool/actions)
2. Extract to a folder of your choice

## Running
1. Ensure your load order in `plugins.txt` or your Mod Manager is set with the plugins you want VFRT to scan
2. Execute `StarfieldVFRT.exe`, the first start up will take some time as it goes through your load order and finds the dialogue lines
3. This data is cached at `%LOCALAPPDATA%\StarfieldVFRT\cache.json`
   * When the game or StarfieldVFRT updates, it is imperative to either delete this file or go to **File** --> **Delete Cache** and then restart `StarfieldVFRT.exe`
5. During startup, `ffmpeg.exe` will be downloaded to the installation directory. It is sourced from https://github.com/ffbinaries/ffbinaries-prebuilt/releases. This will only happen once and is required to convert from `wem` to `wav`.

## Searching
The search for the lines now uses [Apache LuceneNET](https://lucenenet.apache.org/)! This means it now supports the [Lucene Query Parser Syntax](https://lucenenet.apache.org/docs/4.8.0-beta00017/api/queryparser/Lucene.Net.QueryParsers.Classic.html). Please check that linked documentation for tips on how to search better.

One thing you may find useful out of the box is prefixing words with "+" to ensure its part of the search. For example searching "+gopher" will only return lines with the word "gopher" in them.

## Troubleshooting
Always delete the cache at `%LOCALAPPDATA%\StarfieldVFRT\cache.json` when encountering issues. A lot of issues can arise from data being stale or incorrect in the cache. Future updates will have the ability to detect game version changes and automatically do this.

You can find the log files at `%LOCALAPPDATA%\StarfieldVFRT\logs`.

Any other issues please post an issue in https://github.com/yak3d/StarfieldVFRT/issues
