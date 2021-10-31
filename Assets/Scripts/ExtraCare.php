#!/usr/bin/php
<?php

if (count($argv) != 2) {
    echo "Usage: ${argv[0]} On|Off\nie: ${argv[0]} On\n";
    exit(1);
}

$activeExtraCare = strtolower($argv[1]) == "on";

/*
    For all *.cs file, look for
`
#if CC_EXTRA_CARE
try {
#endif
`
and
`
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception);}
#endif
`

    and replace them by // CC_EXTRA_CARE_TRY | CC_EXTRA_CARE_CATCH
    - or -
    look for those comment ^
    and replace them by the given blocks
*/

$CC_EXTRA_CARE_TRY = '#if CC_EXTRA_CARE
try {
#endif';
$CC_EXTRA_CARE_CATCH = '#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif';

function replaceInFile(string $stringSource, string $stringDest, string $file) {
    $data = file_get_contents($file);
    $data = str_replace($stringSource, $stringDest, $data);
    file_put_contents($file, $data);
}

$files = shell_exec("find *.cs .|grep -e '.cs$'");

foreach (explode("\n", $files) as $file) {
    if (!file_exists($file)) continue;
    echo "Working with file: " . $file . "\n";
    if ($activeExtraCare) {
        replaceInFile('// CC_EXTRA_CARE_TRY', $CC_EXTRA_CARE_TRY, $file);
        replaceInFile('// CC_EXTRA_CARE_CATCH', $CC_EXTRA_CARE_CATCH, $file);
    } else {
        replaceInFile($CC_EXTRA_CARE_TRY, '// CC_EXTRA_CARE_TRY', $file);
        replaceInFile($CC_EXTRA_CARE_CATCH, '// CC_EXTRA_CARE_CATCH', $file);
    }
}

echo "\n";
exit(0);
