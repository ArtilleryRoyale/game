#!/usr/bin/php
<?php

$list = true;
if ($argc >= 2) {
    $list = false;
    $activate = strtolower($argv[1]) == 'on';
    $tag = !empty($argv[2]) ? $argv[2] . '"' : '';
}

$files = shell_exec("find *.cs .|grep -e '.cs$'");

$tags = [];
foreach (explode("\n", $files) as $file) {
    if (!file_exists($file)) continue;
    if (!$list) {
        echo "Working with file: " . $file . "\n";
    }
    $data = file_get_contents($file);
    if ($list) {
        foreach (explode("\n", $data) as $line) {
            $matches = [];
            if (preg_match('`Log\.Message\("([a-z]+)"`mis', $line, $matches) != 0) {
                $tags[] = $matches[1];
            }
        }
    } else {
        if ($activate) {
            $a = '// '; $b = '';
        } else {
            $a = ''; $b = '// ';
        }
        $data = str_replace('  ' . $a . 'Log.Message("' . $tag, '  ' . $b . 'Log.Message("' . $tag, $data);
        file_put_contents($file, $data);
    }
}

if ($list) {
    $tags = array_unique($tags);
    sort($tags);
    echo implode("\n", $tags);
}

echo "\n";
exit(0);
