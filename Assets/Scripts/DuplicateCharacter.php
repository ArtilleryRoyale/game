#!/usr/bin/php
<?php

if (count($argv) != 3) {
    echo "Usage: ${argv[0]} TypeSource TypeDestination\nie: ${argv[0]} Queen Rook\n";
    exit(1);
}

// Convert from prefab:
$typeSource = ucfirst($argv[1]);
// and make a new character of type:
$typeDestination = ucfirst($argv[2]);

/*
    1/ We will copy all the file from:

     - Prefabs/Characters/CharacterTypePrefab.prefab
     - Prefabs/Characters/Type/Type.prefab
     - ScriptableObjects/Characters/TypeCharacter.asset
     - Sprites/Characters/Type/*.png
     - Sprites/Characters/Type/SpriteMeshes/*.asset
     - Materials/Characters/Type/*

    To some tmp directory,
    2/ save all the meta,
    3/ generate guid for each meta,
    4/ and switch them

    Has to be started from ./Scripts directory
*/

function replaceTypeInFile(string $typeSource, string $typeDest, string $file) {
    $data = file_get_contents($file);
    $data = str_replace($typeSource, $typeDest, $data);
    $data = str_replace(strtolower($typeSource), strtolower($typeDest), $data);
    file_put_contents($file, $data);
}

function replaceGuidInFile(string $guidSource, string $guidDest, string $file) {
    $data = file_get_contents($file);
    $data = str_replace("guid: $guidSource", "guid: $guidDest", $data);
    file_put_contents($file, $data);
}

function guidForMetaFile(string $file) : string {
    $lines = file($file);
    foreach ($lines as $l) {
        if (strpos($l, 'guid: ') !== false) {
            return trim(str_replace('guid: ', '', $l));
        }
    }
    throw new Exception("Did not find 'guid: ' in file $file");
}

function newGuid() : string {
    return "" . substr(sha1(microtime() . random_int(0, 1000000) . ":)"), 0, 32);
}

// Make directories
$tmpDirectory = "/tmp/DuplicateCharacter." . uniqid();
$mkDirectories = [
    "Prefabs/Characters/$typeDestination",
    "ScriptableObjects/Characters",
    "Sprites/Characters/$typeDestination/SpriteMeshes",
    "Materials/Characters/$typeDestination"
];
mkdir($tmpDirectory, 0777, true);
foreach ($mkDirectories as $d) {
    echo "MKDIR: $tmpDirectory/$d\n";
    mkdir($tmpDirectory . "/" . $d, 0777, true);
}

// Places to scan
$globs = [
    "../Prefabs/Characters/Character${typeSource}Prefab.prefab",
    "../Prefabs/Characters/Character${typeSource}Prefab.prefab.meta",
    "../Prefabs/Characters/${typeSource}/${typeSource}.prefab",
    "../Prefabs/Characters/${typeSource}/${typeSource}.prefab.meta",
    "../ScriptableObjects/Characters/${typeSource}Character.asset",
    "../ScriptableObjects/Characters/${typeSource}Character.asset.meta",
    "../Sprites/Characters/${typeSource}/*",
    "../Sprites/Characters/${typeSource}/SpriteMeshes/*",
    "../Materials/Characters/${typeSource}/${typeSource}*",
];

// File type
$metaFiles = [];
$editableFiles = [];
$proceededFiles = [];

foreach ($globs as $g) {
    foreach (glob($g) as $f) {
        if (is_dir($f)) continue;
        $dest = str_replace('../', '', $f);
        $dest = str_replace($typeSource, $typeDestination, $dest);
        $dest = str_replace(strtolower($typeSource), strtolower($typeDestination), $dest);
        echo "COPY: $f TO: /tmp/[...]/$dest\n";
        if (strpos($dest, '.meta') !== false) {
            $metaFiles[] = "$tmpDirectory/$dest";
        } elseif (strpos($dest, '.asset') !== false) {
            $editableFiles[] = "$tmpDirectory/$dest";
        } elseif (strpos($dest, '.prefab') !== false) {
            $editableFiles[] = "$tmpDirectory/$dest";
        } elseif (strpos($dest, '.mat') !== false) {
            $editableFiles[] = "$tmpDirectory/$dest";
        }
        copy($f, "$tmpDirectory/$dest");
        $proceededFiles[] = "$dest";
    }
}

$guids = [];
foreach ($metaFiles as $m) {
    $guids[guidForMetaFile($m)] = newGuid();
}

foreach ([$metaFiles, $editableFiles] as $dataFiles) {
    foreach ($dataFiles as $file) {
        foreach ($guids as $k => $v) {
            replaceGuidInFile($k, $v, $file);
            replaceTypeInFile($typeSource, $typeDestination, $file);
        }
    }
}

foreach ($proceededFiles as $d) {
    @mkdir("../" . dirname($d), 0777, true);
    copy("$tmpDirectory/$d", "../$d");
}

echo "\n";
exit(0);
