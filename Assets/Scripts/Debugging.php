#!/usr/bin/php
<?php

if ($argc != 2) {
    die("Usage: $argv[0] On|Off\n");
}

$activate = strtolower($argv[1]) == 'on';
$files = shell_exec("find *.cs .|grep -e '.cs$'");

$tags = [];
foreach (explode("\n", $files) as $file) {
    if (!file_exists($file)) continue;
    echo "Working with file: " . $file . "\n";
    $data = file_get_contents($file);
    if ($activate) {
        $data = str_replace([
            '  // Debugging.',
            '  // AssertNoRagdoll(',
            '  // NetworkAssertNotGuest('
        ], [
            '  Debugging.',
            '  AssertNoRagdoll(',
            '  NetworkAssertNotGuest('
        ], $data);
    } else {
        $data = str_replace([
            '  Debugging.',
            '  AssertNoRagdoll(',
            '  NetworkAssertNotGuest('
        ], [
            '  // Debugging.',
            '  // AssertNoRagdoll(',
            '  // NetworkAssertNotGuest('
        ], $data);
    }
    file_put_contents($file, $data);
}

echo "\n";
exit(0);
